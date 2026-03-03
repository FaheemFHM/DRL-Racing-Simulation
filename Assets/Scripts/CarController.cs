using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* Reference The Unity Project */

[System.Serializable]
public class Wheel
{
    public WheelCollider col;
    public Transform mesh;
    public bool doMove;
    public bool doTurn;
}

public class CarController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private List<Wheel> wheels;
    [SerializeField] [Range(10f, 1000f)] private float maxMove = 500f;
    [SerializeField] [Range(10f, 100f)] private float maxBrake = 100f;
    [SerializeField] [Range(5f, 75f)] private float maxTurn = 45f;
    [SerializeField] [Range(0f, 1f)] private float reverseSpeedMult = 0.5f;

    [Header("Movement Values")]
    private float moveInput;
    private float moveValue;
    private float turnInput;
    private float turnValue;
    private float brakeValue;

    [Header("Spawn Pose")]
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    void FixedUpdate()
    {
        brakeValue = Mathf.Abs(moveInput) < 0.01f ? maxBrake : 0f;
        moveValue = brakeValue == 0f ? moveInput * maxMove : 0f;
        if (moveValue < 0f) moveValue *= reverseSpeedMult;
        turnValue = turnInput * maxTurn;
        if (Mathf.Abs(moveValue) > maxMove) Debug.Log(moveValue);
        ApplyMovement();
        TurnWheels();
    }

    public void SetStartPosition(Vector3 pos, float rot)
    {
        spawnPos = pos;
        spawnRot = Quaternion.Euler(0, rot, 0);
        GetComponent<CarAgent>().startRotAngle = rot;
        ResetPosition();
    }

    public void ResetPosition()
    {
        transform.localPosition = spawnPos;
        transform.rotation = spawnRot;
    }
    
    void ApplyMovement()
    {
        foreach (Wheel w in wheels)
        {
            if (w.doMove)
            {
                w.col.motorTorque = moveValue;
                w.col.brakeTorque = brakeValue;
            }
            if (w.doTurn)
            {
                w.col.steerAngle = turnValue;
            }
        }
    }

    void TurnWheels()
    {
        foreach (Wheel w in wheels)
        {
            if (!w.doTurn) continue;
            w.col.GetWorldPose(out Vector3 pos, out Quaternion rot);
            w.mesh.SetPositionAndRotation(pos, rot);
        }
    }

    public void SetInput(float move, float turn)
    {
        this.moveInput = move;
        this.turnInput = turn;
    }

    public (float move, float turn, float brake) GetStats() => (moveValue, turnValue, brakeValue);
    public (float maxMove, float maxTurn, float maxBrake) GetStatsMax() => (maxMove, maxTurn, maxBrake);
}
