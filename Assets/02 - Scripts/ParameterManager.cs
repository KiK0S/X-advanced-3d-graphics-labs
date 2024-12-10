using UnityEngine;

public class ParameterManager : MonoBehaviour
{
    private static ParameterManager _instance;
    public static ParameterManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ParameterManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("Parameter Manager");
                    _instance = go.AddComponent<ParameterManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Animal Genetic Parameters")]
    public float swapRate = 0.05f;
    public float mutateRate = 0.05f;
    public float mutateStrength = 0.5f;
    public float mutateDecay = 0.9f;
    public float maxAngle = 10.0f;
    public float minVariance = 0.1f;
    public float maxVariance = 5.0f;
    public float minSpeed = 0.05f;
    public float maxSpeed = 0.5f;

    [Header("Animal Energy Parameters")]
    public float maxEnergy = 50.0f;
    public float initEnergy = 100.0f;
    public float lossEnergy = 0.1f;
    public float gainEnergy = 20.0f;
    public float maxWaterEnergy = 50.0f;
    public float lossWaterEnergy = 0.0f;
    public float spawnEnergyRequired = 3.0f;

    [Header("Animal Vision Parameters")]
    public float maxVision = 20.0f;
    public float stepAngle = 10.0f;
    public int nEyes = 5;
    public float spawnChance = 0.1f;
    
    [Header("Animal Movement")]
    public float maxGoalDistance = 10.0f;  // Reduced from 10 to make steps more manageable
    public float goalUpdateRate = 0.5f;   // How often to update goal position


    public enum ActivationFunction { Sigmoid, ReLU }
    [Header("Neural Network Parameters")]
    public ActivationFunction activationFunction = ActivationFunction.ReLU;
    public float meanInitialWeight = 0.5f;
    public float stdInitialWeight = 0.2f;

    [Header("Foot Stepper Parameters")]
    public float distanceThreshold = 0.4f;
    public float angleThreshold = 135f;
    public float moveDuration = 0.125f;
    public float stepOvershootFraction = 0.75f;
    public float heightOffset = 0.1f;

    [Header("Population Control")]
    public int popSize = 100;
    public int maxAnimals = 1000;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
}
