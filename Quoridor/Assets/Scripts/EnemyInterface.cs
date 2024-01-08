using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IMove
{
    void GetShortRoad(List<Path> path);
    void GetBackRoad();
    void GetHoldRoad();
}

public interface IAttack
{
    void AttackPlayer();
}

public interface IDead
{
    void DieEnemy();
}
