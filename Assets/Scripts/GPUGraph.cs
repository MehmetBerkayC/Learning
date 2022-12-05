using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUGraph : MonoBehaviour
{
    const int maxResolution = 1000;

    [SerializeField, Range(10, maxResolution)] int resolution = 10; // Choose the point amount
    [SerializeField] FunctionLibrary.FunctionName function;

    // Automatic Function Changing - Transitioning
    [SerializeField, Min(0f)] float functionDuration = 1f, transitionDuration = 1f;
    public enum TransitionMode { Cycle, Random }
    [SerializeField] TransitionMode transitionMode;


    // Mesh, Material
    [SerializeField] Material material;
    [SerializeField] Mesh mesh;

    float duration;
    bool transitioning;
    FunctionLibrary.FunctionName transitionFunction;

    // Compute Shader Stuff
    [SerializeField] ComputeShader computeShader;
    ComputeBuffer positionsBuffer;

    static readonly int positionsId  = Shader.PropertyToID("_Positions"),
                        resolutionId = Shader.PropertyToID("_Resolution"),
                        stepId       = Shader.PropertyToID("_Step"),
                        timeId       = Shader.PropertyToID("_Time");

    void UpdateFunctionOnGPU()
    {
        float step = 2f / resolution;
        computeShader.SetInt(resolutionId, resolution);
        computeShader.SetFloat(stepId, step);
        computeShader.SetFloat(timeId, Time.time);

        var kernelIndex = (int)function;

        computeShader.SetBuffer(kernelIndex, positionsId, positionsBuffer);

        int groups = Mathf.CeilToInt(resolution / 8f);
        computeShader.Dispatch(kernelIndex, groups, groups, 1);

        material.SetBuffer(positionsId, positionsBuffer);
        material.SetFloat(stepId, step);

        var bounds = new Bounds(Vector3.zero, Vector3.one * (2f + 2f / resolution));
        Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, resolution * resolution);
    }


    /*
     * This gets us a compute buffer, but these objects do not survive hot reloads, which means that if we change code while in play mode it will disappear.
     * We can deal with this by replacing the Awake method with an OnEnable method, which gets invoked each time the component is enabled.
     * This happens right after it awakens—unless it's disabled—and also after a hot reload is completed.
    */
    private void OnEnable()
    {
        /* We need to pass the amount of elements of the buffer as an argument, 
         * which is the resolution squared, just like for the positions array of Graph.
         * A compute buffer contains arbitrary untyped data. 
         * We have to specify the exact size of each element in bytes, via a second argument.
         * We need to store 3D position vectors, which consist of three float numbers, so the element size is three times four bytes(1 int is 4 bytes). 
         */
        positionsBuffer = new ComputeBuffer(maxResolution * maxResolution, 3 * 4); // something like (total unit count, 1 unit size in bytes)    
    }

    /*
     * We should add a companion OnDisable method. 
     * Which gets invoked when the component is disabled, which also happens if the graph is destroyed and right before a hot reload.
     * Have it release the buffer, by invoking its Release method. 
     * This indicates that the GPU memory claimed by the buffer can be freed immediately.
     /// ***** ///
     * Explicitly setting the field to reference null.
     * This makes it possible for the object to be reclaimed by Unity's memory garbage collection process the next time it runs,
     * if our graph gets disabled or destroyed while in play mode.
     */
    private void OnDisable()
    {
        positionsBuffer.Release();
        
        positionsBuffer = null;
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

        UpdateFunctionOnGPU();

    }

    void PickNextFunction()
    {
        function = transitionMode == TransitionMode.Cycle ?
            FunctionLibrary.GetNextFunctionName(function) :
            FunctionLibrary.GetRandomFunctionNameOtherThan(function);
    }

}
