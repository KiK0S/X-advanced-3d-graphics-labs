using UnityEngine;

public class RandomBrush : TerrainBrush {
    public float height = 1f;
    public KernelType kernelType = KernelType.Square;

    public override void draw(int x, int z) {
        for (int zi = -radius; zi <= radius; zi++) {
            for (int xi = -radius; xi <= radius; xi++) {
                if (!BrushKernel.kernels[kernelType].included(xi, zi, radius)) continue;
                    float y = terrain.get(x + xi, z + zi);
                    terrain.set(x + xi, z + zi, y + Random.Range(-height, height));
            }
        }
    }
} 