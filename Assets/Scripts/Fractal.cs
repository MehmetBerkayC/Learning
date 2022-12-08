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
        public Vector3 direction;
        public Quaternion rotation;
        public Transform transform;
    }

    [SerializeField, Range(1, 8)] int depth = 4;
    [SerializeField] Mesh mesh;
    [SerializeField] Material material;

    FractalPart[][] parts;

    static Vector3[] directions = {
        Vector3.up, Vector3.right, Vector3.left, Vector3.forward, Vector3.back
    };

    static Quaternion[] rotations = {
        Quaternion.identity,
        Quaternion.Euler(0f, 0f, -90f), Quaternion.Euler(0f, 0f, 90f),
        Quaternion.Euler(90f, 0f, 0f), Quaternion.Euler(-90f, 0f, 0f)
    };

    FractalPart CreatePart(int levelIndex, int childIndex, float scale)
    {
        var gameobj = new GameObject("Fractal Part L" + levelIndex + " C" + childIndex);
        gameobj.transform.localScale = scale * Vector3.one;
        gameobj.transform.SetParent(transform, false);
        gameobj.AddComponent<MeshFilter>().mesh = mesh;
        gameobj.AddComponent<MeshRenderer>().material = material;

        // To rebuild the structure of the fractal we have to position all parts directly,
        // this time in world space.
        return new FractalPart
        {
            direction = directions[childIndex],
            rotation = rotations[childIndex],
            transform = gameobj.transform
        };
    }

    private void Awake()
    {
        parts = new FractalPart[depth][];
        
        for(int i = 0, length = 1; i < parts.Length; i++, length *= 5)
        {
            // There will be 5 children of each clone so length * 5
            parts[i] = new FractalPart[length];
           
        }

        // To create and name each child
        float scale = 1f;
        parts[0][0] = CreatePart(0, 0, scale); // Root

        // li -> level iterator, fpi -> fractal parts iterator, ci -> child iterator
        for (int li = 1; li < parts.Length; li++)
        {
            scale *= 0.5f;
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi += 5)
            {
                for (int ci = 0; ci < 5; ci++)
                {
                    levelParts[fpi + ci] = CreatePart(li, ci, scale);
                }
            }
        }
    }

    private void Update()
    {
        Quaternion deltaRotation = Quaternion.Euler(0f, 22.5f * Time.deltaTime, 0f);

        FractalPart rootPart = parts[0][0];
        rootPart.rotation *= deltaRotation;
        rootPart.transform.localRotation = rootPart.rotation;
        parts[0][0] = rootPart;

        // li -> level iterator, fpi -> fractal parts iterator
        for (int li = 1; li < parts.Length; li++)
        {
            FractalPart[] parentParts = parts[li - 1];
            FractalPart[] levelParts = parts[li];
            for (int fpi = 0; fpi < levelParts.Length; fpi++)
            {
                Transform parentTransform = parentParts[fpi / 5].transform;
                FractalPart part = levelParts[fpi];
                part.rotation *= deltaRotation;
                part.transform.localRotation = parentTransform.localRotation * part.rotation;
                part.transform.localPosition = /* scale is uniform, X component sufficient. */
                    parentTransform.localPosition + parentTransform.localRotation * (1.5f * part.transform.localScale.x * part.direction);
                levelParts[fpi] = part;
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
