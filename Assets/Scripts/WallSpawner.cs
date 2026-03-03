using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct Walls
{
    public GameObject roadPrefab;
    public GameObject wallPrefab;
}

public class WallSpawner : MonoBehaviour
{
    [SerializeField] private Walls[] wallPairs;

    public void DoSpawn()
    {
        // Clear any existing walls do avoid duplication
        foreach (Transform child in transform) Destroy(child.gameObject);

        // For each track piece
        Transform roadFolder = transform.parent.Find("Road");
        foreach (Transform roadPiece in roadFolder)
        {
            // Get the corresponding wall piece
            GameObject wall = GetWallForRoad(roadPiece.gameObject);
            if (wall == null) continue;

            // Place the wall according to the track
            Transform w = Instantiate(wall, roadPiece.position, roadPiece.rotation, transform).transform;
            w.localScale = roadPiece.localScale;
        }
    }

    private GameObject GetWallForRoad(GameObject road)
    {
        string roadName = road.name.Split('(')[0].Trim();
        foreach (Walls pair in wallPairs)
        {
            if (pair.roadPrefab.name == roadName) return pair.wallPrefab;
        }
        return null;
    }
}
