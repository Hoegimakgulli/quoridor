using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Soldier : Player
{
    protected override void SignatureAbility() // 보호막
    {
        Vector3 newPosition = transform.position + GameManager.gridSize * new Vector3(0, -2, 0);
        if (GameManager.enemyPositions.Contains(newPosition)) { } // 어떻게 해야하는가
        if (newPosition.y / GameManager.gridSize < -4) newPosition.y = GameManager.gridSize * -4;
        transform.position = newPosition;
        GameManager.playerPosition = newPosition / GameManager.gridSize;

        canSignAbility = false;
    }

    public override void Die()
    {
        if (!canSignAbility) base.Die();
        else SignatureAbility();
    }
}
