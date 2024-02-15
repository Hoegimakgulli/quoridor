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
