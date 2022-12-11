using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fractal : MonoBehaviour
{
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

    FractalPart[][] parts;

    Matrix4x4[][] matrices;

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

    private void Awake()
    {
        parts = new FractalPart[depth][];
        matrices = new Matrix4x4[depth][];
        matricesBuffers = new ComputeBuffer[depth];
        int stride = 16 * 4;
    
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // There will be 5 children of each clone so length * 5
            parts[i] = new FractalPart[length];
            // We need a transformation matrix to store the world pos, rotation etc...
            matrices[i] = new Matrix4x4[length];
            matricesBuffers[i] = new ComputeBuffer(length, stride);
        }

        // To create and name each child
        parts[0][0] = CreatePart(0); // Root

        // li -> level iterator, fpi -> fractal parts iterator, ci -> child iterator
        for (int li = 1; li < parts.Length; li++)
        {
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(ci);
                }
            }
        }
    }

    private void Update()
    {
        //Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);
        float spinAngleDelta = 22.5f * Time.deltaTime;

        FractalPart rootPart = parts[0][0];
        rootPart.spinAngle = spinAngleDelta;
        //rootPart.rotation *= deltaRotation;
        rootPart.worldRotation = rootPart.rotation * Quaternion.Euler(0f, rootPart.spinAngle, 0f);

        parts[0][0] = rootPart;
        matrices[0][0] = Matrix4x4.TRS(rootPart.worldPosition, rootPart.worldRotation, Vector3.one);

        float scale = 1f;
        // li -> level iterator, fpi -> fractal parts iterator
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            Matrix4x4[] levelMatrices = matrices[li];

            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                FractalPart parent = parentParts[fpi / 5];
                FractalPart part = levelParts[fpi];
                //part.rotation *= deltaRotation;
                part.spinAngle += spinAngleDelta;
                part.worldRotation = parent.worldRotation * (part.rotation * Quaternion.Euler(0f, part.spinAngle, 0f));
                part.worldPosition = parent.worldPosition + parent.worldRotation * (1.5f * scale * part.direction);
                levelParts[fpi] = part;
                levelMatrices[fpi] = Matrix4x4.TRS(part.worldPosition, part.worldRotation, scale * Vector3.one);
            }
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
