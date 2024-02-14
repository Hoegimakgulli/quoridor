using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Angelica : Player
{
    protected override void Reset()
    {
        base.Reset();
        canSignAbility = true;
    }
    protected override bool? Attack()
    {
        bool? baseAttack = base.Attack();
        if (baseAttack == null) return null;
        if ((bool)baseAttack && canSignAbility)
        {
            canAttack = true;
            canSignAbility = false;
        }
        return baseAttack;
    }
}
