using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SimpleNeuralNet
{

    private List<float[,]> allWeights;
    private List<float[]> allResults;

    public SimpleNeuralNet(SimpleNeuralNet other)
    {
        allWeights = new List<float[,]>();
        allResults = new List<float[]>();

        for (int i = 0; i < other.allWeights.Count; i++)
        {
            allWeights.Add((float[,])other.allWeights[i].Clone());
        }
        for (int i = 0; i < other.allResults.Count; i++)
        {
            allResults.Add((float[])other.allResults[i].Clone());
        }
    }

    public SimpleNeuralNet(int[] structure)
    {
        allWeights = new List<float[,]>();
        allResults = new List<float[]>();
        
        allResults.Add(new float[structure[0]]);

        for (int i = 1; i < structure.Length; i++)
        {
            float[,] weights = makeLayer(structure[i - 1], structure[i]);
            allWeights.Add(weights);
            float[] results = new float[structure[i]];
            allResults.Add(results);
        }
    }

    private float initWeight()
    {
        // Box-Muller transform to generate normal distribution
        float u1 = UnityEngine.Random.value;
        float u2 = UnityEngine.Random.value;
        
        // Using standard normal distribution (mean = 0, std = 1)
        float randStdNormal = ParameterManager.Instance.stdInitialWeight * Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2) + ParameterManager.Instance.meanInitialWeight;
        
        // Scale the weights - using Xavier initialization
        // The 0.1f factor helps prevent initial saturation of neurons
        return randStdNormal * 0.1f;
    }

    private float[,] makeLayer(int input, int numberNodes)
    {

        // Weights: bias + input x neurons
        float[,] weights = new float[input + 1, numberNodes];
        for (int i = 0; i < weights.GetLength(0); i++)
        {
            for (int j = 0; j < weights.GetLength(1); j++)
            {
                weights[i, j] = initWeight();
            }
        }
        return weights;
    }

    public float[] getOutput(float[] input)
    {
        allResults[0] = (float[])input.Clone();
        for (int idxLayer = 0; idxLayer < allWeights.Count; idxLayer++)
        {
            float[,] weights = allWeights[idxLayer];
            float[] ins = allResults[idxLayer];
            float[] outs = allResults[idxLayer + 1];

            for (int idxNeuron = 0; idxNeuron < outs.Length; idxNeuron++)
            {
                float sum = weights[0, idxNeuron]; // Add bias
                for (int input_i = 0; input_i < ins.Length; input_i++)
                {
                    sum += ins[input_i] * weights[input_i + 1, idxNeuron];
                }
                outs[idxNeuron] = transferFunction(sum, idxLayer); // Apply transfer function
            }
        }
        return allResults[allResults.Count - 1]; // Return final result
    }

    private float sigmoid(float value)
    {
        return 1.0f / (1.0f + Mathf.Exp(-value));
    }

    private float transferFunction(float value, int idxLayer)
    {
        if (idxLayer + 2 == allResults.Count)
            return sigmoid(value);
        if (ParameterManager.Instance.activationFunction == ParameterManager.ActivationFunction.Sigmoid)
            return sigmoid(value);
        else if (ParameterManager.Instance.activationFunction == ParameterManager.ActivationFunction.ReLU)
            return Mathf.Max(0, value);
        else
            return value;
    }

    // Randomly change network weights
    // Swap: completely change a weight to a value between [-1;1]*swap_strength
    // Eps: change a weight by adding a value between [-1;1]*eps_strength
    public void mutate(float swap_rate, float eps_rate, float eps_strength)
    {
        foreach (float[,] weights in allWeights)
        {
            for (int i = 0; i < weights.GetLength(0); i++)
            {
                for (int j = 0; j < weights.GetLength(1); j++)
                {
                    float rand = UnityEngine.Random.value;
                    if (rand < swap_rate)
                    {
                        weights[i, j] = initWeight();
                    }
                    else if (rand < swap_rate + eps_rate)
                    {
                        weights[i, j] += initWeight() * eps_strength;
                    }
                }
            }
        }
    }

    public string SerializeWeights() {
        string json = "{\"weights\":[";
        for (int idxLayer = 0; idxLayer < allWeights.Count; idxLayer++)
        {
            float[,] weights = allWeights[idxLayer];
            for (int i = 0; i < weights.GetLength(0); i++)
            {
                json += "[";
                for (int j = 0; j < weights.GetLength(1); j++)
                {
                    json += weights[i, j].ToString();
                    if (j < weights.GetLength(1) - 1)
                        json += ",";
                }
                json += "]";
            }
            if (idxLayer < allWeights.Count - 1)
                json += ",";
        }
        json += "]}";
        return json;
    }

    public int[] GetStructure()
    {
        int[] structure = new int[allResults.Count];
        for (int i = 0; i < allResults.Count; i++)
        {
            structure[i] = allResults[i].Length;
        }
        
        return structure;
    }

    public int GetOutputSize()
    {
        return allResults[allResults.Count - 1].Length;
    }

    public List<float[]> GetAllLayerValues()
    {
        List<float[]> values = new List<float[]>();
        foreach (var results in allResults)
        {
            values.Add((float[])results.Clone());
        }
        return values;
    }
    public List<float[,]> GetWeights()
    {
        return allWeights;
    }
}
