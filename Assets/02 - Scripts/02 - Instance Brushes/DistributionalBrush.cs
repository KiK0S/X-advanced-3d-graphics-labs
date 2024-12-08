using TreeEditor;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System.Collections.Generic;
using System.Threading; // Required for Thread.Sleep


public class DistributionalBrush : InstanceBrush {
    public KernelType kernelType = KernelType.Square;
    public bool copying = true;
    public List<float> dxs = new List<float>(); // Stores copied trees
    public List<float> dzs = new List<float>();
    public int RandomStrength = 5;

    public override void draw(float x, float z) {
        if (copying) {
            dxs.Clear();
            dzs.Clear();
            // Copy trees within the brush radius
            TreeInstance[] all_trees = terrain.getTreeInstances();

            for (int i = all_trees.Length - 1; i >= 0; i--) {
                Vector3 treePos = terrain.getTreePosition(all_trees[i]);

                int dx = Mathf.RoundToInt(treePos.x - x);
                int dz = Mathf.RoundToInt(treePos.z - z);

                if (BrushKernel.kernels[kernelType].included(dx, dz, radius)) {
                    // Add the tree instance to the copied list
                    dxs.Add(dx);
                    dzs.Add(dz);
                }
            }
        } else {
            // Paste copied trees at the new location
            for (int i = dxs.Count - 1; i >= 0; i--) {
                float dx = dxs[i] + Random.Range(-RandomStrength, RandomStrength);
                float dz = dzs[i] + Random.Range(-RandomStrength, RandomStrength);

                // Spawn a new object at the offset position
                spawnObject(x + dx, z + dz);

                // Optional: Pause between spawns for effect
                Thread.Sleep(200);
            }
        }
    }
} 