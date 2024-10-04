using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public enum Mode
{
    Manual,
    Auto,
    Telemetry,
}

public class ModeManager : MonoBehaviour
{
    public bool SingleRounds = true; //Are the enemies one at a time?
    public bool GroupRounds = false; //Are the enemies in groups?
    public bool MixedRounds = false; //Are the enemies mixed?

    public float actionDelay;
    public DateTime lastActionTime;
    public bool FastPlay = false;

    public List<MonoBehaviour> PlayMode;

    public Mode CurrentMode = Mode.Manual;

    // Start is called before the first frame update
    void Start()
    {
        CurrentMode = Mode.Manual;
        
        GameplayController.OnNewRound += OnNewRound;
        GameplayController.OnNewFight += OnNewFight;
        GameplayController.OnSimOver += OnSimOver;
        GameplayController.OnRoundFinished += OnRoundFinished;
        GameplayController.OnEnemyDeath += OnEnemyDeath;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            if (CurrentMode == Mode.Manual)
            {
                CurrentMode = Mode.Auto;
            }
            else
            {
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (CurrentMode == Mode.Auto || CurrentMode == Mode.Telemetry)
            {
                FastPlay = !FastPlay;
            }
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            if (CurrentMode == Mode.Manual)
            {
                CurrentMode = Mode.Telemetry;
            }
            else
            {
                return;
            }
        }

        //If the G key is pressed, toggle between rounds that are groups
        if (Input.GetKeyDown(KeyCode.G) == true)
        {
            GameplayController.RoundCount = 0;
            SingleRounds = false;
            GroupRounds = true;
            MixedRounds = false;

            GameplayController.NewRound();
        }

        //If the M key is pressed, toggle between rounds that are mixed
        if (Input.GetKeyDown(KeyCode.M) == true)
        {
            GameplayController.RoundCount = 0;
            SingleRounds = false;
            GroupRounds = false;
            MixedRounds = true;

            GameplayController.NewRound();
        }

        if (Input.GetKeyDown(KeyCode.Tab) == true)
        {
            GameplayController.NewRound();
        }

        //If the R key is pressed, restart the simulation.
        if (Input.GetKeyDown(KeyCode.R) == true)
        {
            CurrentMode = Mode.Manual;
            GameplayController.RestartSim();
        }
    }

    public void OnNewRound()
    {
        (PlayMode[(int)CurrentMode] as IPlayMode).OnNewRound();
    }

    public void OnNewFight()
    {
        (PlayMode[(int)CurrentMode] as IPlayMode).OnNewFight();
    }

    public void OnSimOver()
    {
        (PlayMode[(int)CurrentMode] as IPlayMode).OnSimOver();
    }

    public void OnRoundFinished(int fight, int round, float damage, float time, Player player)
    {
        (PlayMode[(int)CurrentMode] as IPlayMode).OnRoundFinished(fight, round, damage, time, player);
    }

    public void OnEnemyDeath()
    {
        (PlayMode[(int)CurrentMode] as IPlayMode).OnEnemyDeath();
    }
}
