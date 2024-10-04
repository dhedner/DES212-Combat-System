/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object an enemy ability. The game object then must
    be parented to the actual enemy game object in order to work. Should this really
    be a different class than the hero ability? It doesn duplicate some functionality,
    but often hero and enemy abilities end up subtly different, so this can be okay to do.
	
*******************************************************************************/

//Standard Unity component libraries
using System;
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic; //Not needed in this file, but here just in case.
using System.Linq;
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI; //This is here so we don't have to type out longer names for UI components.

//Inherits from MonoBehavior like all normal Unity components do...
//Remember that the class name MUST be identical to the file name!
public class EnemyAbility : MonoBehaviour
{
    //Properties that define the ability's cooldown time, damage done, range, etc.
    public float CooldownTime = 1.0f;
    public float DamageDone = 1.0f;
    public float Healing = 0.0f;
    public float MaximumRange = 10.0f;
    public bool Inactive = false; //Make an ability inactive to temporarily or permanently not have it used.

    [HideInInspector]
    public float CooldownLeft = 0.0f; //How much of the cooldown time is actually left.

    [HideInInspector]
    public StatBar CooldownBar; //Reference to the cooldown timer bar, so we don't have to look it up all the time.

    [HideInInspector]
    public Enemy ParentEnemy; //Reference to the parent enemy, so we don't have to look it up all the time.

    //Start is called before the first frame update
    void Start()
    {
        //Get the parent.
        ParentEnemy = transform.parent.GetComponent<Enemy>();
        //Find the cooldown timer gameobject, which must be a child of this object.
        CooldownBar = transform.Find("Cooldown").GetComponent<StatBar>();
    }

    //Update is called once per frame
    void Update()
    {
        if (ParentEnemy.HitPoint.HitPoints == 0.0f)
        {
            CooldownLeft = 0.0f;
        }
        else
        {
            CooldownLeft = Mathf.Clamp(CooldownLeft - GameplayController.DT, 0.0f, CooldownTime);
        }

        //CooldownLeft = Mathf.Clamp(CooldownLeft - GameplayController.DT, 0.0f, CooldownTime);
        //Since cooldowns update every frame, no need to worry about interpolating over time.
        if (Inactive || CooldownTime == 0.0f) //Either doesn't have a cooldown or is inactive, so scale it to nothing.
            CooldownBar.InterpolateToScale(0.0f, 0.0f);
        else
            CooldownBar.InterpolateToScale(CooldownLeft / CooldownTime, 0.0f);
    }

    //Don't let a cooldown affect the next fight
    public void ResetCooldown()
    {
        CooldownLeft = CooldownTime;
    }

    //Get the distance to the target along the X axis (1D not 2D).
    public float DistanceToTarget(HitPointComponent target)
    {
        return Mathf.Abs(ParentEnemy.transform.position.x - target.transform.position.x);
    }

    //Is an ability ready for use?
    public bool IsReady()
    {
        var target = (from o in GameplayController.TargetForEnemy
                      where o != ParentEnemy.HitPoint
                      select o).FirstOrDefault();

        if (Inactive || ParentEnemy.HitPoint.HitPoints == 0.0f || target == null || DistanceToTarget(target) > MaximumRange || CooldownLeft > 0.0f)
        {
            return false;
        }

        // If the target is the player, check if the player is dead
        // Otherwise, if the target is another enemy, check if that enemy is dead
        if (target.GetComponent<Player>())
        {
            Player player = target.GetComponent<Player>();
            if (player.HitPoint.HitPoints == 0.0f)
            {
                return false;
            }
        }
        else if (target.GetComponent<Enemy>())
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy.HitPoint.HitPoints == 0.0f)
            {
                return false;
            }
        }

        //Ready to go
        return true;
    }

    //Use the ability if it is ready.
    public bool Use()
    {
        //Is it ready?
        if (!IsReady())
            return false;

        //Apply the damage (or healing is the damage is negative).
        // If the target is the player, the player will take damage.
        // If the target is another enemy, that enemy will take damage.
        var target = (from o in GameplayController.TargetForEnemy
                      where o != ParentEnemy.HitPoint
                      select o).FirstOrDefault();

        // if the enemy is frenzied, apply a damage multiplier to the enemy's abilities
        float damageMultiplier = 1.0f;
        if (ParentEnemy.HitPoint.IsFrenzied)
        {
            damageMultiplier = 1.5f;
        }

        if (target.TakeDamage(DamageDone * damageMultiplier) == true)
        {
            // if the target wasn't the player
            if (target.GetComponent<Player>())
            {
                target.GetComponent<Player>().GetComponent<SpriteRenderer>().color = Color.grey;
            }
            else if (target.GetComponent<Enemy>())
            {
                target.GetComponent<Enemy>().GetComponent<SpriteRenderer>().color = Color.grey;
            }

            Console.WriteLine("Target is dead");
        }

        if (Healing > 0.0f)
        {
            var enemies = FindObjectsOfType<Enemy>();

            foreach (Enemy enemy in enemies)
            {
                if (enemy.HitPoint.HitPoints > 0.0f)
                {
                    enemy.HitPoint.HitPoints += Healing;
                }
            }
        }

        //Put the ability on cooldown.
        CooldownLeft = CooldownTime;
        return true;
    }

}
