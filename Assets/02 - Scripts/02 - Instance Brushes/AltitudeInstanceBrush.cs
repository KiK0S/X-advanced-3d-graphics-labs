using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltitudeInstanceBrush : InstanceBrush {
    public int n_max_entities = 10;
    public float heightThreshold = 15;
    public KernelType kernelType = KernelType.Square;

    public override void draw(float x, float z) {
        for (int i = 0; i < n_max_entities; ++i) {
            Vector2 offset = InstanceBrushKernel.kernels[kernelType].sample(radius);
            float new_x = x + offset.x;
            float new_z = z + offset.y;
            if (terrain.getInterp(new_x, new_z) > heightThreshold) {
                spawnObject(new_x, new_z);
            }
        }
    }   
}
