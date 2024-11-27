using UnityEngine;

public class GrassManager : MonoBehaviour
{
    [Header("Growth Settings")]
    public float vegetationGrowthRate = 2.0f;
    [Range(0, 100)]
    public float lowGrassThreshold = 10.0f;
    [Range(0, 100)]
    public float highGrassThreshold = 80.0f;
    
    private float currentGrowth;
    private CustomTerrain customTerrain;
    private DayNightLighting dayNightSystem;

    void Start()
    {
        customTerrain = FindObjectOfType<CustomTerrain>();
        dayNightSystem = FindObjectOfType<DayNightLighting>();
        currentGrowth = 0.0f;
    }

    void Update()
    {
        // Only grow resources during the day
        if (dayNightSystem.IsDaytime())
        {
            UpdateGrass();
        }
    }

    public void UpdateGrass()
    {
        Vector2 detail_sz = customTerrain.detailSize();
        int[,] details = customTerrain.getDetails();
        currentGrowth += vegetationGrowthRate * Time.deltaTime;

        while (currentGrowth > 0.0f)
        {
            int x = (int)(Random.value * detail_sz.x);
            int y = (int)(Random.value * detail_sz.y);

            float x_c = (float)x / detail_sz.x * customTerrain.gridSize().x;
            float y_c = (float)y / detail_sz.y * customTerrain.gridSize().z;

            if (customTerrain.get(x_c, y_c) < lowGrassThreshold || customTerrain.get(x_c, y_c) > highGrassThreshold)
            {
                continue;
            }


            details[y, x] = 1; // Set to low grass
            details[y, x] = 2; // Set to high grass

            currentGrowth -= 1.0f;
        }
        
        customTerrain.saveDetails();
    }
} 