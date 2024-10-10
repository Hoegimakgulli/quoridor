﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankerCharacter : BaseCharacter
{
    public TankerCharacter(CharacterController controller) : base(controller) { }

    public override void Attack()
    {
        base.Attack();
    }

    public override void Move()
    {
        base.Move();
    }

    public override void Ability()
    {
        base.Ability();
    }

    public override void Build()
    {
        base.Build();
    }

    public override void HealthRecovery(int recovery)
    {
        base.HealthRecovery(recovery);
    }

    public override void TakeDamage(BaseCharacter baseCharacter, int damage = 0)
    {
        base.TakeDamage(baseCharacter, damage);
    }
}