using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;

public class Fractal : MonoBehaviour
{
    // Delay Unity until burst is complete
    [BurstCompile(CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor {

        public float spinAngleDelta;
        public float scale;
        
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;

        [WriteOnly] 
        public NativeArray<Matrix4x4> matrices;

        // i works the same way as a for loop iterator
        public void Execute(int i) {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            //part.rotation *= deltaRotation;
            part.spinAngle += spinAngleDelta;
            part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
            part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);
            parts[i] = part;
            matrices[i] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
        }
    }

    // While using Jobs
    NativeArray<FractalPart>[] parts;
    NativeArray<Matrix4x4>[] matrices;

    //FractalPart[][] parts;
    //Matrix4x4[][] matrices;

    // Rather than have each part update itself,
    // we'll instead control the entire fractal from the single root object
    // https://catlikecoding.com/unity/tutorials/basics/jobs/#:~:text=entering%20play%20mode.-,Storing%20Information,-Rather%20than%20have
    struct FractalPart
    {
        public Vector3 direction, worldPosition;
        public Quaternion rotation, worldRotation;
        public float spinAngle;
    }

    [SerializeField, Range(1, 8)] int depth = 4;
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;


    static readonly int matricesId = Shader.PropertyToID("_Matrices");

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    FractalPart CreatePart(int childIndex) => new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex]
        };

    ComputeBuffer[] matricesBuffers;
    static MaterialPropertyBlock propertyBlock;
    private void OnEnable()
    {
        // While using Jobs
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<Matrix4x4>[depth];


        //parts = new FractalPart[depth][];
        //matrices = new Matrix4x4[depth][];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
    
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // There will be 5 children of each clone so length * 5
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            // We need a transformation matrix to store the world pos, rotation etc...
            matrices[i] = new NativeArray<Matrix4x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        // To create and name each child
        parts[0][0] = CreatePart(0); // Root

        // li -> level iterator, fpi -> fractal parts iterator, ci -> child iterator
        for (int li = 1; li < parts.Length; li++)
        {
            // While using Jobs
            NativeArray<FractalPart> levelParts = parts[li];
            
            //FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }

        // Assigning something only when the current value is null,
        // can be simplified to a single expression by using the ??= null-coalescing assignment
        //propertyBlock ??= new MaterialPropertyBlock();
        // or 
        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }
    }
    void OnDisable()
    {
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            matricesBuffers[i].Release();
            // While using Jobs
            parts[i].Dispose();
            matrices[i].Dispose();
        }

        parts = null;
        matrices = null;
        matricesBuffers = null;
    }
    void OnValidate()
    {
        if (parts != null && enabled)
        {
            OnDisable();
            OnEnable();
        }
    }

    private void Update()
    {
        //Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle = spinAngleDelta;
        //rootPart.rotation *= deltaRotation;
        rootPart.worldRotation = transform.rotation * (rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f));
        rootPart.worldPosition = transform.position;
        
        float objectScale = transform.lossyScale.x;
        parts[0][0] = rootPart;
        matrices[0][0] = Matrix4x4.TRS(rootPart.worldPosition, rootPart.worldRotation, objectScale * Vector3.one);

        float scale = objectScale;

        // While using Jobs
        JobHandle jobHandle = default;
        // li -> level iterator, fpi -> fractal parts iterator
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            // While using Jobs
            NativeArray<FractalPart> parentParts = parts[li - 1];
            NativeArray<FractalPart> levelParts = parts[li];
            NativeArray<Matrix4x4> levelMatrices = matrices[li];

            jobHandle = new UpdateFractalLevelJob
            {
                spinAngleDelta = spinAngleDelta,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.Schedule(parts[li].Length, jobHandle);

            /* Schedule doesn't immediately run the job, it only schedules it for later processing.
             * It returns a JobHandle value that can be used to track the job's progress. 
             * We can delay further execution of our code until the job is finished by invoking Complete on the handle. */
            // job.Schedule(parts[li].Length, default).Complete(); similar
            
            // While not using Jobs
            //FractalPart[] parentParts = parts[li - 1];
            //FractalPart[] levelParts = parts[li];
            //Matrix4x4[] levelMatrices = matrices[li];

            //for (int fpi = 0; fpi < levelParts.Length; fpi++)
            //{
            //    FractalPart parent = parentParts[fpi / 5];
            //    FractalPart part = levelParts[fpi];
            //    //part.rotation *= deltaRotation;
            //    part.spinAngle += spinAngleDelta;
            //    part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
            //    part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);
            //    levelParts[fpi] = part;
            //    levelMatrices[fpi] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
            //}
        }
        // While using Jobs
        jobHandle.Complete();

        /// Send matrix data to compute shader
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);
            propertyBlock.SetBuffer(matricesId, buffer);
            Graphics.DrawMeshInstancedProcedural(mesh, 0, material, bounds, buffer.count, propertyBlock);
        }

    }

    /*
        // Bad performance after depth 6 (CPU will bottleneck) *Needs mesh renderer + filter
        // Awake() will run its code block every time the object it is attached to is cloned
        // This code can run tenths or hundreds of times in 1 frame -> Quicker memory overload than Start
        // Start() will run its code block every frame after the object its attached to is cloned
        // This code can run once in every frame
        private void Start()
        {
            // Name the new object
            name = "Fractal " + depth;

            // When enough instances created, stop
            if(depth <= 1)
            {
                return;
            }

            Fractal childRight = CreateChild(Vector3.right, Quaternion.identity);
            Fractal childUp = CreateChild(Vector3.up, Quaternion.Euler(0f, 0f, -90f));
            Fractal childLeft = CreateChild(Vector3.left, Quaternion.Euler(0f, 0f, 90f));
            Fractal childForward = CreateChild(Vector3.forward, Quaternion.Euler(90f, 0f, 0f));
            Fractal childBack = CreateChild(Vector3.back, Quaternion.Euler(-90f, 0f, 0f));

            childRight.transform.SetParent(transform, false);
            childUp.transform.SetParent(transform, false);
            childLeft.transform.SetParent(transform, false);
            childForward.transform.SetParent(transform, false);
            childBack.transform.SetParent(transform, false);

        }


        private void Update()
        {
            transform.Rotate(0f, 22.5f * Time.deltaTime, 0f);
        }

        Fractal CreateChild (Vector3 direction, Quaternion rotation)
        {
            // Makes a clone of itself
            Fractal child = Instantiate(this);
            child.depth = depth - 1;
            child.transform.localPosition = 0.75f * direction;
            child.transform.localRotation = rotation;
            child.transform.localScale = 0.5f * Vector3.one;
            return child;
        }
    */
}
