using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class TelemetryMode : MonoBehaviour, IPlayMode
{
    public ModeManager ModeManager;
    public DecisionMaker DecisionMaker;
    public TelemetryWriter TelemetryWriter;
    private MetricsTracker MetricsTracker;

    public int iterations;
    private int _currentModeIndex = 0;

    public PlayStyle CurrentAI;

    public void Start()
    {
        DecisionMaker.playStyle = CurrentAI;
        MetricsTracker = new MetricsTracker(iterations, GameplayController.Rounds);
    }

    public void Update()
    {
        if (ModeManager.CurrentMode != Mode.Telemetry)
        {
            return;
        }

        DecisionMaker.playStyle = CurrentAI;

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

            var key = new MetricsTrackerKey { Fight = _currentModeIndex, Round = GameplayController.RoundCount - 1, PlayStyle = CurrentAI };
            MetricsTracker.RecordAction(key, action);
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
        MetricsTracker.RecordRoundData(
            _currentModeIndex,
            round,
            CurrentAI,
            GameplayController.CurrentRoundDescriptor[round],
            player,
            damage,
            time);
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
            ModeManager.SingleRounds = true;
            ModeManager.GroupRounds = false;
            ModeManager.MixedRounds = false;
            _currentModeIndex = 0;

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
                    TelemetryWriter.WriteData(MetricsTracker);
                }
                else
                {
                    MetricsTracker.AdvanceIteration();
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
        var key = new MetricsTrackerKey { Fight = _currentModeIndex, Round = GameplayController.RoundCount - 1, PlayStyle = CurrentAI };
        MetricsTracker.RecordEnemyDeath(key);
    }
}
