using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticObstacles : MonoBehaviour
{
    [SerializeField] private GameObject[] prefabs;
    private float maxDist = 3f;

    public void DoSpawn(Transform checkpointParent, bool doSpawn, bool doClear, int obstacleCount)
    {
        if (doClear) { foreach (Transform t in transform) Destroy(t.gameObject); }
        if (!doSpawn) return;
        maxDist = (checkpointParent.GetChild(0).localScale.z / 2f) * 0.9f;
        Transform[] checkpoints = new Transform[checkpointParent.childCount];
        for (int i = 0; i < checkpointParent.childCount; i++) checkpoints[i] = checkpointParent.GetChild(i);
        List<int> indexes = GetIndexes(obstacleCount, 0, checkpoints.Length);
        foreach (int i in indexes)
        {
            Transform t = checkpoints[i];
            Instantiate(prefabs[Random.Range(0, prefabs.Length)], t.position + t.forward * Random.Range(-maxDist, maxDist), t.rotation, transform);
        }
    }

    List<int> GetIndexes(int count, int min, int max)
    {
        // Keep all obstacle positions unique
        List<int> numbers = new List<int>();
        HashSet<int> uniqueNumbers = new HashSet<int>();

        while (uniqueNumbers.Count < count)
        {
            int rand = Random.Range(min, max);
            if (uniqueNumbers.Add(rand)) numbers.Add(rand);
        }
        return numbers;
    }
}
