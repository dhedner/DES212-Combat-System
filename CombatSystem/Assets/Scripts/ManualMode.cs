using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManualMode : MonoBehaviour, IPlayMode
{
    public ModeManager ModeManager;
    public int iterations = 1;
    private int _currentModeIndex = 0;

    public void Start()
    {
    }

    public void Update()
    {
    }

    public void OnNewRound()
    {
    }

    public void OnNewFight()
    {
    }

    public void OnRoundFinished(int fight, int round, float damage, float time, Player player)
    {
    }

    public void OnSimOver()
    {
        bool continueSim = true;
        if (ModeManager.SingleRounds)
        {
            ModeManager.SingleRounds = false;
            ModeManager.GroupRounds = true;
            ModeManager.MixedRounds = false;
            _currentModeIndex = 1;
            Debug.Log("Switching to Group Rounds");
        }
        else if (ModeManager.GroupRounds)
        {
            ModeManager.SingleRounds = false;
            ModeManager.GroupRounds = false;
            ModeManager.MixedRounds = true;
            _currentModeIndex = 2;
            Debug.Log("Switching to Mixed Rounds");
        }
        else if (ModeManager.MixedRounds)
        {
            continueSim = false;
        }

        if (continueSim)
        {
            // Not really over
            GameplayController.SimOver = false;
        }
    }

    public void OnEnemyDeath()
    {
    }
}
