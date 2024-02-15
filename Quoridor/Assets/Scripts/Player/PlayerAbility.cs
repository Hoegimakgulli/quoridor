using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
#if UNITY_EDITOR
namespace UnityEditor
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute), true)]
    public class ReadOnlyAttributeDrawer : PropertyDrawer
    {
        // Necessary since some properties tend to collapse smaller than their content
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }
        // Draw a disabled property field
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            GUI.enabled = !Application.isPlaying && ((ReadOnlyAttribute)attribute).runtimeOnly;
            EditorGUI.PropertyField(position, property, label, true);
            GUI.enabled = true;
        }
    }
}
#endif
[AttributeUsage(AttributeTargets.Field)]
public class ReadOnlyAttribute : PropertyAttribute
{
    public readonly bool runtimeOnly;
    public ReadOnlyAttribute(bool runtimeOnly = false)
    {
        this.runtimeOnly = runtimeOnly;
    }
}

public class PlayerAbility : MonoBehaviour
{
    public enum EAbilityType { DiePassive, AttackPassive, KillPassive, Active }

    Player player;
    GameManager gameManager;

    [HideInInspector]
    public Enemy targetEnemy;

    [SerializeField, ReadOnly, Tooltip("ReadOnly! 절대 에디터에서 수정하지 말 것")]
    public List<int> abilitiesID = new List<int>();
    public List<IAbility> abilities = new List<IAbility>() { };

    public List<int> startAbilities = new List<int>();
#if UNITY_EDITOR
    public List<int> debugAbility = new List<int>() { 0, 0 };
#endif
    private void Start()
    {
        player = GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        foreach (var startAbility in startAbilities) AddAbility(startAbility);
    }
    private void Update()
    {
#if UNITY_EDITOR
        if (debugAbility.Count == 2) debugAbility[1] = debugAbility[0];
        else if (debugAbility.Count > 2)
        {
            AddAbility(debugAbility[0]);
            debugAbility.RemoveAt(0);
        }
        else if (debugAbility.Count < 2)
        {
            RemoveAbility(debugAbility[0]);
            debugAbility.Add(0);
        }
#endif
    }
    public void Reset()
    {
        foreach (var ability in abilities)
        {
            ability.Reset();
        }
    }
    public void PostAttackEvent(bool isDead, Enemy hitEnemy)
    {
        targetEnemy = hitEnemy;
        if (isDead)
        {
            foreach (var ability in abilities)
            {
                if (ability.abilityType == EAbilityType.KillPassive && ability.canEvent)
                {
                    ability.Event();
                }
            }
        }
        else
        {
            foreach (var ability in abilities)
            {
                if (ability.abilityType == EAbilityType.AttackPassive && ability.canEvent)
                {
                    ability.Event();
                }
            }
        }
        targetEnemy = null;
    }
    public bool DieEvent()
    {
        bool shouldDie = true;
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.DiePassive && ability.canEvent)
            {
                shouldDie &= ability.Event();
                Debug.Log(shouldDie);
            }
        }
        return shouldDie;
    }
    public void AddAbility(int id)
    {
        bool isSuccess = true;
        switch (id)
        {
            case 6:
                abilities.Add(new Shield(this));
                break;
            case 8:
                abilities.Add(new Reload(this));
                break;
            case 33:
                abilities.Add(new EvasiveManeuver(this));
                break;
            case 36:
                abilities.Add(new KnockBack(this));
                break;
            default:
                Debug.LogError("Invalid Ability Id");
                isSuccess = false;
                break;
        }
        if (isSuccess) abilitiesID.Add(id);
    }
    public void RemoveAbility(int id)
    {
        int index = abilitiesID.IndexOf(id);
        if (index == -1)
        {
            Debug.LogError("Cannot Found Ability");
            return;
        }
        abilities.RemoveAt(index);
        abilitiesID.RemoveAt(index);
    }
    class Shield : IAbility // 6.보호막
    {
        private EAbilityType mAbilityType = EAbilityType.DiePassive;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public Shield(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Vector3 newPosition = GameManager.playerPosition + new Vector3(0, -2, 0);
            if (GameManager.enemyValueList.Any(enemyValue => enemyValue.position == newPosition))
            {
                newPosition = GameManager.playerPosition + new Vector3(0, -1, 0);
                if (GameManager.enemyValueList.Any(enemyValue => enemyValue.position == newPosition)) newPosition = GameManager.playerPosition;
            }
            if (newPosition.y < -4) newPosition.y = -4;
            thisScript.transform.position = newPosition * GameManager.gridSize;
            GameManager.playerPosition = newPosition;

            canEvent = false;
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
                thisScript.RemoveAbility(6);
            }
        }
    }
    class Reload : IAbility // 8.재장전
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private bool mbEnable = true;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public Reload(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool isEnable { get { return mbEnable; } set { mbEnable = value; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.canAttack = true;

            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
    }
    class EvasiveManeuver : IAbility // 33.회피 기동
    {
        private EAbilityType mAbilityType = EAbilityType.AttackPassive;
        private bool mbEnable = true;
        private bool mbEvent = false;
        private int mCount = 1;

        private List<Vector2Int> originMovablePositions;
        private List<Vector2Int> newMovablePositions = new List<Vector2Int>() { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        PlayerAbility thisScript;
        public EvasiveManeuver(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            originMovablePositions = playerAbility.player.movablePositions;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool isEnable { get { return mbEnable; } set { mbEnable = value; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            if (thisScript.player.canAction) return false;

            thisScript.player.movablePositions = newMovablePositions.ToList();
            thisScript.player.shouldMove = true;

            return false;
        }
        public void Reset()
        {
            canEvent = true;
            thisScript.player.movablePositions = originMovablePositions.ToList();
            thisScript.player.shouldMove = false;
        }
    }
    class KnockBack : IAbility // 36.넉백
    {
        private EAbilityType mAbilityType = EAbilityType.AttackPassive;
        private bool mbEnable = true;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public KnockBack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool isEnable { get { return mbEnable; } set { mbEnable = value; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Vector3 newPosition = thisScript.targetEnemy.transform.position / GameManager.gridSize + new Vector3(0, 1, 0);
            if (Physics2D.Raycast(thisScript.targetEnemy.transform.position, Vector2.up, GameManager.gridSize, LayerMask.GetMask("Wall"))) return false;
            if (Physics2D.Raycast(thisScript.targetEnemy.transform.position, Vector2.up, GameManager.gridSize, LayerMask.GetMask("OuterWall"))) return false;
            if (Physics2D.Raycast(thisScript.targetEnemy.transform.position, Vector2.up, GameManager.gridSize, LayerMask.GetMask("Token"))) return false;

            GameManager.enemyValueList.Find(enemyValue => enemyValue.position == thisScript.targetEnemy.transform.position).position = newPosition;
            thisScript.targetEnemy.transform.position = newPosition * GameManager.gridSize;

            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
    }
}