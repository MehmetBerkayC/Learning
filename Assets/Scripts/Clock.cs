using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Clock : MonoBehaviour
{
    [SerializeField] Transform hoursPivot, minutesPivot, secondsPivot;
    float hourstoDegrees = -30.0f;
    float minutesToDegrees = -6.0f;
    float secondsToDegrees = -6.0f;
    private void Awake()
    {
        CalculateRotations();
    }

    // Update is called once per frame
    void Update()
    {
        CalculateRotations();
    }

    private void CalculateRotations()
    {
        TimeSpan CurrentTime = DateTime.Now.TimeOfDay;

        hoursPivot.localRotation   = Quaternion.Euler(0, 0, hourstoDegrees * DateTime.Now.Hour      /*(float)CurrentTime.TotalHours)*/);
        minutesPivot.localRotation = Quaternion.Euler(0, 0, minutesToDegrees * DateTime.Now.Minute  /*(float)CurrentTime.TotalMinutes)*/);
        secondsPivot.localRotation = Quaternion.Euler(0, 0, secondsToDegrees * DateTime.Now.Second  /*(float)CurrentTime.TotalSeconds)*/);
    }
}

