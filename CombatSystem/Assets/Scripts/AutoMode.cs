using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoMode : MonoBehaviour, IPlayMode
{
    public ModeManager ModeManager;
    public DecisionMaker DecisionMaker;

    public int iterations;

    public PlayStyle CurrentAI;

    public void Start()
    {
        DecisionMaker.playStyle = CurrentAI;
    }

    public void Update()
    {
        if (ModeManager.CurrentMode != Mode.Auto)
        {
            return;
        }

        var now = DateTime.Now;
        if (now - ModeManager.lastActionTime > TimeSpan.FromMilliseconds(ModeManager.actionDelay))
        {
            ModeManager.lastActionTime = now;

            var state = GameplayController.GameplayState;
            PlayerAbilities action = DecisionMaker.MakeDecision(state);

            // if the action is not valid, just return
            if (action == 0)
            {
                return;
            }

            GameplayController.TriggerAction(action);
        }
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
            Debug.Log("Switching to Group Rounds");
        }
        else if (ModeManager.GroupRounds)
        {
            ModeManager.SingleRounds = false;
            ModeManager.GroupRounds = false;
            ModeManager.MixedRounds = true;
            Debug.Log("Switching to Mixed Rounds");
        }
        else if (ModeManager.MixedRounds)
        {
            ModeManager.SingleRounds = true;
            ModeManager.GroupRounds = false;
            ModeManager.MixedRounds = false;

            if (CurrentAI == PlayStyle.Random)
            {
                CurrentAI = PlayStyle.Smart;

                Debug.Log("Switching to Single Rounds with Smart AI");
            }
            else
            {
                CurrentAI = PlayStyle.Random;

                Debug.Log("Iterations left: " + iterations);

                if (--iterations == 0)
                {
                    continueSim = false;
                }
            }
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
