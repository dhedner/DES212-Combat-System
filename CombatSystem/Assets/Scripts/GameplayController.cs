/*******************************************************************************
Author:    Benjamin Ellinger
DP Email:  bellinge@digipen.edu
Date:      2/1/2022
Course:    DES 212

Description:
	This file is part of the framework for the 1D Combat Simulator assignment in
	DES 212 (System Design Methods). It can be freely used and modified by students
	for that assignment.
	
    This component controls the entire combat simulation. This component is added to
    a game object whose only purpose to contain this functionality, but using a
    ScriptedObject would potentially be a more advanced	way of doing this.
	
*******************************************************************************/

//Standard Unity component libraries
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TMPro;
using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEditor;

[Flags]
public enum PlayerAbilities
{
    ForbiddenTruth = 0x0001,
    RayOfClarity = 0x0002,
    EldritchWhisper = 0x0004,
    ArcaneEnlightenment = 0x0008,
    RitualOfReason = 0x0010,
    CullTheIgnorant = 0x0020
}

public enum RoundCategory
{
    Melee,
    Ranged,
    Support,
    Elite,
    Mixed
}

public class EnemySpawnDescriptor
{
    public GameObject Prefab;
    public Vector3 Position;
    public Quaternion Rotation;
}

public class RoundDescriptor
{
    public RoundCategory Category;
    public List<EnemySpawnDescriptor> Enemies;
}

public class GameplayController : MonoBehaviour
{
    public static float DT;

    //How many different AI types (and therefore how many "fights") do we want?
    public static int Fights = 1;
    public static int FightCount = 0;
    //How many rounds is each "fight"?
    public static int Rounds = 6;
    public static int RoundCount = 0;
    private static bool IsFightStart = true;
    public static float RoundTimeElapsed = 0.0f;
    public static float RoundDamageDone = 0.0f;
    public static bool RoundOver { get; private set; } = false; //Did the current round just end?
    public static bool RoundStart { get; private set; } = false; //Is a new round just starting (make sure the player has time to find a target)?

    //How long a delay between rounds?
    public static float RoundDelay = 3.0f;
    public static float RoundTimer = 3.0f;

    public static bool SimOver = false; //Have all the fights been completed?

    //How far from the center of the screen is the "edge" of the arena?
    public static float EdgeDistance = 8.0f;
    //How far from the center of the screen do combatants start?
    public static float StartingX = 4.0f;

    //Need a reference to the player
    public static Player Player;

    //We will use the UI canvas a lot
    public static GameObject Canvas;

    //References for text prefabs and enemy prefabs,
    public static GameObject InfoTextPrefab;
    public static GameObject StaticInfoTextPrefab;

    public static GameObject AbyssalGruntPrefab; //Should really be an array or dictionary.
    public static GameObject VoidCallerPrefab;
    public static GameObject VeilingWraithPrefab;
    public static GameObject AbyssalBrutePrefab;
    public static GameObject WailingMadnessPrefab;
    public static GameObject ApostleOfChaosPrefab;

    public static List<RoundDescriptor> SingleRoundPrefabs;

    public static List<RoundDescriptor> GroupRoundPrefabs;

    public static List<RoundDescriptor> MixedRoundPrefabs;

    public static event Action OnNewRound;
    public static event Action OnNewFight;
    public static event Action OnSimOver;
    public static event Action OnEnemyDeath;

    public delegate void RoundFinished(int fight, int round, float damage, float time, Player player);
    public static event RoundFinished OnRoundFinished;

    public static ModeManager ModeManager;

    // Fight rating comparison is SmartAI score - RandomAI score (Random has no comparison)

    public static GameplayState GameplayState
    {
        get
        {
            return new GameplayState
            {
                EnabledAbilities = AvailableAbilities,
                NumberOfEnemies = (from o in GameObject.FindObjectsOfType<Enemy>()
                                   where o.HitPoint.HitPoints > 0
                                   select o).Count(),
                PlayerHealth = Player.HitPoint.HitPoints,
                PlayerInsight = Player.Insight
            };
        }
    }


    public static PlayerAbilities AvailableAbilities
    {
        get
        {
            PlayerAbilities abilities = 0;
            for (int i = 0; i < Player.Abilities.Count; i++)
            {
                if (Player.Abilities[i].IsReady())
                {
                    abilities |= (PlayerAbilities)(1 << i);
                }
            }

            return abilities;
        }
    }

    public static IEnumerable<HitPointComponent> TargetForEnemy
    {
        get
        {
            return from o in GameObject.FindObjectsOfType<HitPointComponent>()
                   where o.IsFrenzied || o.name == "Player"
                   orderby o.IsFrenzied descending
                   select o;
        }
    }

    public static List<RoundDescriptor> CurrentRoundDescriptor
    {
        get
        {
            if (ModeManager.SingleRounds)
            {
                return SingleRoundPrefabs;
            }
            else if (ModeManager.GroupRounds)
            {
                return GroupRoundPrefabs;
            }
            else
            {
                return MixedRoundPrefabs;
            }
        }
    }

