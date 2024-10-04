using System;

public interface IPlayMode
{
    void OnNewRound();

    void OnNewFight();

    void OnRoundFinished(int fight, int round, float damage, float time, Player player);

    void OnSimOver();

    void OnEnemyDeath();
}
