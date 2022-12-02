using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Graph : MonoBehaviour
{
    [SerializeField] Transform pointPrefab;
    [SerializeField, Range(10,100)] int resolution = 10; // Choose the point amount
    [SerializeField] FunctionLibrary.FunctionName function;

    // Automatic Function Changing - Transitioning
    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    public enum TransitionMode { Cycle, Random }
    [SerializeField] TransitionMode transitionMode;

    Transform[] points;

    float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;


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

        if (transitioning) // already transitioning, we need to stop this
        {
            if (duration >= transitionDuration) // When we exceed the transitionDuration
            {
                // deduct the extra time (function switches) from the duration of the next function
                duration -= transitionDuration;
                transitioning = false;
            }
        }
        else if (duration >= functionDuration) // we are displaying the function, begin transition 
        {
            // deduct the extra time (function switches) from the duration of the next function
            duration -= functionDuration;

            transitioning = true;
            transitionFunction = function;

            PickNextFunction();
        }

        /* Once the duration exceeds the function duration we move on to the next one. 
        * Before picking the next function indicate that we're transitioning and make the transition function equal to the current function.
        * But if we're already transitioning we have to do something else. So first check whether we're transitioning.
        * Only if that's not the case do we have to check whether we exceeded the function duration.
        * If we are transitioning then we have to check whether we exceeded the transition duration.
        * If so deduct the transition duration from the current duration and switch back to the single function mode.
        */

        if (transitioning) // Whenever transitioning
        {
            UpdateFunctionTransition(); // Display with transition
        }
        else
        {
            UpdateFunction(); // Display with no transition
        }
        
    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
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

    void UpdateFunctionTransition() // Updates the current graph on display
    {
        FunctionLibrary.Function
            from = FunctionLibrary.GetFunction(transitionFunction),
            to = FunctionLibrary.GetFunction(function);

        float progress = duration / transitionDuration;
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

            points[i].localPosition = FunctionLibrary.Morph(u, v, time, from, to, progress);
        }
    }
}
