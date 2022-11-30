using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10,100)] int resolution = 10; // Choose the point amount
    [SerializeField] FunctionLibrary.FunctionName function;

    // Automatic Function Changing
    [SerializeField, Min(0f)] float functionDuration = 1f;
    
    Transform[] points;

    float duration;

    private void Awake()
    {
        float step = 2f / resolution; // keep graph scale inside [-1,1]

        var scale = Vector3.one * step;

        // Make a list to store points with given resolution(total points count)
        points = new Transform[resolution * resolution];

        for (int i = 0; i < points.Length; i++)
        {
            // create point, store it inside the list
            Transform point = points[i] = Instantiate(pointPrefab);

            // scale the point
            point.localScale = scale;

            point.SetParent(transform, false); // set the point as a children of the Graph object

        }
    }

    // Update is called once per frame
    void Update()
    {
        duration += Time.deltaTime;
        if(duration >= functionDuration)
        {
            // deduct the extra time (function switches) from the duration of the next function
            duration -= functionDuration;
            function = FunctionLibrary.GetNextFunctionName(function);
        }

        UpdateFunction();
    }

    void UpdateFunction() // Updates the current graph on display
    {
        FunctionLibrary.Function f = FunctionLibrary.GetFunction(function);

        float time = Time.time;
        float step = 2f / resolution;

        // we only need to recalculate v when z changes
        float v = 0.5f * step - 1f;

        for (int i = 0, x = 0, z = 0; i < points.Length; i++, x++)
        {

            if (x == resolution)
            {
                x = 0;
                z += 1;
                v = (z + 0.5f) * step - 1f;
            }

            float u = (x + 0.5f) * step - 1f;

            points[i].localPosition = f(u, v, time);
        }
    }
}
