using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UI;

public class Animal : MonoBehaviour
{

    [Header("Animal parameters")]
    public float swapRate = 0.01f;
    public float mutateRate = 0.01f;
    public float swapStrength = 10.0f;
    public float mutateStrength = 0.5f;
    public float maxAngle = 10.0f;
    public float maxSpeed = 10.0f;

    [Header("Energy parameters")]
    public float maxEnergy = 10.0f;
    public float lossEnergy = 0.1f;
    public float gainEnergy = 10.0f;
    public float spawnEnergyRequired = 7.0f;
    private float energy;

    [Header("Sensor - Vision")]
    public float maxVision = 20.0f;
    public float stepAngle = 10.0f;
    public int nEyes = 5;

    [Header("Spawning")]
    public float spawnChance = 0.0001f;

    private int[] networkStruct;
    private SimpleNeuralNet brain = null;

    // Terrain.
    private CustomTerrain terrain = null;
    private int[,] details = null;
    private Vector2 detailSize;
    private Vector2 terrainSize;

    // Animal.
    private Transform tfm;
    private float[] visionInfo;
    private float[] geoInfo;
    private float[] dayInfo;
    private float[] networkInput;

    // Genetic alg.
    private GeneticAlgo genetic_algo = null;

    private DayNightLighting dayNightSystem;

    // Renderer.
    private Material mat = null;

    private float speedCoeff = 0.0f;

    private float timeOfLife = 0.0f;
    private bool isDestroyed = false;

    void Start()
    {
        // Network: 1 input per receptor, 1 output per actuator.
        visionInfo = new float[nEyes];
        geoInfo = new float[3];
        dayInfo = new float[2];

        MakeNetworkInput(visionInfo, geoInfo, dayInfo);

        energy = maxEnergy;
        tfm = transform;
        dayNightSystem = FindObjectOfType<DayNightLighting>();

        // Renderer used to update animal color.
        // It needs to be updated for more complex models.
        MeshRenderer renderer = GetComponentInChildren<MeshRenderer>();
        if (renderer != null)
            mat = renderer.material;
    }

    void Update()
    {
        if (isDestroyed)
            return;
        // In case something is not initialized...
        if (brain == null)
            brain = new SimpleNeuralNet(networkStruct);
        if (terrain == null)
            return;
        if (details == null)
        {
            UpdateSetup();
            return;
        }

        // Retrieve animal location in the heighmap
        int dx = (int)((tfm.position.x / terrainSize.x) * detailSize.x);
        int dy = (int)((tfm.position.z / terrainSize.y) * detailSize.y);

        // For each frame, we lose lossEnergy
        energy -= lossEnergy;
        timeOfLife += Time.deltaTime;
        // If the animal is located in the dimensions of the terrain and over a grass position (details[dy, dx] > 0), it eats it, gain energy and spawn an offspring.
        if ((dx >= 0) && dx < (details.GetLength(1)) && (dy >= 0) && (dy < details.GetLength(0)) && details[dy, dx] > 0)
        {
            // Eat (remove) the grass and gain energy.
            details[dy, dx] = 0;
            energy += gainEnergy;
            if (energy > maxEnergy)
                energy = maxEnergy;

            if (shouldSpawn())
                spawnOffspring();
        }

        // If the energy is below 0, the animal dies.
        if (energy < 0)
        {
            energy = 0.0f;
            genetic_algo.removeAnimal(this);
            return;
        }

        // Update the color of the animal as a function of the energy that it contains.
        if (mat != null)
            mat.color = Color.white * (energy / maxEnergy);

        // 1. Update receptor.
        UpdateVision();
        
        UpdateGeo();

        MakeNetworkInput(visionInfo, geoInfo, dayInfo);


        // 2. Use brain.
        float[] output = brain.getOutput(networkInput);

        // 3. Act using actuators.
        float angle = (output[0] * 2.0f - 1.0f) * maxAngle;
        tfm.Rotate(0.0f, angle, 0.0f);

        speedCoeff = (output[1] * 2.0f - 1.0f) * maxSpeed;        
    }

