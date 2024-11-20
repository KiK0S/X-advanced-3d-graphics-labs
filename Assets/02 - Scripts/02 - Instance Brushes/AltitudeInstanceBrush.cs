using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltitudeInstanceBrush : InstanceBrush {
    public int n_max_entities = 10;
    public float heightThreshold = 15;

    public override void draw(float x, float z) {
        for (int i = 0; i < n_max_entities; ++i) {
            float new_x = x + Random.Range(-radius, radius);
            float new_z = z + Random.Range(-radius, radius);
            if (terrain.getInterp(new_x, new_z) > heightThreshold) {
                spawnObject(new_x, new_z);
            }
        }
    }   
}
