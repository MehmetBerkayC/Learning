using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Random = UnityEngine.Random;

using static Unity.Mathematics.math;
using float4x4 = Unity.Mathematics.float4x4;
using quaternion = Unity.Mathematics.quaternion;
using float3 = Unity.Mathematics.float3;
using float3x3 = Unity.Mathematics.float3x3;
using float3x4 = Unity.Mathematics.float3x4;

public class Fractal : MonoBehaviour
{
    /// 
    ///  Fields And Structs
    /// 

    // Delay Unity until burst is complete
    [BurstCompile(FloatPrecision.Standard, FloatMode.Fast, CompileSynchronously = true)]
    struct UpdateFractalLevelJob : IJobFor {

        public float deltaTime;
        public float scale;
        
        [ReadOnly]
        public NativeArray<FractalPart> parents;
        public NativeArray<FractalPart> parts;

        [WriteOnly] 
        public NativeArray<float3x4> matrices;

        // i works the same way as a for loop iterator
        public void Execute(int i) {
            FractalPart parent = parents[i / 5];
            FractalPart part = parts[i];
            part.spinAngle += part.spinVelocity * deltaTime;

            // Sagging
            float3 upAxis = mul(mul(parent.worldRotation, part.rotation), up());
            float3 sagAxis = cross(up(), upAxis);
            float sagMagnitude = length(sagAxis);
            quaternion baseRotation;
            
            if (sagMagnitude > 0f)
            {
                sagAxis /= sagMagnitude;
                quaternion sagRotation = quaternion.AxisAngle(sagAxis, part.maxSagAngle * sagMagnitude);
                baseRotation = mul(sagRotation, parent.worldRotation);
            }
            else
            {
                baseRotation = parent.worldRotation;
            }

            part.worldRotation = mul(baseRotation, mul(part.rotation, quaternion.RotateY(part.spinAngle)));
            part.worldPosition = parent.worldPosition + mul(part.worldRotation, float3(0f, 1.5f * scale, 0f));
            parts[i] = part;

            float3x3 r = float3x3(part.worldRotation) * scale;
            matrices[i] = float3x4(r.c0, r.c1, r.c2, part.worldPosition);
        }
    }

    NativeArray<FractalPart>[] parts;
    NativeArray<float3x4>[] matrices;

    [SerializeField, Range(3, 8)] int depth = 4;
    
    [SerializeField] Mesh mesh, leafMesh;
    [SerializeField] Material material;

    [SerializeField] Gradient gradientA, gradientB;
    [SerializeField] Color leafColorA, leafColorB;

    [SerializeField, Range(0f, 90f)] float maxSagAngleA = 15f, maxSagAngleB = 25f;
    [SerializeField, Range(0f, 90f)] float spinSpeedA = 20f, spinSpeedB = 25f;
    [SerializeField, Range(0f, 1f)] float reverseSpinChance = 0.25f;

    static readonly int
        colorAId = Shader.PropertyToID("_ColorA"),
        colorBId = Shader.PropertyToID("_ColorB"),
        matricesId = Shader.PropertyToID("_Matrices"),
        sequenceNumbersId = Shader.PropertyToID("_SequenceNumbers");

    ComputeBuffer[] matricesBuffers;
    static MaterialPropertyBlock propertyBlock;

    Vector4[] sequenceNumbers;

    static quaternion[] rotations = {
        quaternion.identity,
        quaternion.RotateZ(-0.5f * PI), quaternion.RotateZ(0.5f * PI),
        quaternion.RotateX(0.5f * PI), quaternion.RotateX(-0.5f * PI)
    };

    // Rather than have each part update itself,
    // we'll instead control the entire fractal from the single root object
    // https://catlikecoding.com/unity/tutorials/basics/jobs/#:~:text=entering%20play%20mode.-,Storing%20Information,-Rather%20than%20have
    struct FractalPart
    {
        public float3 worldPosition;
        public quaternion rotation, worldRotation;
        public float maxSagAngle, spinAngle, spinVelocity;
    }

    /// 
    /// Functions
    /// 

