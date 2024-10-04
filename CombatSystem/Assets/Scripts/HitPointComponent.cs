using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HitPointComponent : MonoBehaviour
{
    public float MaxHitPoints = 50;
    public float HitPoints = 0; // Current hit points.

    public StatBar HealthBar;

    public bool IsFrenzied; //If the enemy is frenzied, it will be attacked by all other enemies.

    [HideInInspector]
    public float FrenzyLeft = 0.0f;
    [HideInInspector]
    public float FrenzyDuration = 0.0f;

    private Color _originalColor;

    // Start is called before the first frame update
    void Start()
    {
        //No resetting of position because we don't want to override the position they were instansiated at.
        //(Unlike the player who is never actually deleted and needs to be reset every round.)
        //Reset hit points.
        HitPoints = MaxHitPoints;
    }

    // Update is called once per frame
    void Update()
    {
        CheckFrenzy();
    }

    public void Initialize()
    {
        //Make sure the health bar gets reset as well.
        //HealthBar.InterpolateImmediate(HitPoints / MaxHitPoints);
    }

    //Take damage from any source.
    public bool TakeDamage(float damage)
    {
        if (damage != 0.0f) //Don't bother if the damage is 0
        {
            //Accumulate the telemetry data.
            //GameplayController.DamageDone += Mathf.Min(Mathf.Max(damage, 0.0f), HitPoints); //Can't do more damage than the target has HP, and negative damage is actually healing.
            //Make sure hit points do not go negative (or above max, because the "damage" could be negative, i.e., healing).
            HitPoints = Mathf.Clamp(HitPoints - damage, 0.0f, MaxHitPoints);
            //Interpolate the hit point UI bar over half a second.
            HealthBar.InterpolateToScale(HitPoints / MaxHitPoints, 0.5f);
            //Create a temporary InfoText object to show the damage using the static Instantiate() function.
            TextMeshProUGUI damageText = Object.Instantiate(GameplayController.InfoTextPrefab, transform.position, Quaternion.identity, GameplayController.Canvas.transform).GetComponent<TextMeshProUGUI>();
            //Set the damage text to just the integer amount of the damage done.
            //Uses the "empty string plus number" trick to make it a string.
            damageText.text = "" + Mathf.Floor(damage);
        }

        //Debug.Log(gameObject.name + " took " + damage + " damage");
        //Debug.Log(gameObject.name + " has " + HitPoints + " health left");

        //Return true if dead.
        return (HitPoints <= 0.0f);
    }

    public void ApplyFrenzy(float duration)
    {
        FrenzyLeft = duration;
        IsFrenzied = true;
        _originalColor = GetComponent<SpriteRenderer>().color;
        GetComponent<SpriteRenderer>().color = Color.red;
        //Debug.Log("Frenzy applied to " + gameObject.name);
    }

    public void CheckFrenzy()
    {
        if (!IsFrenzied)
        {
            return;
        }

        FrenzyLeft = Mathf.Max(FrenzyLeft - GameplayController.DT, 0.0f);

        if ((HitPoints > 0.0f && FrenzyLeft == 0.0f) || HitPoints <= 0.0f)
        {
            IsFrenzied = false;
            //Debug.Log("Frenzy removed from " + gameObject.name);
            GetComponent<SpriteRenderer>().color = _originalColor;
        }
    }
}
