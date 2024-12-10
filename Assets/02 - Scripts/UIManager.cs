using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public Dropdown animalSelector;
    public Button saveWeightsButton;
    public Button saveScreenshotButton;
    public Button followButton;
    public Camera mainCamera;
    public Camera followingCamera;
    public GameObject eyeVisualizerPanel;
    public Text outputText;
    
    private Animal[] animals;
    private GeneticAlgo geneticAlgo;
    private Transform cameraOriginalTransform;
    private bool isFollowing = false;
    private Transform targetToFollow;
    private List<Image[]> networkLayers;
    private List<List<LineRenderer>> networkConnections;
    public float nodeSize = 10f;
    public float horizontalSpacing = 100f;
    public float verticalSpacing = 15f;
    private Animal selectedAnimal;

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
        if (eyeVisualizerPanel != null && networkLayers == null && selectedAnimal != null)
        {
            CreateNetworkVisualizer(selectedAnimal.GetBrain());
        }
    }

    public void UpdateAnimalList()
    {
        if (animalSelector == null) return;

        animals = FindObjectsOfType<Animal>();
        Array.Sort(animals, (a, b) => b.timeOfLife.CompareTo(a.timeOfLife));
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
        if (index >= 0 && index < animals.Length)
        {
            selectedAnimal = animals[index];
            if (isFollowing)
            {
                targetToFollow = selectedAnimal.transform;
            }
            
            // Create network visualizer if it doesn't exist
            if (eyeVisualizerPanel != null && networkLayers == null)
            {
                CreateNetworkVisualizer(selectedAnimal.GetBrain());
            }
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

        // Update network visualizer and output text
        if (selectedAnimal != null && selectedAnimal.gameObject.activeInHierarchy)
        {
            UpdateNetworkVisualizer();
            // UpdateNetworkConnections();
            UpdateOutputText();
        }
    }

    private void CreateNetworkVisualizer(SimpleNeuralNet brain)
    {
        // Clean up existing visualizer
        if (networkLayers != null)
        {
            foreach (var layer in networkLayers)
            {
                foreach (var node in layer)
                {
                    if (node != null)
                        Destroy(node.gameObject);
                }
            }
        }

        if (networkConnections != null)
        {
            foreach (var layerConnections in networkConnections)
            {
                foreach (var line in layerConnections)
                {
                    if (line != null)
                        Destroy(line.gameObject);
                }
            }
        }

        networkLayers = new List<Image[]>();
        networkConnections = new List<List<LineRenderer>>();
        
        int[] structure = brain.GetStructure();
        float startX = -((structure.Length - 1) * horizontalSpacing) / 2f;
        
        // Create nodes for each layer
        for (int layerIdx = 0; layerIdx < structure.Length; layerIdx++)
        {
            int nodeCount = structure[layerIdx];
            Image[] layerNodes = new Image[nodeCount];
            float startY = -(nodeCount * verticalSpacing) / 2f;

            for (int nodeIdx = 0; nodeIdx < nodeCount; nodeIdx++)
            {
                GameObject nodeObj = new GameObject($"Node_L{layerIdx}_N{nodeIdx}");
                nodeObj.transform.SetParent(eyeVisualizerPanel.transform, false);
                
                Image nodeImage = nodeObj.AddComponent<Image>();
                nodeImage.rectTransform.sizeDelta = new Vector2(nodeSize, nodeSize);
                nodeImage.rectTransform.anchoredPosition = new Vector2(
                    startX + (layerIdx * horizontalSpacing),
                    startY + (nodeIdx * verticalSpacing)
                );
                
                layerNodes[nodeIdx] = nodeImage;
            }
            
            networkLayers.Add(layerNodes);

            // // Create connections to previous layer
            // if (layerIdx > 0)
            // {
            //     List<LineRenderer> layerConnections = new List<LineRenderer>();
            //     Image[] prevLayer = networkLayers[layerIdx - 1];
            //     Image[] currentLayer = networkLayers[layerIdx];

            //     for (int prevIdx = 0; prevIdx < prevLayer.Length; prevIdx++)
            //     {
            //         for (int currIdx = 0; currIdx < currentLayer.Length; currIdx++)
            //         {
            //             GameObject lineObj = new GameObject($"Connection_L{layerIdx-1}_{prevIdx}_to_L{layerIdx}_{currIdx}");
            //             lineObj.transform.SetParent(eyeVisualizerPanel.transform, false);
                        
            //             LineRenderer line = lineObj.AddComponent<LineRenderer>();
            //             line.sortingOrder = 1;
            //             line.positionCount = 2;
            //             line.startWidth = 0.5f;
            //             line.endWidth = 0.5f;
            //             line.material = new Material(Shader.Find("Sprites/Default"));
                        
            //             layerConnections.Add(line);
            //         }
            //     }
            //     networkConnections.Add(layerConnections);
            // }
        }
    }

    private void UpdateNetworkVisualizer()
    {
        if (networkLayers == null || selectedAnimal == null) return;

        // Get all layer values from the brain
        var layerValues = selectedAnimal.GetAllLayerValues();
        
        // Debug.Log(layerValues);

        // Update node colors based on activation values
        for (int layerIdx = 0; layerIdx < networkLayers.Count && layerIdx < layerValues.Count; layerIdx++)
        {
            var layer = networkLayers[layerIdx];
            var values = layerValues[layerIdx];
            
            for (int nodeIdx = 0; nodeIdx < layer.Length && nodeIdx < values.Length; nodeIdx++)
            {
                float value = values[nodeIdx];
                // Use a gradient from black (0) to green (1)
                layer[nodeIdx].color = new Color(0, value, 0, 1);
            }
        }
    }

    private void UpdateNetworkConnections()
    {
        if (networkConnections == null || selectedAnimal == null) return;

        var weights = selectedAnimal.GetBrain().GetWeights();
        
        for (int layerIdx = 0; layerIdx < networkConnections.Count; layerIdx++)
        {
            var connections = networkConnections[layerIdx];
            var layerWeights = weights[layerIdx];
            var prevLayer = networkLayers[layerIdx];
            var currentLayer = networkLayers[layerIdx + 1];
            int connectionIdx = 0;

            for (int prevIdx = 0; prevIdx < prevLayer.Length; prevIdx++)
            {
                for (int currIdx = 0; currIdx < currentLayer.Length; currIdx++)
                {
                    var line = connections[connectionIdx];
                    float weight = layerWeights[prevIdx + 1, currIdx]; // +1 to skip bias

                    // Set line positions
                    Vector3 startPos = prevLayer[prevIdx].transform.position;
                    Vector3 endPos = currentLayer[currIdx].transform.position;
                    line.SetPosition(0, startPos);
                    line.SetPosition(1, endPos);

                    // Set line color and width based on weight
                    float absWeight = Mathf.Abs(weight);
                    line.startWidth = 0.1f;
                    line.endWidth = 0.1f;
                    
                    // Green for positive weights, red for negative
                    Color lineColor = weight > 0 ? 
                        new Color(0, absWeight, 0, 1) : 
                        new Color(absWeight, 0, 0, 1);
                    line.startColor = lineColor;
                    line.endColor = lineColor;

                    connectionIdx++;
                }
            }
        }
    }

    private void UpdateOutputText()
    {
        if (outputText != null && selectedAnimal != null)
        {
            var outputs = selectedAnimal.GetLastOutputs();
            if (outputs != null)
            {
                outputText.text = "Outputs: [" + string.Join(", ", Array.ConvertAll(outputs, x => $"{x:F2}")) + "]\n";
            }
            outputText.text += $"Health: {selectedAnimal.GetHealth():F2}\n";
        }
    }
} 