    //Start is called before the first frame update
    void Start()
    {
        //Get a reference to the canvas (used for UI objects)
        Canvas = GameObject.Find("Canvas");

        //Get a reference to the player's game object
        Player = GameObject.Find("Player").GetComponent<Player>();

        //Load all the prefabs we are going to use
        InfoTextPrefab = Resources.Load("Prefabs/InfoText") as GameObject;
        StaticInfoTextPrefab = Resources.Load("Prefabs/StaticInfoText") as GameObject;

        AbyssalGruntPrefab = Resources.Load("Prefabs/AbyssalGrunt") as GameObject;
        VoidCallerPrefab = Resources.Load("Prefabs/VoidCaller") as GameObject;
        VeilingWraithPrefab = Resources.Load("Prefabs/VeilingWraith") as GameObject;
        AbyssalBrutePrefab = Resources.Load("Prefabs/AbyssalBrute") as GameObject;
        WailingMadnessPrefab = Resources.Load("Prefabs/WailingMadness") as GameObject;
        ApostleOfChaosPrefab = Resources.Load("Prefabs/ApostleOfChaos") as GameObject;

        ModeManager = GameObject.Find("World").GetComponent<ModeManager>();

        // Define which prefabs to use for each round
        CreateRounds();
    }

    void Update()
    {
        //Get the actual delta time, but cap it at actionDelay Time
        if (ModeManager.FastPlay)
        {
            DT = ModeManager.actionDelay;
        }
        else if (Time.deltaTime < ModeManager.actionDelay)
        {
            DT = Time.deltaTime;
        }
        else
        {
            DT = ModeManager.actionDelay;
        }

        //If the ESC key is pressed, exit the program.
        if (Input.GetKeyDown(KeyCode.Escape) == true)
            Application.Quit();

        //The simulation is over, so stop updating.
        if (FightCount >= Fights)
        {
            if (SimOver == false) //Did the simulation just end?
            {
                SimOver = true;
                OnSimOver?.Invoke();

                if (SimOver)
                {
                    SpawnInfoText("SIMULATION OVER", true);
                }
                else
                {
                    // New iteration
                    IsFightStart = true;
                    RoundCount = 0;
                    FightCount = 0;
                }
            }
            return;
        }

        //It's the start of a fight, so start a new round.
        if (IsFightStart)
        {
            NewRound();
            IsFightStart = false;
        }

        RoundOver = IsRoundOver();
        if (RoundOver == false)
        {
            RoundTimeElapsed += DT; //Accumulate the SIMULATED time for telemetry data.
        }
        else if (RoundTimer > 0.0f) //The round is over, but this is the delay before a new round.
        {
            RoundTimer -= DT; //Update the round delay timer.
        }
        else //Time for a new round.
        {
            OnRoundFinished?.Invoke(
                FightCount,
                RoundCount - 1,
                RoundDamageDone,
                RoundTimeElapsed,
                Player);
            NewRound();
        }
    }

    public static void TriggerAction(PlayerAbilities action)
    {
        //Debug.Log($"Autoplay triggered {action}");
        Player.UseAbility(action);

    }

    //The round is over if either the player is dead or all enemies are.
    static bool IsRoundOver()
    {
        //Player is dead.
        if (Player.HitPoint.HitPoints == 0.0f)
        {
            if (RoundOver == false) //Player just died.
            {
                SpawnInfoText("DEFEAT...");
            }
            return true;
        }
        //Enemies are dead.
        if (Player.Target == null)
        {
            if (RoundStart == true) //Make sure player has a chance to find a target at the start of a round.
                return false;
            if (RoundOver == false) //Last enemy just died.
            {
                SpawnInfoText("VICTORY!!!");
            }
            return true;
        }
        //Round is not over.
        RoundStart = false;
        return false;
    }

    //Reset everything for the new round.
    public static void NewRound()
    {
        ++RoundCount;

        //Clear out any remaining enemies.
        ClearEnemies();

        // The whole fight is over, so start a new one
        if (RoundCount > Rounds)
        {
            NewFight();
            return;
        }

        var rounds = CurrentRoundDescriptor;

        Debug.Log("Round " + RoundCount + " of " + Rounds + " in fight " + (FightCount + 1) + " of " + Fights);

        foreach (var enemy in rounds[RoundCount - 1].Enemies)
        {
            Instantiate(enemy.Prefab, enemy.Position, enemy.Rotation, null);
        }

        //Each fight should be one AI type against one enemy type multiple times. And then each AI type
        //against a group of the same type multiple times. And then each AI type against a mixed group
        //multiple times. Group mode and mixed mode will require different logic, etc.
        // Fire event for new round
        OnNewRound?.Invoke();

        //Call the Initialize() functions for the player.
        Player.Initialize();

        SpawnInfoText("ROUND " + RoundCount);

        //Reset the round delay timer (and round start flag) for after this new round ends.
        RoundTimeElapsed = 0.0f;
        RoundDamageDone = 0.0f;
        RoundTimer = RoundDelay;
        RoundStart = true;
    }

    //Reset everything for the new fight.
    public static void NewFight()
    {
        FightCount++;
        OnNewFight?.Invoke();
        RoundCount = 0;
        IsFightStart = true;
    }

