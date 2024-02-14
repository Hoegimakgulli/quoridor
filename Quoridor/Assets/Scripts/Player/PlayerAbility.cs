using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class PlayerAbility : MonoBehaviour
{
    public enum EAbilityType { DiePassive, AttackPassive, KillPassive, Active }

    Player player;
    GameManager gameManager;

    public List<IAbility> abilities = new List<IAbility>() { };

    private void Start()
    {
        player = GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        abilities.Add(new Shield(this.gameObject));
    }
    public void Reset()
    {
        foreach (var ability in abilities)
        {
            if (ability.isEnable) ability.Reset();
        }
    }
    public void PostAttackEvent(bool isDead)
    {
        if (isDead)
        {
            foreach (var ability in abilities)
            {
                if (ability.abilityType == EAbilityType.KillPassive && ability.isEnable)
                {
                    ability.Event();
                }
            }
        }
        else
        {
            foreach (var ability in abilities)
            {
                if (ability.abilityType == EAbilityType.AttackPassive && ability.isEnable)
                {
                    ability.Event();
                }
            }
        }
    }
    public bool DieEvent()
    {
        bool shouldDie = true;
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.DiePassive && ability.isEnable)
            {
                shouldDie &= ability.Event();
            }
        }
        return shouldDie;
    }

    class Shield : IAbility
    {
        private EAbilityType mAbilityType = EAbilityType.DiePassive;
        private bool mbEnable = false;
        private bool mbEvent = false;
        private int mCount = 1;

        GameObject thisObject;
        public Shield(GameObject gameObject) { thisObject = gameObject; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool isEnable { get { return mbEnable; } set { mbEnable = value; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Vector3 newPosition = GameManager.playerPosition + new Vector3(0, -2, 0);
            if (GameManager.enemyPositions.Contains(newPosition))
            {
                newPosition = GameManager.playerPosition + new Vector3(0, -1, 0);
                if (GameManager.enemyPositions.Contains(newPosition)) newPosition = GameManager.playerPosition;
            }
            if (newPosition.y / GameManager.gridSize < -4) newPosition.y = GameManager.gridSize * -4;
            thisObject.transform.position = newPosition;
            GameManager.playerPosition = newPosition / GameManager.gridSize;

            isEnable = false;
            mCount--;

            return false;
        }
        public void Reset()
        {
            if (mCount > 0)
            {
                canEvent = true;
            }
            else
            {
                isEnable = false;
                canEvent = false;
            }
        }
    }
    void Reload()
    {
        player.canAttack = true;
    }
}