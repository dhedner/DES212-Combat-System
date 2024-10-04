/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object an enemy. These are dynamically spawned at
    the start of each round by the GameplayController script.
	
*******************************************************************************/

//Standard Unity component libraries
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Enemy : MonoBehaviour
{
    //Properties for maximum hit points, movement speed, and optimal range
    public float MoveSpeed = 0.1f;
    public float OptimalRange = 5.0f;

    //[HideInInspector]
    //public GameObject Target; // Current target

    //References to the abilities
    public EnemyAbility _AbilityOne;

    //Always need this to be a "real" ability.
    [HideInInspector]
    public EnemyAbility AbilityOne
    {
        get
        {
            if (_AbilityOne == null)
            {
                _AbilityOne = transform.Find("AbilityOne").GetComponent<EnemyAbility>();
            }

            return _AbilityOne;
        }
    }

    //This one might be inactive for simple enemies.
    //[HideInInspector]
    //public EnemyAbility AbilityTwo { get; private set; }

    public EnemyAbility _AbilityTwo;

    //Always need this to be a "real" ability.
    [HideInInspector]
    public EnemyAbility AbilityTwo
    {
        get
        {
            if (_AbilityTwo == null)
            {
                _AbilityTwo = transform.Find("AbilityTwo").GetComponent<EnemyAbility>();
            }

            return _AbilityTwo;
        }
    }

    public HitPointComponent HitPoint;

    //Start is called before the first frame update
    void Start()
    {
        //Find() will get the first child game object of that name.
        //Use GetComponent so we don't have to use it later to access the functionality we want.

        //AbilityOne = transform.Find("AbilityOne").GetComponent<EnemyAbility>();
        //AbilityTwo = transform.Find("AbilityTwo").GetComponent<EnemyAbility>();

        //Reset all the cooldowns.
        if (AbilityOne != null) AbilityOne.ResetCooldown();
        if (AbilityTwo != null) AbilityTwo.ResetCooldown();
        HitPoint.Initialize();
    }

    //Update is called once per frame
    void Update()
    {
        if (GameplayController.RoundOver) //Don't update between rounds (or when the sim is over).
            return;
        //if (Target == null) //If we don't have a target, the round must have just started.
        //    Initialize();

        //The fight is on, so move and use abilities.
        DoMovement();
        UseRandomAbility();

        // if the enemy is frenzied, apply a damage multiplier to the enemy's abilities
        //if (GameplayController.Frenzy)
        //{
        //    AbilityOne.DamageMultiplier = 1.5f;
        //    AbilityTwo.DamageMultiplier = 1.5f;
        //}
        //else
        //{
        //    AbilityOne.DamageMultiplier = 1.0f;
        //    AbilityTwo.DamageMultiplier = 1.0f;
        //}
    }

    //Try to stay close to optimal range.
    public void DoMovement()
    {
        var target = (from o in GameplayController.TargetForEnemy
                      where o != HitPoint
                      select o).FirstOrDefault();

        if (HitPoint.HitPoints <= 0.0f || target == null) //If the enemy or the player is dead, no need to move.
            return;

        //Calculate distance to target along the X axis (1D not 2D).
        float distanceToTarget = transform.position.x - target.transform.position.x;
        //If we are between 80% and 100% of optimal range, that's good enough.
        if (Mathf.Abs(distanceToTarget) <= OptimalRange && Mathf.Abs(distanceToTarget) >= OptimalRange * 0.8f)
            return;
        //If we are too close, flip the "distance" so we will move away instead of towards.
        if (Mathf.Abs(distanceToTarget) < OptimalRange * 0.8f)
            distanceToTarget = -distanceToTarget;
        //We need to move, so get our current X position.
        float newX = transform.position.x;
        if (distanceToTarget > 0) //Move to the left.
            newX -= MoveSpeed * GameplayController.DT; //Make sure to use the simulated DT.
        else //Move to the right.
            newX += MoveSpeed * GameplayController.DT; //Make sure to use the simulated DT.
        //Don't go past the edge of the arena.
        newX = Mathf.Clamp(newX, -GameplayController.EdgeDistance, GameplayController.EdgeDistance);
        //Update the transform.
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    //Try to use a random ability.
    public bool UseRandomAbility()
    {
        //Get a random number between 1 and 2. Yes, the integer version of this function is not
        //inclusive. This is wrong and Unity should feel bad for doing this.
        return UseAbility(Random.Range(1, 3));
    }

    //Try to use a specific ability. Returns whether it actually was used.
    public bool UseAbility(int abilityNumber)
    {
        //We could do this with an array of abilities,
        //but there are only two and we are lazy.
        if (abilityNumber == 1 && AbilityOne != null)
            return AbilityOne.Use();
        if (abilityNumber == 2 && AbilityTwo != null)
            return AbilityTwo.Use();
        return false;
    }
}
