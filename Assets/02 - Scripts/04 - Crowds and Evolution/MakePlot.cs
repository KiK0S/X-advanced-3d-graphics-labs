using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakePlot : MonoBehaviour
{
    public GameObject pointPrefab;  // Prefab for the point (a UI Image)
    public RectTransform canvasRectTransform;  // Canvas RectTransform
    
    [System.Serializable]
    public class DataStream
    {
        public string name;
        public Color color;
        public List<float> values = new List<float>();
    }
    
    public List<DataStream> dataStreams = new List<DataStream>();
    private List<GameObject> pointInstances = new List<GameObject>();
    private GeneticAlgo geneticAlgo;
    private CustomTerrain customTerrain;
    protected int timer = 0;

    void Start()
    {
        geneticAlgo = FindObjectOfType<GeneticAlgo>();
        customTerrain = FindObjectOfType<CustomTerrain>();
        
        // Initialize data streams
        dataStreams.Add(new DataStream { 
            name = "Animals", 
            color = Color.green 
        });
        dataStreams.Add(new DataStream { 
            name = "Grass", 
            color = Color.red 
        });
    }

    void Update()
    {
        if (timer++ % 20 == 0) {
            // Update animal count
            dataStreams[0].values.Add(geneticAlgo.getAnimalNum());
            
            // Update grass count
            int grassCount = 0;
            int[,] details = customTerrain.getDetails();
            for (int i = 0; i < details.GetLength(0); i++) {
                for (int j = 0; j < details.GetLength(1); j++) {
                    if (details[i, j] > 0) grassCount++;
                }
            }
            dataStreams[1].values.Add(grassCount);
            
            UpdatePlot();
        }
    }

    public void UpdatePlot(bool erase_only=false) {
        // Clear existing points
        foreach (GameObject point in pointInstances)
        {
            Destroy(point);
        }
        pointInstances.Clear();
        
        if (erase_only || dataStreams[0].values.Count < 2)
            return;

        // Trim data points if too many
        foreach (var stream in dataStreams) {
            while (stream.values.Count > 100) {
                stream.values.RemoveAt(0);
            }
        }

        // Plot each data stream with its own normalization
        foreach (var stream in dataStreams) {
            // Find maximum value for this stream
            float maxValue = float.MinValue;
            foreach (float value in stream.values) {
                maxValue = Mathf.Max(maxValue, value);
            }
            
            // Plot points for this stream
            for (int i = 0; i < stream.values.Count; i++)
            {
                float y = Mathf.Lerp(0f, canvasRectTransform.rect.height, stream.values[i] / maxValue);
                float x = Mathf.Lerp(0f, canvasRectTransform.rect.width, (float)i / (stream.values.Count - 1));
                
                GameObject pointInstance = Instantiate(pointPrefab, canvasRectTransform);
                pointInstances.Add(pointInstance);
                
                // Set point color
                UnityEngine.UI.Image image = pointInstance.GetComponent<UnityEngine.UI.Image>();
                if (image != null) {
                    image.color = stream.color;
                }
                
                pointInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
            }
        }
    }
}
