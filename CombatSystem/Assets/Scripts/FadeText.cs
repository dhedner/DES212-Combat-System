/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      3/1/2021
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component will fade out the text on any game object it is added to over the
	specified time frame, after a specified delay.

*******************************************************************************/

//Standard Unity component libraries
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic; //Not needed in this file, but here just in case.
using UnityEngine;
using UnityEngine.UI;
using TMPro;

//The following statement telling the Unity editor to not allow this component to be
//added to a game object that does not also have a TextMeshProUGUI component.
[RequireComponent(typeof(TextMeshProUGUI))]

//Inherents from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class FadeText : MonoBehaviour
{
	//By making these member variables public, they can automatically be edited in the
	//property inspector.
    public float TimeUntilFade = 0.0f;
    public float TimeToFade = 1.0f;

	//This varialbe just tracks how long the object has been fading, so it doesn't needed
	//to be public.
    private float TimeFading = 0.0f;

	//We don't need a StartUp() function for this component.

    //Update is called once per frame
    void Update()
    {
		//Time.deltaTime is the amount of time passed since the last update in seconds
		//and is from the UnityEngine library.
        TimeUntilFade -= Time.deltaTime;

		//Not time to start fading yet.
        if (TimeUntilFade > 0.0f)
            return;
		
		//Fading is done, so don't keep trying to fade.
        if (TimeFading > TimeToFade)
            return;

        Color fadingColor = GetComponent<TextMeshProUGUI>().color;
        fadingColor.a = Mathf.Clamp(1.0f - (TimeFading / TimeToFade), 0.0f, 1.0f);
        GetComponent<TextMeshProUGUI>().color = fadingColor;
		//Increment the time while fading.
        TimeFading += Time.deltaTime;
    }
}
