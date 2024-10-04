/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object a hero that can be controlled by the player.
    There is only a single hero that is already placed in the scene.
	
*******************************************************************************/

//Standard Unity component libraries
using System;
using System.Collections; //Not needed in this file, but here just in case.
using System.Collections.Generic; //Not needed in this file, but here just in case.
using System.Linq;
using TMPro;
using UnityEngine; //The library that lets your access all of the Unity functionality.
using UnityEngine.UI; //This is here so we don't have to type out longer names for UI components.

public enum PlayerResourceType
{
    Sanity,
    Insight
}

public class Player : MonoBehaviour
{
    //Properties for maximum hit points, movement speed, maximum insight, and optimal range.
    //public float MaxSanity = 200;
    public float MoveSpeed = 0.1f;
    public float MaxInsight = 100;
    public float OptimalRange = 5.0f;

    //[HideInInspector]
    //public float Sanity = 200; //Current hit points
    [HideInInspector]
    public float Insight = 20; //Current insight

    [HideInInspector]
    public Enemy Target; //Current target enemy.

    [HideInInspector]
    public StatBar SanityBar;
    [HideInInspector]
    public StatBar InsightBar;

    public HitPointComponent HitPoint;

    public GameObject TargetIndicator;

    public ModeManager ModeManager;

    public DecisionMaker DecisionMaker;

    //List of abilities the hero has.
    public List<PlayerAbility> Abilities = new List<PlayerAbility>();

    private GameObject _targetIndicatorInstance;

    //Start is called before the first frame update
    void Start()
    {
        SanityBar = GameObject.Find("PlayerResources/SanityBar").GetComponent<StatBar>();
        InsightBar = GameObject.Find("PlayerResources/InsightBar").GetComponent<StatBar>();
        HitPoint = GetComponent<HitPointComponent>();
        ModeManager = GetComponentInParent<ModeManager>();
    }

