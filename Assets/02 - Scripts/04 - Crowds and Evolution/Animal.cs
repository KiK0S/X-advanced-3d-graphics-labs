using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.UI;

public class Animal : MonoBehaviour
{

    private float speed = 0.0f;
    [HideInInspector]
    public int generation = 0;
    public float energy;
    public float waterEnergy;

    [Header("Goal")]
    public Transform hips = null;
    public Transform goal = null;
    public Transform goalRoot = null;

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
    private float[] hiddenInfo;
    private float[] networkInput;
    private int outputs = 4;

    private static ParameterManager parameters;
    // Genetic alg.
    private GeneticAlgo genetic_algo = null;

    private DayNightLighting dayNightSystem;

    // Renderer.
    private Material[] mats = null;

    private float speedCoeff = 0.0f;

    public float timeOfLife = 0.0f;
    private bool isDestroyed = false;

    private float goalUpdateTimer = 0f;

    public Vector3 minScale = new Vector3(0.33f, 0.33f, 0.33f);
    public Vector3 maxScale = new Vector3(1f, 1f, 1f);
    public float ageAdult = 15f;
    ReproductionEffectSpawner effectSpawner;

    void Start()
    {
        parameters = ParameterManager.Instance;
        GameObject goalObj = new GameObject("Goal");
        goal = goalObj.transform;
        goal.SetParent(goalRoot);
        // Network: 1 input per receptor, 1 output per actuator.
        visionInfo = new float[parameters.nEyes];
        geoInfo = new float[3];
        dayInfo = new float[2];
        hiddenInfo = new float[outputs + 1];
        for (int i = 0; i < hiddenInfo.Length; i++) {
            hiddenInfo[i] = 0.0f;
        }

        MakeNetworkInput(visionInfo, geoInfo, dayInfo, hiddenInfo);

        energy = parameters.initEnergy;
        waterEnergy = parameters.maxWaterEnergy;
        tfm = transform;
        dayNightSystem = FindObjectOfType<DayNightLighting>();

        // Get all mesh renderers and their materials
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        List<Material> materialsList = new List<Material>();
        foreach (MeshRenderer renderer in renderers)
        {
            materialsList.AddRange(renderer.materials);
        }
        mats = materialsList.ToArray();

        tfm.localScale = minScale;

        effectSpawner = FindObjectOfType<ReproductionEffectSpawner>();
    }

