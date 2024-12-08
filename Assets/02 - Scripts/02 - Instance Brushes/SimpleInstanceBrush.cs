using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class SimpleInstanceBrush : InstanceBrush {

    public override void draw(float x, float z) {
        spawnObject(x, z);
        Thread.Sleep(200);
    }
}
