using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UIElements;

public enum PlayStyle
{
    Random,
    Smart
}

public class GameplayState : IComparable<GameplayState>, IEquatable<GameplayState>
{
    public PlayerAbilities EnabledAbilities
    {
        get
        {
            PlayerAbilities maskOut = 0;
            return _enabledActions & ~maskOut;
        }

        set
        {
            _enabledActions = value;
        }
    }

    public int NumberOfEnemies { get; set; }
    public float PlayerHealth { get; set; }

    public float PlayerInsight { get; set; }

    private PlayerAbilities _enabledActions;

    public override string ToString()
    {
        return $"{EnabledAbilities}";
    }

    public bool IsAbilityEnabled(PlayerAbilities button)
    {
        return EnabledAbilities.HasFlag(button);
    }

    // Implement IComparable if the dictionary is a SortedDictionary
    public int CompareTo(GameplayState other)
    {
        return EnabledAbilities.CompareTo(other.EnabledAbilities);
    }

    public bool Equals(GameplayState obj)
    {
        return obj != null && EnabledAbilities == obj.EnabledAbilities;
    }

    public override int GetHashCode()
    {
        return EnabledAbilities.GetHashCode();
    }

    //static public GameplayState Parse(string str)
    //{
    //    var parts = str.Split('-');
    //    var state = new GameplayState();
    //    state.EnabledAbilities = (PlayerAbilities)Enum.Parse(typeof(PlayerAbilities), parts[0]);
    //    return state;
    //}
}

public class DecisionMaker : MonoBehaviour
{
    public ModeManager ModeManager;
    private IDictionary<GameplayState, SortedDictionary<PlayerAbilities, double>> abilityUsage
        = new SortedDictionary<GameplayState, SortedDictionary<PlayerAbilities, double>>();

    public PlayStyle playStyle
    {
        get { return _playStyle; }
        set
        {
            _playStyle = value;
        }
    }

    private PlayStyle _playStyle = PlayStyle.Random;

    public PlayerAbilities MakeDecision(GameplayState state)
    {
        var random = new System.Random();

        var distribution = GetActionDistribution(state);

        // Roulette wheel selection
        var r = random.NextDouble();
        double sum = 0.0;
        foreach (var option in distribution)
        {
            sum += option.Value;
            if (r < sum)
            {
                return option.Key;
            }
        }

        return 0;
        //throw new Exception("No decision made");
    }

    public IDictionary<PlayerAbilities, double> GetActionDistribution(GameplayState state)
    {
        // Debug.LogWarning($"No decision data for state={state}, using random actions");
        // if our playstyle is random, return a uniform distribution
        if (playStyle == PlayStyle.Random)
        {
            return GenerateRandomDistribution(state);
        }
        else
        {
            return GenerateSmartDistribution(state);
        }
    }

    private void NormalizeDistribution(IDictionary<PlayerAbilities, double> distribution)
    {
        double sum = 0;
        foreach (var option in distribution.Values)
        {
            sum += option;
        }

        var keys = distribution.Keys.ToArray();
        foreach (var option in keys)
        {
            distribution[option] /= sum;
        }
    }

    private IDictionary<PlayerAbilities, double> GenerateRandomDistribution(GameplayState state)
    {
        var distribution = new Dictionary<PlayerAbilities, double>();
        var options = Enum.GetValues(typeof(PlayerAbilities));
        foreach (PlayerAbilities option in options)
        {
            distribution[option] = state.IsAbilityEnabled(option) ? 1.0 : 0.0;
        }
        // Add "Nothing" as an option
        distribution[0] = 1.0;

        NormalizeDistribution(distribution);
        return distribution;
    }

    private IDictionary<PlayerAbilities, double> GenerateSmartDistribution(GameplayState state)
    {
        var distribution = new Dictionary<PlayerAbilities, double>();
        var options = Enum.GetValues(typeof(PlayerAbilities));
        foreach (PlayerAbilities option in options)
        {
            distribution[option] = state.IsAbilityEnabled(option) ? 1.0 : 0.0;
        }
        // Add "Nothing" as an option
        distribution[0] = 1.0;

        // if there's only one enemy or the player would die on use, don't use the 6th ability
        if (state.NumberOfEnemies == 1 || state.PlayerHealth <= 10)
        {
            distribution[PlayerAbilities.CullTheIgnorant] = 0.0;
        }

        // if there's multiple enemies, use the 3rd ability less often
        if (state.NumberOfEnemies > 1 && state.PlayerInsight <= 20)
        {
            distribution[PlayerAbilities.EldritchWhisper] = 0.5;
        }

        // if the player's health is low, use the 5th ability more often
        if (state.PlayerHealth < 30)
        {
            distribution[PlayerAbilities.RitualOfReason] = 0.5;
        }

        // if the player's insight is high enough, don't use the 4th ability
        if (state.PlayerInsight > 30)
        {
            distribution[PlayerAbilities.ArcaneEnlightenment] = 0.0;
        }

        NormalizeDistribution(distribution);
        return distribution;
    }
}