    void Update()
    {
        if (isDestroyed)
            return;
        // In case something is not initialized...
        if (brain == null)
            brain = new SimpleNeuralNet(networkStruct);
        if (terrain == null) {
            // Setup(FindObjectOfType<CustomTerrain>(), null);
            return;
        }
        if (details == null)
        {
            UpdateSetup();
            return;
        }

        // Retrieve animal location in the heighmap
        int dx = (int)((tfm.position.x / terrainSize.x) * detailSize.x);
        int dy = (int)((tfm.position.z / terrainSize.y) * detailSize.y);

        // For each frame, we lose lossEnergy
        energy -= parameters.lossEnergy;
        waterEnergy -= parameters.lossWaterEnergy;
        timeOfLife += Time.deltaTime;
        // If the animal is located in the dimensions of the terrain and over a grass position (details[dy, dx] > 0), it eats it, gain energy and spawn an offspring.
        int[] offsetsX = {-1, 0, 1, -1, 0, 1, -1, 0, 1};
        int[] offsetsY = {-1, -1, -1, 0, 0, 0, 1, 1, 1};
        bool eated = false;
        foreach (int offsetX in offsetsX) {
            foreach (int offsetY in offsetsY) {
                if ((dx + offsetX >= 0) && (dx + offsetX < details.GetLength(1)) && (dy + offsetY >= 0) && (dy + offsetY < details.GetLength(0)) && details[dy + offsetY, dx + offsetX] > 0) {
                    details[dy + offsetY, dx + offsetX] = 0;
                    energy += parameters.gainEnergy;
                    if (energy > parameters.maxEnergy)
                        energy = parameters.maxEnergy;
                    eated = true;
                }
            }
        }
        if (eated && shouldSpawn())
            spawnOffspring();
        
        //If in water set water energy to maximum
        if (tfm.position.y <= 15f)
        {
            waterEnergy = parameters.maxWaterEnergy;
        }
        
        // If the energy is below 0, the animal dies.
        if (energy < 0 || waterEnergy < 0)
        {
            energy = 0.0f;
            genetic_algo.removeAnimal(this);
            return;
        }

        // Update the color of all materials based on energy and vision
        if (false && mats != null && mats.Length > 0)
        {
            // Calculate the base brightness from energy
            float brightness = energy / parameters.maxEnergy;
            
            // Calculate green component based on weighted vision input
            float totalVision = 0f;
            float maxPossibleVision = parameters.nEyes * (1f / 1f); // Maximum possible vision value per eye
            
            foreach (float vision in visionInfo)
            {
                totalVision += vision;
            }
            
            // Normalize the green component (0 to 1 range)
            float greenComponent = Mathf.Clamp01(totalVision / maxPossibleVision);
            
            // Create the final color: base brightness for R&B, enhanced green
            Color newColor = new Color(
                brightness * 0.5f,                    // Red
                Mathf.Clamp01(brightness * 0.5f + greenComponent * 0.5f),  // Green (boosted by vision)
                brightness * 0.5f                     // Blue
            );
            
            // Apply to all materials
            foreach (Material mat in mats)
            {
                mat.color = newColor;
            }
        }

        // Change size of animal based on its age
        float ageFactor = Mathf.Clamp(timeOfLife, 0f, ageAdult) / ageAdult;
        tfm.localScale = Vector3.Lerp(minScale, maxScale, ageFactor);

        // 1. Update receptor.
        UpdateVision();
        
        UpdateGeo();

        UpdateDay();

        MakeNetworkInput(visionInfo, geoInfo, dayInfo, hiddenInfo);


        // 2. Use brain to get mean and variance
        float[] output = brain.getOutput(networkInput);
        
        // Mean angle from first output (mapped from [0,1] to [-maxAngle, maxAngle])
        float meanAngle = (output[0] * 2.0f - 1.0f) * parameters.maxAngle;
        
        // Variance from second output (clamped between min and max variance)
        float variance = Mathf.Lerp(parameters.minVariance, parameters.maxVariance, output[1]);
        
        // Sample from Gaussian distribution using Box-Muller transform
        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;
        float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
        
        // Apply the sampled angle
        float finalAngle = meanAngle + randStdNormal * Mathf.Sqrt(variance);
        finalAngle = Mathf.Clamp(finalAngle, -parameters.maxAngle, parameters.maxAngle);
        
        speed = Mathf.Lerp(parameters.minSpeed, parameters.maxSpeed, output[2] * 2.0f - 1.0f);
        
        for (int i = 0; i < output.Length; i++) {
            hiddenInfo[i] = output[i];
        }

        goalUpdateTimer += Time.deltaTime;
        if (goalUpdateTimer >= parameters.goalUpdateRate) {
            goalUpdateTimer = 0f;
            
            // Calculate new goal position
            Vector3 targetDirection = Quaternion.Euler(0f, finalAngle, 0f) * hips.rotation * Vector3.forward;
            
            goal.position = tfm.position + targetDirection.normalized * parameters.maxGoalDistance;
        }
    }

    public float GetSpeed() {
        return speed;
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
        return energy >= parameters.spawnEnergyRequired &&
               UnityEngine.Random.Range(0.0f, 1.0f) < parameters.spawnChance * (1.0f + timeOfLife);
    }