    //Destroy all the enemy game objects.
    static void ClearEnemies()
    {
        //Find all the game objects that have an Enemy component.
        var enemies = FindObjectsOfType<Enemy>();
        foreach (Enemy enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    // Restart simulation
    public static void RestartSim()
    {
        FightCount = 0;
        RoundCount = 0;
        IsFightStart = true;
        RoundStart = false;
        RoundOver = false;
        SimOver = false;
    }

    public static void NotifyEnemyDeath()
    {
        OnEnemyDeath?.Invoke();
    }

    //Spawn text at the center of the screen.
    //If set to static, that just means it doesn't move.
    static void SpawnInfoText(string text, bool isStatic = false)
    {
        SpawnInfoText(new Vector3(0, 0, 0), text, isStatic);
    }

    //Spawn text wherever you want.
    //If set to static, that just means it doesn't move.
    static void SpawnInfoText(Vector3 location, string text, bool isStatic = false)
    {
        //Throw up some text by calling the Unity engine function Instantiate().
        //Pass in the appropriate InfoText prefab, its position, its rotation (none in this case),
        //and its parent (the canvas because this is text). Then we get the
        //Text component from the new game object in order to set the text itself.
        TextMeshProUGUI infotext;
        if (isStatic)
            infotext = Instantiate(StaticInfoTextPrefab, location, Quaternion.identity, Canvas.transform).GetComponent<TextMeshProUGUI>();
        else
            infotext = Instantiate(InfoTextPrefab, location, Quaternion.identity, Canvas.transform).GetComponent<TextMeshProUGUI>();

        infotext.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        //Set the text.
        infotext.text = text;
    }

    void CreateRounds()
    {
        SingleRoundPrefabs = new List<RoundDescriptor>
        {
            new RoundDescriptor
            {
                Category = RoundCategory.Melee,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX + 1, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Ranged,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 1, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                },
            },
            {
                new RoundDescriptor
                {
                    Category = RoundCategory.Support,
                    Enemies = new List<EnemySpawnDescriptor>
                    {
                        new EnemySpawnDescriptor
                        {
                            Prefab = VeilingWraithPrefab,
                            Position = new Vector3(StartingX + 1, 0, 0),
                            Rotation = Quaternion.Euler(0, 0, 90)
                        }
                    }
                }
            },
            {
                new RoundDescriptor
                {
                    Category = RoundCategory.Melee,
                    Enemies = new List<EnemySpawnDescriptor>
                    {
                        new EnemySpawnDescriptor
                        {
                            Prefab = AbyssalBrutePrefab,
                            Position = new Vector3(StartingX + 1, 0, 0),
                            Rotation = Quaternion.Euler(0, 0, 90)
                        }
                    }
                }
            },
            {
                new RoundDescriptor
                {
                    Category = RoundCategory.Ranged,
                    Enemies = new List<EnemySpawnDescriptor>
                    {
                        new EnemySpawnDescriptor
                        {
                            Prefab = WailingMadnessPrefab,
                            Position = new Vector3(StartingX + 1, 0, 0),
                            Rotation = Quaternion.Euler(0, 0, 90)
                        }
                    }
                }
            },
            {
                new RoundDescriptor
                {
                    Category = RoundCategory.Elite,
                    Enemies = new List<EnemySpawnDescriptor>
                    {
                        new EnemySpawnDescriptor
                        {
                            Prefab = ApostleOfChaosPrefab,
                            Position = new Vector3(StartingX + 1, 0, 0),
                            Rotation = Quaternion.Euler(0, 0, 90)
                        }
                    }
                }
            }
        };

        GroupRoundPrefabs = new List<RoundDescriptor>
        {
            new RoundDescriptor
            {
                Category = RoundCategory.Melee,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    //new EnemySpawnDescriptor
                    //{
                    //    Prefab = AbyssalGruntPrefab,
                    //    Position = new Vector3(StartingX + 2, 0, 0),
                    //    Rotation = Quaternion.Euler(0, 0, 90)
                    //},
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Ranged,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX - 0.5f, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 2.5f, 0.75f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 2.5f, -0.75f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Support,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Melee,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Ranged,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Elite,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = ApostleOfChaosPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = ApostleOfChaosPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = ApostleOfChaosPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            }
        };

        MixedRoundPrefabs = new List<RoundDescriptor>
        {
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalGruntPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = AbyssalBrutePrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VoidCallerPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = WailingMadnessPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
            new RoundDescriptor
            {
                Category = RoundCategory.Mixed,
                Enemies = new List<EnemySpawnDescriptor>
                {
                    new EnemySpawnDescriptor
                    {
                        Prefab = ApostleOfChaosPrefab,
                        Position = new Vector3(StartingX + 1, -1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = VeilingWraithPrefab,
                        Position = new Vector3(StartingX, 0, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    },
                    new EnemySpawnDescriptor
                    {
                        Prefab = ApostleOfChaosPrefab,
                        Position = new Vector3(StartingX + 1, 1.5f, 0),
                        Rotation = Quaternion.Euler(0, 0, 90)
                    }
                }
            },
        };
    }
}