    //Update is called once per frame
    void Update()
    {
        if (GameplayController.RoundOver) //Don't update between rounds (or when the sim is over).
        {
            return;
        }

        if (Target == null) //If we don't have a target, the round must have just started.
        {
            Target = FindTarget();
        }

        DoMovement();

        if (ModeManager.CurrentMode == Mode.Manual) // If we're not in Manual Mode
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) == true)
                UseAbility(PlayerAbilities.ForbiddenTruth);
            if (Input.GetKeyDown(KeyCode.Alpha2) == true)
                UseAbility(PlayerAbilities.RayOfClarity);
            if (Input.GetKeyDown(KeyCode.Alpha3) == true)
                UseAbility(PlayerAbilities.EldritchWhisper);
            if (Input.GetKeyDown(KeyCode.Alpha4) == true)
                UseAbility(PlayerAbilities.ArcaneEnlightenment);
            if (Input.GetKeyDown(KeyCode.Alpha5) == true)
                UseAbility(PlayerAbilities.RitualOfReason);
            if (Input.GetKeyDown(KeyCode.Alpha6) == true)
                UseAbility(PlayerAbilities.CullTheIgnorant);
        }

        UpdateTargetIndicator();
    }

    //Try to stay close to optimal range. Note this is done even in Auto mode.
    public void DoMovement()
    {
        if (HitPoint.HitPoints <= 0.0f || Target == null) //If all enemies or the player is dead, no need to move.
        {
            return;
        }

        //Get our current X position.
        float newX = transform.position.x;

        if (ModeManager.CurrentMode != Mode.Manual)
        {
            //Calculate distance to target along the X axis (1D not 2D).
            float distanceToTarget = transform.position.x - Target.transform.position.x;

            if (DecisionMaker.playStyle == PlayStyle.Random)
            {
                //If we are between 80% and 100% of optimal range
                if (Mathf.Abs(distanceToTarget) <= OptimalRange && Mathf.Abs(distanceToTarget) >= OptimalRange * 0.7f)
                {
                    return;
                }

                //If we are too close, flip the "distance" so we will move away instead of towards.
                if (Mathf.Abs(distanceToTarget) < OptimalRange * 0.8f)
                {
                    distanceToTarget = -distanceToTarget;
                }
                if (distanceToTarget > 0)
                {
                    newX -= MoveSpeed * GameplayController.DT;
                }
                else
                {
                    newX += MoveSpeed * GameplayController.DT;
                }
            }
            else if (DecisionMaker.playStyle == PlayStyle.Smart)
            {
                //If the player has less than 20% sanity, move away from the target
                if (HitPoint.HitPoints / HitPoint.MaxHitPoints < 0.2f)
                {
                    //if (distanceToTarget > 0)
                    //{
                    //    newX += MoveSpeed * GameplayController.DT;
                    //}
                    //else
                    //{
                    //    newX -= MoveSpeed * GameplayController.DT;
                    //}

                    //Debug.Log("Player is low on sanity, moving away from target");
                }
                else
                {
                    //If we are between 90% and 100% of optimal range
                    if (Mathf.Abs(distanceToTarget) <= OptimalRange && Mathf.Abs(distanceToTarget) >= OptimalRange * 0.9f)
                    {
                        return;
                    }

                    //If we are too close, flip the "distance" so we will move away instead of towards.
                    if (Mathf.Abs(distanceToTarget) < OptimalRange * 0.8f)
                    {
                        distanceToTarget = -distanceToTarget;
                    }
                    if (distanceToTarget > 0)
                    {
                        newX -= MoveSpeed * GameplayController.DT;
                    }
                    else
                    {
                        newX += MoveSpeed * GameplayController.DT;
                    }
                }
            }

        }
        else // Player is in control of movement.
        {
            if (Input.GetKey(KeyCode.LeftArrow) == true)
            {
                newX -= MoveSpeed * GameplayController.DT;
            }

            if (Input.GetKey(KeyCode.RightArrow) == true)
            {
                newX += MoveSpeed * GameplayController.DT;
            }
        }

        //Don't go past the edge of the arena.
        newX = Mathf.Clamp(newX, -GameplayController.EdgeDistance, GameplayController.EdgeDistance);
        //Update the transform.
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    // Find the best target
    public Enemy FindTarget()
    {
        // Find all the enemies in the scene
        var enemies = (from e in FindObjectsOfType<Enemy>()
                       where e.HitPoint.HitPoints > 0.0f
                       select e).ToArray();

        if (enemies.Length == 0)
        {
            return null;
        }

        Enemy target = null;
        if (Target != null && Target.HitPoint.HitPoints > 0.0f)
        {
            target = Target;
        }

        // Find the enemy with the lowest HP.
        float lowestHP = float.MaxValue;
        if (target)
        {
            lowestHP = target.HitPoint.HitPoints;
        }

        if (DecisionMaker.playStyle == PlayStyle.Smart)
        {
            foreach (Enemy enemy in enemies)
            {
                // if the enemy is a VeilingWraith, target it
                if (enemy.name == "VeilingWraith(Clone)")
                {
                    return enemy;
                }
            }

            foreach (Enemy enemy in enemies)
            {
                // Loop through the enemy's abilities
                foreach (EnemyAbility ability in new EnemyAbility[] { enemy.AbilityOne, enemy.AbilityTwo })
                {
                    if (ability.DamageDone >= 30)
                    {
                        // This is a heavy hitter
                        //Debug.Log("Heavy hitter found");
                        //Debug.Log("Targeting" + enemy.name);
                        return enemy;
                    }
                }
            }

            foreach (Enemy enemy in enemies)
            {
                // if the enemy is a VeilingWraith, target it
                if (enemy.HitPoint.HitPoints < lowestHP)
                {
                    lowestHP = enemy.HitPoint.HitPoints;
                    target = enemy;
                }
            }
        }
        else
        {
            // Pick a random enemy
            target = enemies[UnityEngine.Random.Range(0, enemies.Length)];
            //foreach (Enemy enemy in enemies)
            //{
            //    // if the enemy is a VeilingWraith, target it
            //    if (enemy.HitPoint.HitPoints < lowestHP)
            //    {
            //        lowestHP = enemy.HitPoint.HitPoints;
            //        target = enemy;
            //    }
            //}
        }

        //Debug.Log("Targeting " + target.name);
        return target;
    }

    //This is NOT a Start() function because we need to be able to call Initialize() whenever a new
    //round starts, not just when the object is created.
    public void Initialize()
    {
        Debug.Log("Player Initialize");

        //Set our X position to the correct starting position on the left side of the arena, while keeping the Y and Z the same.
        transform.position = new Vector3(-GameplayController.StartingX, transform.position.y, transform.position.z);
        //Reset hit points.
        HitPoint.HitPoints = HitPoint.MaxHitPoints;
        //Reset insight, but to 20% of MaxInsight, not the full amount.
        Insight = MaxInsight * 0.2f;
        //Reset all the cooldowns.
        foreach (PlayerAbility ability in Abilities)
        {
            ability.ResetCooldown();
        }
        Target = null;
        //Find a target.
        Target = FindTarget();
        //Make sure the health and power bars get reset.
        SanityBar.InterpolateImmediate(HitPoint.HitPoints / HitPoint.MaxHitPoints);
        InsightBar.InterpolateImmediate(Insight / MaxInsight);
        GetComponent<SpriteRenderer>().color = Color.white;
    }

    public bool UseAbility(PlayerAbilities ability)
    {
        int abilityNumber = (int)Math.Log((int)ability, 2) + 1;
        Abilities[abilityNumber - 1].Use();

        return false;
    }

    //Use a given amount of insight.
    public void UseResource(PlayerResourceType type, float amount)
    {
        if (type == PlayerResourceType.Sanity)
        {
            HitPoint.HitPoints = Mathf.Clamp(HitPoint.HitPoints - amount, 0.0f, HitPoint.MaxHitPoints);
        }
        else if (type == PlayerResourceType.Insight)
        {
            Insight = Mathf.Clamp(Insight - amount, 0.0f, MaxInsight);
            InsightBar.InterpolateToScale(Insight / MaxInsight, 0.5f);
        }
    }

    public float GetResource(PlayerResourceType type)
    {
        if (type == PlayerResourceType.Sanity)
            return HitPoint.HitPoints;
        if (type == PlayerResourceType.Insight)
            return Insight;
        return 0.0f;
    }

    public void UpdateTargetIndicator()
    {
        if (!Target)
        {
            if (_targetIndicatorInstance != null)
            {
                Destroy(_targetIndicatorInstance);
            }

            return;
        }

        if (_targetIndicatorInstance == null)
        {
            _targetIndicatorInstance = Instantiate(TargetIndicator, Target.transform.position + new Vector3(0, 1.0f, 0), Quaternion.identity);
        }
        else
        {
            // make the target float with a sin wave
            _targetIndicatorInstance.transform.position = Target.transform.position + new Vector3(0, 1.0f + Mathf.Sin(Time.time * 5.0f) * 0.1f, 0);
            //_targetIndicatorInstance.transform.position = Target.transform.position + new Vector3(0, 1.0f, 0);
        }
    }

}

