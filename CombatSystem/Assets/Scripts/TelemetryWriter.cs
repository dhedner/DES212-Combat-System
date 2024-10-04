using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;

public class TelemetryWriter : MonoBehaviour
{
    public TelemetryMode TelemetryMode;

    private string date;

    void Start()
    {
        date = DateTime.Now.ToString("yyyyMMdd-\\THHmmss\\Z");
    }

    public void WriteData(MetricsTracker metricsTracker)
    {
        Debug.Log("Writing data");

        if (!File.Exists($"FightData{date}.csv"))
        {
            string abilityPercents = string.Join(",", Enum.GetNames(typeof(PlayerAbilities)));
            WriteToCSV($"FightData{date}.csv", $"AI TYPE,CATEGORY,ENEMIES,WINS,LOSSES,RATING,RATING DIFFERENCE,WIN%,DPS,ROUND LENGTH,{abilityPercents}");
        }

        foreach (var key in metricsTracker.Keys)
        {
            var aiType = key.PlayStyle.ToString();
            var rounds = GameplayController.CurrentRoundDescriptor;
            var category = metricsTracker.GetRoundCategory(key).ToString();
            var enemies = metricsTracker.GetEnemyNames(key);
            var victories = metricsTracker.Victories[key];
            var defeats = metricsTracker.Defeats[key];

            var winPercent = (float)Math.Round((float)victories / (victories + defeats) * 100, 1);
            var roundLength = (float)Math.Round(metricsTracker.RoundTimes[key] / metricsTracker.CurrentIteration, 1);
            var dps = (float)Math.Round(metricsTracker.DamageDone[key] / metricsTracker.RoundTimes[key], 1);
            var rating = (float)Math.Round(metricsTracker.GetRating(key), 1);
            // TODO: Rating Difference (only for SmartAI)

            // if we're in random mode, print the rating again
            // if we're in smart mode, take the smart rating - the random rating of the same key
            var ratingDifference = 0.0f;
            if (key.PlayStyle == PlayStyle.Random)
            {
                ratingDifference = (float)Math.Round(metricsTracker.GetRating(key), 1);
            }
            else
            {
                var randomKey = new MetricsTrackerKey { Fight = key.Fight, Round = key.Round, PlayStyle = PlayStyle.Random };
                ratingDifference = (float)Math.Round(metricsTracker.GetRating(key) - metricsTracker.GetRating(randomKey), 1);
            }

            var abilityQuery = from PlayerAbilities ability in Enum.GetValues(typeof(PlayerAbilities))
                                select Math.Round(metricsTracker.GetAverageAbilityUsage(key, ability), 1);
            var abilityUsage = string.Join(",", abilityQuery);

            WriteToCSV($"FightData{date}.csv", $"{aiType},{category},{enemies},{victories},{defeats},{rating},{ratingDifference},{winPercent},{dps},{roundLength},{abilityUsage}");
        }
    }

    private void WriteToCSV(string filename, string data)
    {
        using StreamWriter dataStream = new StreamWriter(filename, true);

        if (dataStream != null)
        {
            dataStream.WriteLine(data);
            dataStream.Flush();
        }
    }
}
