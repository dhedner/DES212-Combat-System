/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component makes a game object a hero ability. The game object then must
    be parented to the actual hero game object in order to work. Should this really
    be a different class than the enemy ability? It doesn duplicate some functionality,
    but often hero and enemy abilities end up subtly different, so this can be okay to do.
	
*******************************************************************************/

//Standard Unity component libraries
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public struct AbilityCost
{
    [SerializeField] public PlayerResourceType resourceType;
    [SerializeField] public int Amount;

    public AbilityCost(PlayerResourceType resource, int amount)
    {
        resourceType = resource;
        Amount = amount;
    }

    public override string ToString()
    {
        return $"{resourceType}: {Amount}";
    }
}

[Serializable]
public struct AbilityAttributes
{
    [SerializeField] public float CooldownTime;
    [SerializeField] public float DamageDone;
    [SerializeField] public float Knockback;
    [SerializeField] public float Frenzy;
    [SerializeField] public float MaximumRange;
    [SerializeField] public bool Active;

    public AbilityAttributes(float cooldownTime, float damageDone, float knockback, float frenzy, float maximumRange, bool active)
    {
        CooldownTime = cooldownTime;
        DamageDone = damageDone;
        Knockback = knockback;
        Frenzy = frenzy;
        MaximumRange = maximumRange;
        Active = active;
    }

    public override string ToString()
    {
        return $"Cooldown: {CooldownTime}, Damage: {DamageDone}, Knockback: {Knockback}, Frenzy: {Frenzy}, Range: {MaximumRange}, Active: {Active}";
    }
}

public class PlayerAbility : MonoBehaviour
{
    [HideInInspector]
    public float CooldownLeft = 0.0f;

    [HideInInspector]
    public StatBar CooldownBar;

    [HideInInspector]
    public TextMeshProUGUI AbilityNumber;

    [HideInInspector]
    public Player ParentPlayer;

    public List<AbilityCost> AbilityCost
    {
        get
        {
            return _abilityCosts;
        }
        set
        {
            _abilityCosts = value;
        }
    }

    [SerializeField]
    private List<AbilityCost> _abilityCosts;

    public AbilityAttributes AbilityAttributes
    {
        get
        {
            return _abilityAttributes;
        }
        set
        {
            _abilityAttributes = value;
        }
    }

    [SerializeField]
    private AbilityAttributes _abilityAttributes;

    void Start()
    {
        ParentPlayer = GameObject.Find("Player").GetComponent<Player>();
        CooldownBar = transform.Find("Cooldown").GetComponent<StatBar>();
        AbilityNumber = transform.Find("AbilityNumber").GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        CooldownLeft = Mathf.Clamp(CooldownLeft - GameplayController.DT, 0.0f, _abilityAttributes.CooldownTime);

        if (!_abilityAttributes.Active || _abilityAttributes.CooldownTime == 0.0f)
        {
            CooldownBar.InterpolateToScale(0.0f, 0.0f);
        }
        else
        {
            CooldownBar.InterpolateToScale(CooldownLeft / _abilityAttributes.CooldownTime, 0.0f);
        }

        if (IsReady())
        {
            // make the ability number more visible over a short period of time
            AbilityNumber.color = new Color(AbilityNumber.color.r, AbilityNumber.color.g, AbilityNumber.color.b, Mathf.Lerp(AbilityNumber.color.a, 1.0f, 0.1f));

            // make the ability number pulse
            AbilityNumber.transform.localScale = Vector3.one * (1.0f + 0.1f * Mathf.Sin(Time.time * 3.0f));
        }
        else
        {
            AbilityNumber.color = new Color(AbilityNumber.color.r, AbilityNumber.color.g, AbilityNumber.color.b, 0.2f);
        }
    }

    public void ResetCooldown()
    {
        CooldownLeft = 0.0f;
    }

    public float DistanceToTarget()
    {
        return Mathf.Abs(ParentPlayer.transform.position.x - ParentPlayer.Target.transform.position.x);
    }

    public bool IsReady()
    {
        if (!_abilityAttributes.Active ||
            ParentPlayer.HitPoint.HitPoints == 0.0f ||
            ParentPlayer.Target == null ||
            DistanceToTarget() > _abilityAttributes.MaximumRange ||
            CooldownLeft > 0.0f)
        {
            return false;
        }

        for (int i = 0; i < _abilityCosts.Count; i++)
        {
            if (ParentPlayer.GetResource(_abilityCosts[i].resourceType) < _abilityCosts[i].Amount)
            {
                return false;
            }
        }

        return true;
    }

    public bool Use()
    {
        if (!IsReady())
        {
            return false;
        }

        foreach (AbilityCost cost in _abilityCosts)
        {
            ParentPlayer.UseResource(cost.resourceType, cost.Amount);
        }

        if (ParentPlayer.Target.HitPoint.TakeDamage(_abilityAttributes.DamageDone) == true)
        {
            ParentPlayer.Target.GetComponent<SpriteRenderer>().color = Color.grey;
            
            ParentPlayer.Target = ParentPlayer.FindTarget(); // If the target is dead, find a new one

            GameplayController.NotifyEnemyDeath();
            return true;
        }

        if (_abilityAttributes.DamageDone > 0)
        {
            GameplayController.RoundDamageDone += _abilityAttributes.DamageDone;
        }

        // If the ability has knockback, apply it
        if (_abilityAttributes.Knockback > 0.0f)
        {
            float knockback = _abilityAttributes.Knockback;
            if (ParentPlayer.Target.transform.position.x < ParentPlayer.transform.position.x)
            {
                knockback = -knockback;
            }

            // apply knockback to the target, but don't make the change in position instantaneous
            ParentPlayer.Target.transform.position = new Vector3(
                ParentPlayer.Target.transform.position.x + knockback,
                ParentPlayer.Target.transform.position.y,
                ParentPlayer.Target.transform.position.z);
        }

        // If the ability applies frenzy, apply it
        if (_abilityAttributes.Frenzy > 0.0f)
        {
            ParentPlayer.Target.HitPoint.ApplyFrenzy(_abilityAttributes.Frenzy);
        }

        // Put the ability on cooldown
        CooldownLeft = _abilityAttributes.CooldownTime;
        return true;
    }
}
