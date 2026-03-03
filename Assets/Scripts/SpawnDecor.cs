using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct decoration
{
    public string name;
    public GameObject[] prefabs;
    public float sizeMin;
    public float sizeMax;
}

public class SpawnDecor : MonoBehaviour
{
    [SerializeField] private decoration[] decorations;
    [SerializeField] private LayerMask collideMask;

    public void DoSpawn(Transform groundPlane, bool doSpawn, bool doClear, float spawnChance)
    {
        // Destroy old objects
        if (doClear) { foreach (Transform child in transform) Destroy(child.gameObject); }
        if (!doSpawn) return;

        // Get values
        int spawnRadius = (int)groundPlane.localScale.x / 2;
        int xOffset = (int)groundPlane.position.x;
        int zOffset = (int)groundPlane.position.z;

        // Spawn new objects
        for (int x = -spawnRadius; x < spawnRadius; x += 5)
        {
            for (int z = -spawnRadius; z < spawnRadius; z += 5)
            {
                if (Random.Range(0f, 1f) > spawnChance) continue;
                Spawn(new Vector3(x + xOffset, 0, z + zOffset));
            }
        }
    }

    void Spawn(Vector3 pos)
    {
        // Get values
        int decorType = Random.Range(0, decorations.Length);
        int decorIndex = Random.Range(0, decorations[decorType].prefabs.Length);
        float scale = Random.Range(decorations[decorType].sizeMin, decorations[decorType].sizeMax);

        // Check if position is valid
        Collider[] hitColliders = Physics.OverlapSphere(pos, scale, collideMask);
        if (hitColliders.Length != 0) return;

        // Create object
        Transform t = Instantiate(decorations[decorType].prefabs[decorIndex], pos, Quaternion.identity, transform).transform;
        t.Rotate(0, 90 * Random.Range(0, 4), 0);
        t.localScale = Vector3.one * scale;
    }
}
