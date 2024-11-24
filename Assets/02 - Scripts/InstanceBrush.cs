using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public abstract class InstanceBrush : Brush {
    [System.Serializable]
    public class PrefabDistribution {
        public GameObject prefab;
        public float minHeight = 0f;
        public float maxHeight = 100f;
        public float density = 1f;
        public float minScale = 0.8f;
        public float maxScale = 1.2f;
    }

    [Header("Placement System")]
    public bool useTreeSystem = true;
    public List<PrefabDistribution> prefabDistributions = new List<PrefabDistribution>();
    
    private int prefab_idx = -1;

    public override void callDraw(float x, float z) {
        if (useTreeSystem) {
            if (terrain.object_prefab) {
                prefab_idx = terrain.registerPrefab(terrain.object_prefab);
            } else {
                terrain.debug.text = "No prefab to instantiate";
                return;
            }
        } else if (prefabDistributions.Count == 0) {
            terrain.debug.text = "No prefab distributions defined";
            return;
        }

        Vector3 grid = terrain.world2grid(x, z);
        draw(grid.x, grid.z);
    }

    public override void draw(int x, int z) {
        draw((float)x, (float)z);
    }

    protected void spawnObject(float x, float z) {
        Vector3 position = terrain.getInterp3(x, z);
        
        if (useTreeSystem) {
            if (prefab_idx == -1) return;
            float scaleTree = Random.Range(terrain.min_scale, terrain.max_scale);
            terrain.spawnObject(position, scaleTree, prefab_idx);
            return;
        }

        // Filter valid distributions based on height
        float height = terrain.getInterp(x, z);
        var validDistributions = prefabDistributions.Where(
            dist => height >= dist.minHeight && 
                   height <= dist.maxHeight && 
                   Random.value <= dist.density
        ).ToList();

        if (validDistributions.Count == 0) return;

        // Randomly select one distribution
        var selectedDist = validDistributions[Random.Range(0, validDistributions.Count)];
        
        // Spawn the object
        float scale = Random.Range(selectedDist.minScale, selectedDist.maxScale);
        Vector3 worldPos = terrain.grid2world(position);
        GameObject obj = Instantiate(
            selectedDist.prefab, 
            worldPos, 
            Quaternion.Euler(0, Random.Range(0f, 360f), 0)
        );
        obj.transform.localScale = Vector3.one * scale;
        obj.transform.parent = terrain.transform;
    }
}
