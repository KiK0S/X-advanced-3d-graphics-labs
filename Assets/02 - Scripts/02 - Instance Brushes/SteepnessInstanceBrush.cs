using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SteepnessInstanceBrush : InstanceBrush {
    public int n_max_entities = 10;
    public float steepnessThreshold = 1;
    public KernelType kernelType = KernelType.Square;

    public override void draw(float x, float z) {
        for (int i = 0; i < n_max_entities; ++i) {
            Vector2 offset = InstanceBrushKernel.kernels[kernelType].sample(radius);
            float new_x = x + offset.x;
            float new_z = z + offset.y;
            if (terrain.getSteepness(new_x, new_z) <= steepnessThreshold) {
                spawnObject(new_x, new_z);
            }
        }
        Thread.Sleep(200);
    }   
}
