using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealthBorder : MonoBehaviour
{
    public Player Player;

    void Update()
    {
        // if the player's health is less than 50, make the opacity of the border fluctuate between 0.5 and 0.8
        if (Player.HitPoint.HitPoints < 50)
        {
            var color = GetComponent<SpriteRenderer>().color;
            color.a = Mathf.PingPong(Time.time, 0.3f) + 0.5f;
            GetComponent<SpriteRenderer>().color = color;
        }
        else
        {
            var color = GetComponent<SpriteRenderer>().color;
            color.a = 0.0f;
            GetComponent<SpriteRenderer>().color = color;
        }
    }
}
