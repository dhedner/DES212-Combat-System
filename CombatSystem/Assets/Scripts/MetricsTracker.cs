using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Playables;

public struct MetricsTrackerKey
{
    public int Fight;
    public int Round;
    public PlayStyle PlayStyle;
}

public class MetricsTracker
{
    private int _fights;
    private int _rounds;

    public IDictionary<MetricsTrackerKey, int> Victories { get; private set; }
    public IDictionary<MetricsTrackerKey, int> Defeats { get; private set; }
    public IDictionary<MetricsTrackerKey, float> DamageDone { get; private set; }
    public IDictionary<MetricsTrackerKey, float> RoundTimes { get; private set; }
    public int CurrentIteration { get; private set; }
    public MetricsTrackerKey[] Keys
    {
        get
        {
            return Victories.Keys.ToArray();
        }
    }

    // To string
    public override string ToString()
    {
        return $"{Keys}";
    }

    private IDictionary<MetricsTrackerKey, IDictionary<PlayerAbilities, float>> _averageAbilityUsage;
    private IDictionary<MetricsTrackerKey, string> _enemyNames;
    private IDictionary<MetricsTrackerKey, RoundCategory> _roundCategories;
    private IDictionary<MetricsTrackerKey, int> _totalEnemies;
    private IDictionary<MetricsTrackerKey, List<int>> _enemyDeaths;
    private IDictionary<MetricsTrackerKey, List<float>> _playerHealth;

    public MetricsTracker(int fights, int rounds)
    {
        _fights = fights;
        _rounds = rounds;
        Reset();
    }

    public void AdvanceIteration()
    {
        CurrentIteration++;
    }

    public void RecordRoundData(
        int fight,
        int round,
        PlayStyle playStyle,
        RoundDescriptor roundDescriptor,
        Player player,
        float damage,
        float time)
    {
        var key = new MetricsTrackerKey { Fight = fight, Round = round, PlayStyle = playStyle };
        if (!Victories.ContainsKey(key))
        {
            Victories.Add(key, 0);
            Defeats.Add(key, 0);
            DamageDone.Add(key, 0);
            RoundTimes.Add(key, 0);
            _totalEnemies.Add(key, 0);
            _enemyNames.Add(key, "");
            _roundCategories.Add(key, RoundCategory.Melee); // Dummy
            _playerHealth.Add(key, new List<float>());
        }

        if (!_enemyDeaths.ContainsKey(key))
        {
            _enemyDeaths.Add(key, new List<int>());
        }

        if (_enemyDeaths[key].Count <= CurrentIteration)
        {
            _enemyDeaths[key].Add(0);
        }

        Victories[key] += player.HitPoint.HitPoints > 0 ? 1 : 0;
        Defeats[key] += player.HitPoint.HitPoints > 0 ? 0 : 1;
        DamageDone[key] += damage;
        RoundTimes[key] += time;

        var query = from e in roundDescriptor.Enemies
                    group e by e.Prefab.ToString() into g
                    select g.Count() + "x" + g.First().Prefab.ToString().Split(' ')[0];

        _totalEnemies[key] = roundDescriptor.Enemies.Count();
        _enemyNames[key] = string.Join('/', query);
        _roundCategories[key] = roundDescriptor.Category;

        if (_playerHealth[key].Count <= CurrentIteration)
        {
            _playerHealth[key].Add(0);
        }

        _playerHealth[key][CurrentIteration] = player.HitPoint.HitPoints;
    }

    public void RecordAction(MetricsTrackerKey key, PlayerAbilities action)
    {
        if (!_averageAbilityUsage.ContainsKey(key))
        {
            _averageAbilityUsage.Add(key, new Dictionary<PlayerAbilities, float>());
        }

        if (!_averageAbilityUsage[key].ContainsKey(action))
        {
            _averageAbilityUsage[key].Add(action, 0);
        }

        _averageAbilityUsage[key][action] += 1;
    }

    public void RecordEnemyDeath(MetricsTrackerKey key)
    {
        if (!_enemyDeaths.ContainsKey(key))
        {
            _enemyDeaths.Add(key, new List<int>());
        }

        if (_enemyDeaths[key].Count <= CurrentIteration)
        {
            _enemyDeaths[key].Add(0);
        }

        _enemyDeaths[key][CurrentIteration] += 1;
    }

    public void Reset()
    {
        Victories = new Dictionary<MetricsTrackerKey, int>();
        Defeats = new Dictionary<MetricsTrackerKey, int>();
        DamageDone = new Dictionary<MetricsTrackerKey, float>();
        RoundTimes = new Dictionary<MetricsTrackerKey, float>();
        _averageAbilityUsage = new Dictionary<MetricsTrackerKey, IDictionary<PlayerAbilities, float>>();
        _enemyNames = new Dictionary<MetricsTrackerKey, string>();
        _roundCategories = new Dictionary<MetricsTrackerKey, RoundCategory>();
        _enemyDeaths = new Dictionary<MetricsTrackerKey, List<int>>();
        _totalEnemies = new Dictionary<MetricsTrackerKey, int>();
        _playerHealth = new Dictionary<MetricsTrackerKey, List<float>>();
    }

    public float GetRating(MetricsTrackerKey key)
    {
        float total = 0.0f;
        for (int i = 0; i < _playerHealth[key].Count; i++)
        {
            float playerHealth = _playerHealth[key][i];
            if (playerHealth > 0)
            {
                // the total is the sum of the percentage of health remaining
                // if the player's max health was 150, the equation would be:
                total += (playerHealth / 150.0f) * 100.0f;
            }
            else
            {
                total -= (1f - (_enemyDeaths[key][i] / (float)_totalEnemies[key])) * 100.0f;
            }
        }

        return total / _playerHealth[key].Count;
    }

    public float GetAverageAbilityUsage(MetricsTrackerKey key, PlayerAbilities action)
    {
        //float total = 0;
        //foreach (var ability in _averageAbilityUsage)
        //{
        //    total += (float)ability.Value;
        //}
        //return _averageAbilityUsage[action] / total;

        if (!_averageAbilityUsage.ContainsKey(key) || !_averageAbilityUsage[key].ContainsKey(action))
        {
            return 0;
        }

        return _averageAbilityUsage[key][action] / _averageAbilityUsage[key].Values.Sum();
    }

    public RoundCategory GetRoundCategory(MetricsTrackerKey key)
    {
        return _roundCategories[key];
    }

    public string GetEnemyNames(MetricsTrackerKey key)
    {
        return _enemyNames[key];
    }
}
