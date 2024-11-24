using UnityEngine;
using System.Collections.Generic;

public abstract class InstanceBrushKernel {
    public abstract Vector2 sample(float radius);
    
    public static Dictionary<KernelType, InstanceBrushKernel> kernels = new Dictionary<KernelType, InstanceBrushKernel>() {
        { KernelType.Square, new SquareInstanceKernel() },
        { KernelType.Circle, new CircleInstanceKernel() },
        { KernelType.Diamond, new DiamondInstanceKernel() }
    };
}

public class SquareInstanceKernel : InstanceBrushKernel {
    public override Vector2 sample(float radius) {
        return new Vector2(
            Random.Range(-radius, radius),
            Random.Range(-radius, radius)
        );
    }
}

public class CircleInstanceKernel : InstanceBrushKernel {
    public override Vector2 sample(float radius) {
        SquareInstanceKernel sampler = InstanceBrushKernel.kernels[KernelType.Square] as SquareInstanceKernel;
        for (int i = 0; i < 1000; ++i) {
            Vector2 sample = sampler.sample(radius);
            if (sample.magnitude <= radius) {
                return sample;
            }
        }
        return new Vector2(0, 0);
    }
}

public class DiamondInstanceKernel : InstanceBrushKernel {
    public override Vector2 sample(float radius) {
        // Sample from unit square and transform to diamond
        float x = Random.Range(-1f, 1f);
        float y = Random.Range(-1f, 1f);
        
        // Transform to diamond shape while maintaining uniform distribution
        float px = (x + y) * radius / 2;
        float py = (x - y) * radius / 2;
        
        return new Vector2(px, py);
    }
} 