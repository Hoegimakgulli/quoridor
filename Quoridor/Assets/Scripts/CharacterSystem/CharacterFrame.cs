using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Define
{
    public enum ECharacter
    {
        None = -1,
        Player,
        Enemy
    };

    public enum EPlayerControlStatus
    {
        None = -1, 
        Move, 
        Build, 
        Attack, 
        Ability, 
        Destroy
    };

    public enum EPlayerAxisField
    {
        None = -1,
        Move,
        Attack,
        Ability
    };
}

namespace CharacterState
{
    public class MoveIndex
    {
        Dictionary<int, List<Vector2>> moveIndex = new Dictionary<int, List<Vector2>>();

        public List<Vector2> GetMoveRange(int Index)
        {
            return moveIndex[Index];
        }
    }

    public class AttackIndex
    {
        Dictionary<int, List<Vector2>> attackIndex = new Dictionary<int, List<Vector2>>();

        public List<Vector2> GetAttackRange(int Index)
        {
            return attackIndex[Index];
        }
    }
}

namespace SpawnUtil
{
    public class SpawnObject : MonoBehaviour
    {

    }
}