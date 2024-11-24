using UnityEngine;

public class EraserInstanceBrush : InstanceBrush {
    public KernelType kernelType = KernelType.Square;

    public override void draw(float x, float z) {
        // Get all tree instances in the terrain
        TreeInstance[] trees = terrain.getTreeInstances();
        
        // Check each tree if it's within our erase radius
        for (int i = trees.Length - 1; i >= 0; i--) {
            Vector3 treePos = terrain.getTreePosition(trees[i]);
            
            // Convert tree position to relative coordinates from brush center
            int dx = Mathf.RoundToInt(treePos.x - x);
            int dz = Mathf.RoundToInt(treePos.z - z);
            
            // Use the same kernel logic as terrain brushes
            if (BrushKernel.kernels[kernelType].included(dx, dz, radius)) {
                terrain.RemoveObjectAtPosition(treePos, radius, useTreeSystem);
            }
        }
    }
} 