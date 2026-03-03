using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lidar : MonoBehaviour
{
    [SerializeField] private LayerMask collideLayer;
    [SerializeField] private float rayDistance;
    [SerializeField] private Gradient colorMap;
    [SerializeField] [Range(1f, 359f)] private float spinSpeed;
    [SerializeField] [Range(0f, 3f)] private float duration;
    [SerializeField] private int gridRes;
    private float angle;
    private float[,] grid;
    private int gridSize;
    private Vector3 offset;
    private Texture2D texture;
    private float segments = 36f;
    private Renderer rend;
    private Transform ogMap;
    private Transform target;

    void Awake()
    {
        ogMap = GameObject.FindWithTag("Map").transform;
        target = GameObject.FindWithTag("Player").transform;
        rend = ogMap.GetComponent<Renderer>();
    }

    public void SetTarget(Transform car)
    {
        target = car;
        transform.position = target.position + Vector3.up * 0.2f;
    }

    public void ResetMap(Transform ground)
    {
        // Variables
        gridSize = (int)ground.localScale.x * gridRes;
        ogMap.position = ground.position - Vector3.right * gridSize;
        ogMap.localScale = ground.localScale;
        offset = new Vector3(gridSize / 2 - ground.position.x, 0, gridSize / 2 - ground.position.z);

        // Reset map values
        grid = new float[gridSize, gridSize];
        texture = new Texture2D(gridSize, gridSize);
    }

    void Update()
    {
        transform.position = target.position + Vector3.up * 0.2f;
    }

    void FixedUpdate()
    {
        angle += spinSpeed;
        FireRay();
        ApplyColorMap();
        rend.material.mainTexture = texture;
    }

    void ApplyColorMap()
    {
        // Apply lidar results to texture
        for (int x = 0; x < gridSize; x++)
        {
            for (int y = 0; y < gridSize; y++)
            {
                Color color = colorMap.Evaluate(grid[x, y]);
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
    }

    void FireRay()
    {
        RaycastHit hit;
        Color rayCol = Color.green;
        float dist = rayDistance;

        Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
        
        // Lidar-like ray cast
        if (Physics.Raycast(transform.position, direction, out hit, rayDistance, collideLayer))
        {
            rayCol = Color.red;
            dist = hit.distance;
            int gridX = (int)((hit.point.x + offset.x) * gridRes);
            int gridY = (int)((hit.point.z + offset.z) * gridRes);

            if (gridX >= 0 && gridX < gridSize && gridY >= 0 && gridY < gridSize)
            {
                grid[gridX, gridY] = 1f;
            }
        }

        Debug.DrawRay(transform.position, direction * dist, rayCol, duration);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        float angleStep = 360f / segments;
        Vector3 lastPoint = transform.position + new Vector3(rayDistance, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float currentAngle = i * angleStep * Mathf.Deg2Rad;
            Vector3 newPoint = transform.position + new Vector3(Mathf.Cos(currentAngle) * rayDistance, 0, Mathf.Sin(currentAngle) * rayDistance);
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }
    }
}
