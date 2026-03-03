using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;

using System.Collections;
using System.Collections.Generic;
using System;

using UnityEngine;

public class CarAgent : Agent
{
    [Header("Components")]
    [SerializeField] private Transform testCube;
    private Rigidbody rb;
    private CarController controller;
    private Transform point;

    [Header("Variables")]
    public int myIndex;
    private float rwd;
    private float episodeTime;

    [Header("Settings")]
    [HideInInspector] public bool isManual = false;
    [HideInInspector] public bool alwaysAccelerate = true;
    [HideInInspector] public float startRotAngle;
    
    [Header("Prev Pose")]
    private Vector3 prevPointDist;
    private float prevPointAngle = float.MaxValue;
    private int prevCheckIndex;
    private int nextCheckIndex;

    [Header("Collisions")]
    [SerializeField] private float[] directions = { -45f, 45f };
    [SerializeField] private float maxDistance = 15f;
    [SerializeField] private LayerMask collideLayer;
    private List<float> hitDistances = new List<float>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        controller = GetComponent<CarController>();
        myIndex = transform.GetSiblingIndex();
    }

    public string GetModelName()
    {
        var behaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (behaviorParameters == null || behaviorParameters.Model == null) return "NoModel";
        return behaviorParameters.Model.name.Replace(" ", "");
    }

    public void SwitchControlMode(bool doSet = false, bool setValue = false)
    {
        alwaysAccelerate = true;
        if (doSet) isManual = setValue;
        else isManual = !isManual;
    }

    // ML Agent calls this to update state space
    public override void CollectObservations(VectorSensor sensor)
    {
        // Get distance and angle to next checkpoint
        point = GameManager.Instance.GetCheckpoint(nextCheckIndex);
        Vector3 dist = point.position - transform.position;
        float pointAngle = Vector3.Angle(transform.forward, dist);

        // Apply observations
        sensor.AddObservation(rb.velocity);
        sensor.AddObservation(dist);
        sensor.AddObservation(pointAngle);

        // Visualisations
        testCube.position = point.position;
        testCube.rotation = point.rotation;
    }

    // ML Agent calls this to reset on episode begin
    public override void OnEpisodeBegin()
    {
        controller.ResetPosition();
        prevCheckIndex = -1;
        nextCheckIndex = 0;
        episodeTime = Time.time;
    }

    void DoEndEpisode()
    {
        StatsManager.Instance.EndEpisode(myIndex, StepCount, GetCumulativeReward());
        EndEpisode();
    }

    // ML Agent gives an action to be executed
    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Get and apply actions
        ActionSegment<float> actions = actionBuffers.ContinuousActions;
        controller.SetInput(actions[1], actions[0]);

        // Get checkpoint data
        Vector3 dist = point.position - transform.position;
        float curDist = dist.magnitude;
        float curAngle = Vector3.Angle(transform.forward, dist);

        // Reward for getting closer to the checkpoint
        float distDelta = prevPointDist.magnitude - curDist;
        float distReward = (distDelta > 0) ? 0.005f : -0.01f;
        AddReward(distReward);
        prevPointDist = dist;

        // Reward based on angle to checkpoint
        float angleReward = Interpolate(curAngle, 0f, 55f, 0.0075f, -0.0035f);
        AddReward(angleReward);
        prevPointAngle = curAngle;

        // Small step penalty to encourage speed
        AddReward(-0.01f);

        // Stats
        StatsManager.Instance.EndStep(myIndex, actions[1]);
        UI.Instance.UpdateUI(myIndex);

        // End episode if max step reached
        if (StepCount >= MaxStep - 1) DoEndEpisode();
    }

    // Used to provide manual control to test the agent
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        if (isManual)
        {
            continuousActions[0] = Input.GetAxis("Horizontal");
            continuousActions[1] = alwaysAccelerate ? 1 : Input.GetAxis("Vertical");
        }
        else
        {
            continuousActions[0] = DoVisual();
            continuousActions[1] = 1;
        }
    }

    float DoVisual()
    {
        // Get hit distances for each direction and draw debug lines
        hitDistances.Clear();
        foreach (float dir in directions)
        {
            // Fire ray
            Vector3 position = transform.position + Vector3.up * 0.4f;
            Vector3 direction = Quaternion.Euler(0, dir, 0) * transform.forward;
            Physics.Raycast(position, direction, out RaycastHit hit, maxDistance, collideLayer);

            // Add to hit distances
            hitDistances.Add(hit.collider != null ? hit.distance : maxDistance);

            // Draw debug rays
            if (hit.collider != null) Debug.DrawLine(transform.position, hit.point, Color.red);
            else Debug.DrawRay(transform.position, direction * maxDistance, Color.green);
        }

        // Get totals for both sides
        float leftTotal = 0f;
        float rightTotal = 0f;
        for (int i = 0; i < hitDistances.Count; i++)
        {
            if (i < hitDistances.Count / 2) leftTotal += hitDistances[i];
            else rightTotal += hitDistances[i];
        }

        // Get the turn value linearly proportional to the magnitude of the difference in totals
        float x = Interpolate(Mathf.Abs(rightTotal - leftTotal), 0f, maxDistance, 0f, 1f);

        // Apply directionality and return
        x *= (rightTotal - leftTotal < 0f) ? -1 : 1;
        return x;
    }

    // Apply collision penalties
    void OnCollisionEnter(Collision col)
    {
        CollisionPenalty(col, 0.5f, 0.4f);
        StatsManager.Instance.AddColTrigger(myIndex);
    }

    void OnCollisionStay(Collision col)
    {
        CollisionPenalty(col, 0.005f, 0.004f);
        StatsManager.Instance.AddColStay(myIndex);
    }

    void CollisionPenalty(Collision col, float val1, float val2)
    {
        if (col.gameObject.layer == 11)
        {
            AddReward(-val1);
        }
        if (col.gameObject.TryGetComponent<CarController>(out CarController other))
        {
            AddReward(-val2);
        }
    }

    // Hit a checkpoint
    void OnTriggerEnter(Collider other)
    {
        // If not a checkpoint
        if (other.gameObject.layer != 12) return;

        // Get sibling index of checkpoint
        int curIndex = other.transform.GetSiblingIndex();
        int cnt = GameManager.Instance.GetCheckpointCount();

        // Reward if correct checkpoint, else penalise
        if (curIndex == nextCheckIndex)
        {
            // Add reward for hitting checkpoint
            AddReward(1f);

            // Update checkpoint tracking
            prevCheckIndex = curIndex;
            nextCheckIndex = (curIndex + 1) % cnt;
            StatsManager.Instance.HitCheckpoint(myIndex, prevCheckIndex);
            UI.Instance.SetPoint(myIndex);

            // If hit end of lap
            if (nextCheckIndex == 0 && prevCheckIndex > -1)
            {
                bool doEnd = StatsManager.Instance.SetLapEnd(myIndex);
                AddReward(1.5f);
                UI.Instance.SetLap(myIndex);
                if (doEnd) DoEndEpisode();
            }
        }
        else
        {
            AddReward(-0.05f);
        }
    }

    float Interpolate(float x, float xMin, float xMax, float yMin, float yMax)
    {
        return (yMin + (((x - xMin) * (yMax - yMin)) / (xMax - xMin)));
    }
}
