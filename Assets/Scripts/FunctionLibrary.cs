// https://catlikecoding.com/unity/tutorials/basics/mathematical-surfaces/

using UnityEngine;
using static UnityEngine.Mathf;
public static class FunctionLibrary 
{
    // This is the template we use for our functions
    public delegate Vector3 Function(float u, float v, float t);

    public enum FunctionName { Wave, MultiWave, Ripple, Sphere, Torus }

    // Make an array for every Function in this class
    static Function[] functions = { Wave, MultiWave, Ripple, Sphere, Torus };
    
    // Get the function by its name
    public static Function GetFunction(FunctionName name)
    {
        return functions[(int)name];
    }

    // Get the next function by its name
    public static FunctionName GetNextFunctionName(FunctionName name)
    {
        // Cycle through all functions
        if((int)name <  functions.Length - 1) // if not the last function
        {
            return name + 1; // send the next function
        }
        else
        {
            return 0; // return to first function
        }

        /// return (int)name < functions.Length - 1 ? name + 1 : 0; -> Ternary expression for the same blocks</functions.Length>
    }

    // Get a random function other than current (don't want duplicates)
    public static FunctionName GetRandomFunctionNameOtherThan(FunctionName name)
    {
        var choice = (FunctionName)Random.Range(1, functions.Length);
        return choice == name ? 0: choice; 
    }

    // Smooth transition between functions
    public static Vector3 Morph(float u, float v, float t, Function from, Function to, float progress)
    {
        return Vector3.LerpUnclamped(from(u, v, t), to(u, v, t), SmoothStep(0f, 1f, progress));
    }

    public static Vector3 Wave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + v + t));
        p.z = v;
        return p;
    }

    public static Vector3 MultiWave(float u, float v, float t)
    {
        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (u + 0.5f * t));
        p.y += (0.5f) * Sin(2f * PI * (v + t));
        p.y += Sin(PI * (u + v + 0.25f * t));
        p.y *= 1f / 2.5f;
        p.z = v;
        return p;
    }

    public static Vector3 Ripple (float u, float v, float t)
    {
        float d = Sqrt(u * u + v * v);

        Vector3 p;
        p.x = u;
        p.y = Sin(PI * (4f * d - t));
        p.y /= 1f + 10f * d;
        p.z = v;
        return p;
    }

    public static Vector3 Sphere (float u, float v, float t)
    {
        // Uniform UV-Sphere

        //float r = Cos(0.5f * PI * v);
        //Vector3 p;
        //p.x = r * Sin(PI * u);
        //p.y = Sin(PI * 0.5f * v);
        //p.z = r * Cos(PI * u);

        // Scaling Sphere
        // float r = 0.5f + 0.5f * Sin(PI * t);
        
        // Sphere with vertical bands
        // float r = 0.9f + 0.1f * Sin(8f * PI * u + t);

        // Sphere with horizontal bands
        // float r = 0.9f + 0.1f * Sin(8f * PI * v + t);

        // Rotating twisted sphere
        float r = 0.9f + 0.1f * Sin(PI * (6f * u + 4f * v + t));


        float s = r * Cos(0.5f * PI * v);
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r * Sin(PI * 0.5f * v);
        p.z = s * Cos(PI * u);
        return p;
    }

    public static Vector3 Torus (float u, float v, float t)
    {
        // Rotating Star
        float r1 = 0.7f + 0.1f * Sin(PI * (6f * u + 0.5f * t));
        // Twisting Ring
        float r2 = 0.15f + 0.05f * Sin(PI * (8f * u + 4f * v + 2f * t));

        // Basic Torus 
        // - Major Radius
        // float r1 = 0.75f;
        // - Minor Radius
        // float r2 = 0.25f;

        float s =  r1 + r2 * Cos(PI * v);
        Vector3 p;
        p.x = s * Sin(PI * u);
        p.y = r2 * Sin(PI * v);
        p.z = s * Cos(PI * u);
        return p;
    }
}
