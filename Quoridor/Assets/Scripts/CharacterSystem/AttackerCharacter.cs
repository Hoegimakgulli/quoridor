using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackerCharacter : BaseCharacter
{
    public AttackerCharacter(CharacterController controller) : base(controller) { }

    public override void Attack()
    {
        base.Attack();
    }

    public override void Move(Vector2 targetPos)
    {
        base.Move(targetPos);
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
