using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnCheckpoints : MonoBehaviour
{
    public GameObject checkpointPrefab;

    public void DoSpawn()
    {
        // Clear all existing checkpoints
        foreach (Transform child in transform) Destroy(child.gameObject);

        // Get the road folder
        Transform roadFolder = transform.parent.Find("Road");

        // Traverse the road, replacing all checkpoint placeholders with checkpoints
        foreach (Transform roadPiece in roadFolder)
        {
            // Instantiate checkpoint
            foreach (Transform checkpoint in roadPiece)
                Instantiate(checkpointPrefab, checkpoint.position, checkpoint.rotation, transform);

            // Destroy placeholder
            foreach (Transform placeholder in roadPiece)
                Destroy(placeholder.gameObject);
        }
    }
}
