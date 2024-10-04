/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      3/1/2021
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component will control the X scale any game object it is added to (even if it
	isn't actually a bar). In particular, it will keep track of the original scale and
	allow other code to just give it the percentage of that original scale that is
	desired (and can interpolate the scale to the new value over time if desired).

*******************************************************************************/

//Standard Unity component libraries
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class StatBar : MonoBehaviour
{
    public bool Rotated = false; //If the object is rotated, this flag needs to be set

    //Track the scaling, position, and interpolation
    private float MaxRealScale;
    private Vector3 OriginalPosition;
    private float CurrentScale = 1.0f;
    private float TargetScale = 1.0f;
    private float ScaleTime = 0.0f;
    private float InterpolationTime = 0.0f;

    //Start is called before the first frame update
    void Start()
    {
        MaxRealScale = gameObject.transform.localScale.x;
        OriginalPosition = gameObject.transform.localPosition;
    }

    //Update is called once per frame
    void Update()
    {
        if (InterpolationTime > 0.0f)
        {
            InterpolationTime = Mathf.Clamp(InterpolationTime - Time.deltaTime, 0.0f, 1.0f);
            if (Rotated == true)
            {
                SetScaleRotated(TargetScale + ((CurrentScale - TargetScale) * InterpolationTime) / ScaleTime);
            }
            else
            {
                SetScaleStandard(TargetScale + ((CurrentScale - TargetScale) * InterpolationTime) / ScaleTime);
            }
        }
    }


    void SetScaleStandard(float newScale)
    {
        gameObject.transform.localScale = new Vector3(newScale, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
        float positionAdjustment = (MaxRealScale - newScale) / 2.0f;
        gameObject.transform.localPosition = new Vector3(OriginalPosition.x - positionAdjustment, OriginalPosition.y, OriginalPosition.z);
    }


    void SetScaleRotated(float newScale)
    {
        gameObject.transform.localScale = new Vector3(newScale, gameObject.transform.localScale.y, gameObject.transform.localScale.z);
        float positionAdjustment = (MaxRealScale - newScale) / 2.0f;
        gameObject.transform.localPosition = new Vector3(OriginalPosition.x, OriginalPosition.y - positionAdjustment, OriginalPosition.z);
    }


    public void InterpolateToScale(float percent, float time)
    {
        CurrentScale = gameObject.transform.localScale.x;
        TargetScale = percent * MaxRealScale;
        ScaleTime = time + 0.001f;
        InterpolationTime = time + 0.001f;
    }


    public void InterpolateImmediate(float percent)
    {
        CurrentScale = gameObject.transform.localScale.x;
        TargetScale = percent * MaxRealScale;
        ScaleTime = 0.0f;
        InterpolationTime = 0.0f;

        if (Rotated == true)
        {
            SetScaleRotated(TargetScale);
        }
        else
        {
            SetScaleStandard(TargetScale);
        }
    }
}
