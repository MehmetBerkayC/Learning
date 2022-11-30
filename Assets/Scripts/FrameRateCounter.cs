using UnityEngine;
using TMPro;

public class FrameRateCounter : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI display;

    public enum DisplayMode { FPS, MS }

    [SerializeField] DisplayMode displayMode = DisplayMode.FPS;
    [SerializeField, Range(0.1f, 2f)] float sampleDuration = 1f;

    int frames;
    float duration, bestDuration = float.MaxValue, worstDuration;

    private void Update()
    {
        float frameDuration = Time.unscaledDeltaTime;
        frames += 1;
        duration += frameDuration;

        // Check the current Frame duration to be the best/worst one so far
        if (frameDuration < bestDuration)
        {
            bestDuration = frameDuration;
        }
        
        if (frameDuration > worstDuration)
        {
            worstDuration = frameDuration;
        }

        // We're measuring the duration between Unity frames 
        if (duration >= sampleDuration)
        {
            if(displayMode == DisplayMode.FPS)
            {
                // Display average frame rate over given time range (visually stablizing the FPS, Frames / 1 Sec - Default)
                display.SetText("FPS\n{0:0}\n{1:0}\n{2:0}", 1f / bestDuration, frames / duration, 1f / worstDuration);

            }
            else
            {
                display.SetText("MS\n{0:1}\n{1:1}\n{2:1}", 1000f * bestDuration, 1000f * duration / frames, 1000f * worstDuration);
            }

            // Reset
            frames = 0;
            duration = 0f;
            bestDuration = float.MaxValue;
            worstDuration = 0f;
        }
    }
}