    private void UpdateGeo() {
        geoInfo[0] = tfm.position.x;
        geoInfo[1] = tfm.position.y;
        geoInfo[2] = tfm.position.z;
    }

    private void UpdateDay() {
        if (dayNightSystem.IsDaytime()) {
            dayInfo[0] = dayNightSystem.GetDayNightCycle();
            dayInfo[1] = -Mathf.Min(dayNightSystem.GetDayNightCycle(), 1 - dayNightSystem.GetDayNightCycle());
        } else {
            dayInfo[0] = -Mathf.Min(dayNightSystem.GetDayNightCycle(), 1 - dayNightSystem.GetDayNightCycle());
            dayInfo[1] = dayNightSystem.GetDayNightCycle();
        }
    }

    private bool shouldSpawn() {
        return energy >= spawnEnergyRequired &&
               UnityEngine.Random.Range(0.0f, 1.0f) < spawnChance * timeOfLife;
    }

    private void spawnOffspring() {
        if (genetic_algo.addOffspring(this)) {
            energy -= spawnEnergyRequired * 2.0f / 3.0f;
        }
    }

    /// <summary>
    /// Calculate distance to the nearest food resource, if there is any.
    /// </summary>
    private void UpdateVision()
    {
        float startingAngle = -((float)nEyes / 2.0f) * stepAngle;
        Vector2 ratio = detailSize / terrainSize;

        for (int i = 0; i < nEyes; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (stepAngle * i), 0.0f);
            Vector3 forwardAnimal = rotAnimal * Vector3.forward;
            float sx = tfm.position.x * ratio.x;
            float sy = tfm.position.z * ratio.y;
            visionInfo[i] = 0.0f;
            Debug.DrawRay(tfm.position, forwardAnimal * maxVision, Color.red);

            // Interate over vision length.
            for (float distance = 1.0f; distance < maxVision; distance += 0.5f)
            {
                // Position where we are looking at.
                float px = (sx + (distance * forwardAnimal.x * ratio.x));
                float py = (sy + (distance * forwardAnimal.z * ratio.y));

                if (px < 0)
                    px += detailSize.x;
                else if (px >= detailSize.x)
                    px -= detailSize.x;
                if (py < 0)
                    py += detailSize.y;
                else if (py >= detailSize.y)
                    py -= detailSize.y;

                if ((int)px >= 0 && (int)px < details.GetLength(1) && (int)py >= 0 && (int)py < details.GetLength(0) && details[(int)py, (int)px] > 0)
                {
                    visionInfo[i] = 1 / distance;
                    break;
                }
            }
        }
    }

    public void Setup(CustomTerrain ct, GeneticAlgo ga)
    {
        terrain = ct;
        genetic_algo = ga;
        UpdateSetup();
    }

    private void UpdateSetup()
    {
        detailSize = terrain.detailSize();
        Vector3 gsz = terrain.terrainSize();
        terrainSize = new Vector2(gsz.x, gsz.z);
        details = terrain.getDetails();
    }

    public void InheritBrain(SimpleNeuralNet other, bool mutate)
    {
        brain = new SimpleNeuralNet(other);
        if (mutate)
            brain.mutate(swapRate, mutateRate, swapStrength, mutateStrength);
    }
    public SimpleNeuralNet GetBrain()
    {
        return brain;
    }
    public float GetHealth()
    {
        return energy / maxEnergy;
    }
    public void NoFurtherUpdates()
    {
        isDestroyed = true;
    }
    private void MakeNetworkInput(params float[][] inputs) {
        if (networkInput == null) {
            int inputSize = 0;
            foreach (float[] input in inputs) {
                inputSize += input.Length;
            }
            networkInput = new float[inputSize];
            networkStruct = new int[] { inputSize, 5, 2 };
        }
        int index = 0;
        foreach (float[] input in inputs) {
            foreach (float value in input) {
                networkInput[index] = value;
                index++;
            }
        }
    }
}
