using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;

public class UIManager : MonoBehaviour
{
    public Dropdown animalSelector;
    public Button saveWeightsButton;
    public Button saveScreenshotButton;
    public Button followButton;
    public Camera mainCamera;
    public Camera followingCamera;
    
    private Animal[] animals;
    private GeneticAlgo geneticAlgo;
    private Transform cameraOriginalTransform;
    private bool isFollowing = false;
    private Transform targetToFollow;

    void Start()
    {
        geneticAlgo = FindObjectOfType<GeneticAlgo>();
        if (mainCamera != null)
        {
            cameraOriginalTransform = mainCamera.transform;
        }
        SetupUI();
        InvokeRepeating("UpdateAnimalList", 0f, 5f);
    }

    void SetupUI()
    {
        if (saveWeightsButton != null)
        {
            saveWeightsButton.onClick.AddListener(SaveSelectedAnimalWeights);
        }

        if (saveScreenshotButton != null)
        {
            saveScreenshotButton.onClick.AddListener(TakeScreenshot);
        }

        if (animalSelector != null)
        {
            animalSelector.onValueChanged.AddListener(OnAnimalSelected);
            UpdateAnimalList();
        }

        if (followButton != null)
        {
            followButton.onClick.AddListener(ToggleFollowSelected);
        }
    }

    public void UpdateAnimalList()
    {
        if (animalSelector == null) return;

        animals = FindObjectsOfType<Animal>();
        animalSelector.ClearOptions();

        for (int i = 0; i < animals.Length; i++)
        {
            animalSelector.options.Add(new Dropdown.OptionData($"Animal {i} (Gen {animals[i].generation})"));
            if (isFollowing && animals[i].transform == targetToFollow) {
                animalSelector.value = i;
            }
        }

        
        animalSelector.RefreshShownValue();
    }

    void OnAnimalSelected(int index)
    {
        if (isFollowing && index >= 0 && index < animals.Length)
        {
            targetToFollow = animals[index].transform;
        }
    }

    void SaveSelectedAnimalWeights()
    {
        if (animalSelector.value >= 0 && animalSelector.value < animals.Length)
        {
            Animal selectedAnimal = animals[animalSelector.value];
            string weights = selectedAnimal.ExportNetworkWeights();
            
            string path = Path.Combine(Application.persistentDataPath, 
                $"animal_weights_gen{selectedAnimal.generation}_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            
            File.WriteAllText(path, weights);
            Debug.Log($"Weights saved to: {path}");
        }
    }

    void TakeScreenshot()
    {
        string folderPath = Path.Combine(Application.persistentDataPath, "Screenshots");
        Directory.CreateDirectory(folderPath);
        
        string filename = Path.Combine(folderPath, $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        ScreenCapture.CaptureScreenshotAsTexture();
        Debug.Log($"Screenshot saved to: {filename}");
    }

    void ToggleFollowSelected()
    {
        if (!isFollowing)
        {
            // Start following
            if (animalSelector.value >= 0 && animalSelector.value < animals.Length)
            {
                targetToFollow = animals[animalSelector.value].transform;
                isFollowing = true;
                followButton.GetComponentInChildren<Text>().text = "Stop Following";
                
                mainCamera.enabled = false;
                followingCamera.enabled = true;
            }
        }
        else
        {
            // Stop following
            isFollowing = false;
            followButton.GetComponentInChildren<Text>().text = "Follow Animal";
            if (followingCamera != null && cameraOriginalTransform != null)
            {
                followingCamera.transform.position = cameraOriginalTransform.position;
                followingCamera.transform.rotation = cameraOriginalTransform.rotation;
            }
            mainCamera.enabled = true;
            followingCamera.enabled = false;

        }
    }

    void CheckAndSwitchTarget()
    {
        if (targetToFollow == null || !targetToFollow.gameObject.activeInHierarchy)
        {
            UpdateAnimalList();
            // Find a new random living animal
            if (animals.Length > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, animals.Length);
                targetToFollow = animals[randomIndex].transform;
                
                // Update the dropdown to reflect the new selection
                animalSelector.value = randomIndex;
                animalSelector.RefreshShownValue();
                
                Debug.Log($"Switched to following Animal {randomIndex} because previous target died");
            }
            else
            {
                // No animals left, stop following
                isFollowing = false;
            }
        }
    }

    void LateUpdate()
    {
        if (isFollowing && followingCamera != null)
        {
            CheckAndSwitchTarget();
            
            if (targetToFollow != null)
            {
                Vector3 targetPosition = targetToFollow.position;
                followingCamera.transform.position = Vector3.Lerp(followingCamera.transform.position, targetPosition + new Vector3(0, 10, -10), Time.deltaTime * 5f);
                followingCamera.transform.LookAt(targetToFollow);
            }
        }
    }
} 