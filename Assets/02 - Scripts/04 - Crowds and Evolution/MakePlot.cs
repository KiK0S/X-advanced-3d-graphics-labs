using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MakePlot : MonoBehaviour
{
    public GameObject pointPrefab;  // Prefab for the point (a UI Image)
    public RectTransform canvasRectTransform;  // Canvas RectTransform
    private List<GameObject> pointInstances = new List<GameObject>();
    private List<int> dataPoints = new List<int>();
    private GeneticAlgo geneticAlgo;
    protected int timer = 0;
    // Start is called before the first frame update
    void Start()
    {
        geneticAlgo = FindObjectOfType<GeneticAlgo>();
    }

    // Update is called once per frame
    void Update()
    {
        if (timer++ % 20 == 0) {
            dataPoints.Add(geneticAlgo.getAnimalNum());
            UpdatePlot();
        }
    }

    public void UpdatePlot(bool erase_only=false) {
        if (dataPoints.Count < 2)
            return;

        Vector3[] positions = new Vector3[dataPoints.Count];
        
        foreach (GameObject point in pointInstances)
        {
            Destroy(point);
        }
        
        if (erase_only)
            return;
        
        while (dataPoints.Count > 100) {
            dataPoints.RemoveAt(0);
        }

        int max_cnt = dataPoints[0];
        foreach (int x in dataPoints) {
            max_cnt = Math.Max(max_cnt, x);
        }
 
        for (int i = 0; i < dataPoints.Count; i++)
        {
            float y = Mathf.Lerp(0f, canvasRectTransform.rect.height, (float)dataPoints[i] / max_cnt);
            float x = Mathf.Lerp(0f, canvasRectTransform.rect.width, (float)i / (dataPoints.Count - 1));
            
            GameObject pointInstance = Instantiate(pointPrefab, canvasRectTransform);

            pointInstances.Add(pointInstance);
            pointInstance.GetComponent<RectTransform>().anchoredPosition = new Vector2(x, y);
        }
    }
}