    private void spawnOffspring() {
        if (genetic_algo.addOffspring(this)) {
            energy -= parameters.spawnEnergyRequired * 2.0f / 3.0f;

            Color offspringColor = new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value);
            effectSpawner.SpawnReproductionEffect(tfm.position, offspringColor);
        }
    }

    /// <summary>
    /// Calculate distance to the nearest food resource, if there is any.
    /// </summary>
    private void UpdateVision()
    {
        float startingAngle = -((float)parameters.nEyes / 2.0f) * parameters.stepAngle;
        Vector2 ratio = detailSize / terrainSize;

        for (int i = 0; i < parameters.nEyes; i++)
        {
            Quaternion rotAnimal = tfm.rotation * Quaternion.Euler(0.0f, startingAngle + (parameters.stepAngle * i), 0.0f);
            Vector3 forwardAnimal = rotAnimal * Vector3.forward;
            float sx = tfm.position.x * ratio.x;
            float sy = tfm.position.z * ratio.y;
            visionInfo[i] = 0.0f;
            Debug.DrawRay(tfm.position, forwardAnimal * parameters.maxVision, Color.red);

        // Interate over vision length.
            for (float distance = 1.0f; distance < parameters.maxVision; distance += 0.5f)
            {
                // Position where we are looking at.
                int px = (int)(sx + (distance * forwardAnimal.x * ratio.x));
                int py = (int)(sy + (distance * forwardAnimal.z * ratio.y));


                if (px < 0 || px >= detailSize.x || py < 0 || py >= detailSize.y)
                    break;
                
                Vector3 pos = tfm.position + distance * forwardAnimal;
                Vector3 norm = Vector3.Cross(forwardAnimal, Vector3.up);
                // Debug.DrawLine(pos, pos + norm, Color.blue);
                // int[] offsetsX = {0, 1, 0, 1};
                // int[] offsetsY = {0, 0, 1, 1};
                int[] offsetsX = {-1, 0, 1, -1, 0, 1, -1, 0, 1};
                int[] offsetsY = {-1, -1, -1, 0, 0, 0, 1, 1, 1};
                // if (distance > 2.5) {
                    // offsetsX = new int[] {0};
                    // offsetsY = new int[] {0};
                // }
                bool found = false;
                foreach (int offsetX in offsetsX) {
                    foreach (int offsetY in offsetsY) {
                        if ((int)px + offsetX >= 0 && (int)px + offsetX < details.GetLength(1) && (int)py + offsetY >= 0 && (int)py + offsetY < details.GetLength(0) && details[(int)py + offsetY, (int)px + offsetX] > 0) {
                            visionInfo[i] = 1;// or / sqrt(distance) ? // / distance;
                            found = true;
                            break;
                        }
                    }
                    if (found)
                        break;
                }
                if (found)
                    break;
            }
        }
    }

    private void LogNetwork(float[] visionInfo, float[] geoInfo, float[] dayInfo, float meanAngle, float variance, float finalAngle) {
        string inputStr = "Network inputs:\nVision: ";
        for (int i = 0; i < visionInfo.Length; i++) {
            inputStr += visionInfo[i].ToString("F3");
            if (i < visionInfo.Length - 1) {
                inputStr += ", ";
            }
        }
        inputStr += "\nPosition: ";
        for (int i = 0; i < geoInfo.Length; i++) {
            inputStr += geoInfo[i].ToString("F3");
            if (i < geoInfo.Length - 1) {
                inputStr += ", ";
            }
        }
        inputStr += "\nDay/Night: ";
        for (int i = 0; i < dayInfo.Length; i++) {
            inputStr += dayInfo[i].ToString("F3");
            if (i < dayInfo.Length - 1) {
                inputStr += ", ";
            }
        }
        Debug.Log(inputStr);
        Debug.Log("Generated from " + meanAngle + " with variance " + variance + ", final angle: " + finalAngle);
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
        Start();
        brain = new SimpleNeuralNet(other);
        if (mutate)
            brain.mutate(parameters.swapRate, parameters.mutateRate, parameters.mutateStrength * Mathf.Pow(parameters.mutateDecay, generation));
    }
    public SimpleNeuralNet GetBrain()
    {
        return brain;
    }
    public float GetHealth()
    {
        return energy / parameters.maxEnergy;
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
            networkStruct = new int[] { inputSize, outputs };
        }
        int index = 0;
        foreach (float[] input in inputs) {
            foreach (float value in input) {
                networkInput[index] = value;
                index++;
            }
        }
    }

    public string ExportNetworkWeights()
    {
        if (brain == null) return "";
        return brain.SerializeWeights();
    }

    public float[] GetEyeInputs()
    {
        return visionInfo;
    }

    public float[] GetLastOutputs()
    {
        return hiddenInfo;
    }
    public static int GetEyes() {
        return parameters.nEyes;
    }
}
