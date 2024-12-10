using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine;

public class ReproductionEffectSpawner : MonoBehaviour
{
    public GameObject reproductionEffectPrefab; // Assign the Particle System prefab here

    /// <summary>
    /// Spawns a reproduction particle effect at the given position with optional color customization.
    /// </summary>
    /// <param name="position">The world position to spawn the effect.</param>
    /// <param name="color">The color of the particles (optional).</param>
    public void SpawnReproductionEffect(Vector3 position, Color? color = null)
    {
        if (reproductionEffectPrefab == null)
        {
            Debug.LogError("Reproduction effect prefab is not assigned.");
            return;
        }

        // Instantiate the particle system at the given position
        GameObject effect = Instantiate(reproductionEffectPrefab, position, Quaternion.identity);

        // Optional: Customize the particle color
        if (color.HasValue)
        {
            ParticleSystem.MainModule mainModule = effect.GetComponent<ParticleSystem>().main;
            mainModule.startColor = color.Value;
        }

        // Destroy the particle system after it finishes playing
        Destroy(effect, 2f); // Adjust based on the duration of your particle effect
    }
}
