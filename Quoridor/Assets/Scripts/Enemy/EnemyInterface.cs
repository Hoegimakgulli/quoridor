using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMove
{
    void GetShortRoad(List<Vector2> path, bool isPlayer);
    void GetBackRoad();
    void GetHoldRoad();
}

public interface IAttack
{
    void AttackPlayer();
}

public interface IDamage
{
    bool AttackedEnemy(int playerAtk);
}

public interface IDead
{
    void DieEnemy();
}
