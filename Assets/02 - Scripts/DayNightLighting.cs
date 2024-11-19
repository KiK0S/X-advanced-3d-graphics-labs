using UnityEngine;

public class DayNightLighting : MonoBehaviour
{
    [Header("Light References")]
    public Light directionalLight;

    [Header("Day Settings")]
    public Color dayColor = new Color(1f, 0.95f, 0.8f); // Warm sunlight
    public float dayIntensity = 1f;

    [Header("Night Settings")]
    public Color nightColor = new Color(0.5f, 0.5f, 0.8f); // Cool moonlight
    public float nightIntensity = 0.2f;

    [Header("Day/Night Cycle")]
    public float deltaTime = 0.01f;
    public float nightDuration = 10f;
    public float dayDuration = 10f;
    
    private bool isDay = true;
    private float timer = 0;

    [SerializeField] public Material daySkybox;
    [SerializeField] public Material nightSkybox;
   

    private GeneticAlgo geneticAlgo;

    void Start()
    {
        geneticAlgo = FindObjectOfType<GeneticAlgo>();
        if (!directionalLight)
        {
            directionalLight = GetComponent<Light>();
        }
    }

    public bool IsDaytime() => isDay;
    
    public float GetDayNightCycle() => isDay ? 
        timer / dayDuration : 
        timer / nightDuration;

    void Update()
    {
        // Handle day/night cycle
        timer += deltaTime;
        if (isDay)
        {
            if (timer >= dayDuration)
            {
                isDay = false;
                timer = 0;
                OnNightStart?.Invoke();
            }
        }
        else
        {
            if (timer >= nightDuration)
            {
                isDay = true;
                timer = 0;
                OnDayStart?.Invoke();
            }
        }

        UpdateLighting();
    }

    // Add events that other scripts can subscribe to
    public delegate void TimeOfDayChange();
    public event TimeOfDayChange OnDayStart;
    public event TimeOfDayChange OnNightStart;

    private void UpdateLighting()
    {
        // Move existing lighting code here
        float cycleProgress = GetDayNightCycle();

        if (isDay)
        {
            directionalLight.color = Color.Lerp(nightColor, dayColor, cycleProgress);
            directionalLight.intensity = Mathf.Lerp(nightIntensity, dayIntensity, cycleProgress);
            RenderSettings.skybox = daySkybox;
        }
        else
        {
            directionalLight.color = Color.Lerp(dayColor, nightColor, cycleProgress);
            directionalLight.intensity = Mathf.Lerp(dayIntensity, nightIntensity, cycleProgress);
            RenderSettings.skybox = nightSkybox;
        }

    }
} 