    FractalPart CreatePart(int childIndex) => new FractalPart
    {
        maxSagAngle = radians(Random.Range(maxSagAngleA, maxSagAngleB)),
        rotation = rotations[childIndex],
        spinVelocity = (Random.value < reverseSpinChance ? -1f : 1f) * radians(Random.Range(spinSpeedA, spinSpeedB))
    };


    private void OnEnable()
    {
        parts = new NativeArray<FractalPart>[depth];
        matrices = new NativeArray<float3x4>[depth];

        matricesBuffers = new ComputeBuffer[depth];
        sequenceNumbers = new Vector4[depth];

        int stride = 12 * 4;
    
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // There will be 5 children of each clone so length * 5
            parts[i] = new NativeArray<FractalPart>(length, Allocator.Persistent);
            // We need a transformation matrix to store the world pos, rotation etc...
            matrices[i] = new NativeArray<float3x4>(length, Allocator.Persistent);
            matricesBuffers[i] = new ComputeBuffer(length, stride);
            sequenceNumbers[i] = new Vector4(Random.value, Random.value, Random.value, Random.value);
        }

        // To create and name each child
        parts[0][0] = CreatePart(0); // Root

        // li -> level iterator, fpi -> fractal parts iterator, ci -> child iterator
        for (int li = 1; li < parts.Length; li++)
        {
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
            parts[i].Dispose();
            matrices[i].Dispose();
        }

        parts = null;
        matrices = null;
        matricesBuffers = null;
        sequenceNumbers = null;
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
        float deltaTime = Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle = rootPart.spinVelocity * deltaTime;
        rootPart.worldRotation = mul(transform.rotation, mul(rootPart.rotation, quaternion.RotateY(rootPart.spinAngle)));
        rootPart.worldPosition = transform.position;
        
        float objectScale = transform.lossyScale.x;
        parts[0][0] = rootPart;

        float3x3 r = float3x3(rootPart.worldRotation) * objectScale;
        matrices[0][0] = float3x4(r.c0, r.c1, r.c2, rootPart.worldPosition);

        float scale = objectScale;

        JobHandle jobHandle = default;
        // li -> level iterator, fpi -> fractal parts iterator
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            NativeArray<FractalPart> parentParts = parts[li - 1];
            NativeArray<FractalPart> levelParts = parts[li];
            NativeArray<float3x4> levelMatrices = matrices[li];

            jobHandle = new UpdateFractalLevelJob
            {
                deltaTime = deltaTime,
                scale = scale,
                parents = parts[li - 1],
                parts = parts[li],
                matrices = matrices[li]
            }.ScheduleParallel(parts[li].Length, 1, jobHandle);

            /* Schedule doesn't immediately run the job, it only schedules it for later processing.
             * It returns a JobHandle value that can be used to track the job's progress. 
             * We can delay further execution of our code until the job is finished by invoking Complete on the handle. */
            // job.Schedule(parts[li].Length, default).Complete(); similar
    
        }
        jobHandle.Complete();

        /// Send matrix data to compute shader (Draw)
        var bounds = new Bounds(rootPart.worldPosition, 3f * objectScale * Vector3.one);
        int leafIndex = matricesBuffers.Length - 1;
        for (int i = 0; i < matricesBuffers.Length; i++)
        {
            ComputeBuffer buffer = matricesBuffers[i];
            buffer.SetData(matrices[i]);

            Mesh instanceMesh;
            Color colorA, colorB;
            if (i == leafIndex)
            {
                colorA = leafColorA;
                colorB = leafColorB;
                instanceMesh = leafMesh;
            }
            else
            {
                float gradientInterpolator = i / (matricesBuffers.Length - 2f);
                colorA = gradientA.Evaluate(gradientInterpolator);
                colorB = gradientB.Evaluate(gradientInterpolator);
                instanceMesh = mesh;
            }

            propertyBlock.SetColor(colorAId, colorA);
            propertyBlock.SetColor(colorBId, colorB);
            propertyBlock.SetBuffer(matricesId, buffer);
            propertyBlock.SetVector(sequenceNumbersId, sequenceNumbers[i]);
            Graphics.DrawMeshInstancedProcedural(instanceMesh, 0, material, bounds, buffer.count, propertyBlock);
        }

    }

 
}
