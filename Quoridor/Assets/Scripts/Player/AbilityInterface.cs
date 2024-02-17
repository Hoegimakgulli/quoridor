using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbility
{
    PlayerAbility.EAbilityType abilityType { get; }
    bool canEvent { get; set; }
    bool Event();
    void Reset();
}
public interface IActiveAbility
{
    DisposableButton.ActiveCondition activeCondition { get; }
    List<Vector2Int> attackRange { get; }
    List<Vector2Int> attackScale { get; }
    Vector2Int targetPos { get; set; }
}
