using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;
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
    public enum EAbilityType { ValuePassive, DiePassive, MovePassive, AttackPassive, HitPassive, KillPassive, InstantActive, TargetActive }

    Player player;
    GameManager gameManager;

    [HideInInspector]
    public Enemy targetEnemy;

    [SerializeField, ReadOnly, Tooltip("ReadOnly! 절대 에디터에서 수정하지 말 것")]
    public List<int> abilitiesID = new List<int>();
    public List<IAbility> abilities = new List<IAbility>() { };

    public PlayerAbilityPrefabs abilityPrefabs;

    public List<int> startAbilities = new List<int>();
#if UNITY_EDITOR
    public List<int> debugAbility = new List<int>() { 0, 0 };
#endif

    public bool shouldSetUpAbilityUI = true; // [임시 능력 UI]

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
    public void ActiveEvent()
    {
        if (!shouldSetUpAbilityUI) return; // Do Once
        // 능력 UI 활성화
        Transform ContentBox = player.abilityUI.transform.GetChild(0).GetChild(0).GetChild(0);

        ContentBox.GetComponent<RectTransform>().sizeDelta = new Vector2(player.abilityCount * 190f, 0); // 패널 크기 설정

        int index = 0;
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.InstantActive || ability.abilityType == EAbilityType.TargetActive)
            {
                Button abilityButton = ContentBox.GetChild(index).GetComponent<Button>();
                abilityButton.gameObject.SetActive(true);
                abilityButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(index * 190f + 95f, 0);
                int abilityIndex = abilities.IndexOf(ability);
                abilityButton.transform.GetChild(0).GetComponent<Text>().text = abilitiesID[abilityIndex].ToString();
                abilityButton.interactable = ability.canEvent;
                abilityButton.onClick.RemoveAllListeners();
                if (ability.abilityType == EAbilityType.InstantActive) abilityButton.onClick.AddListener(() => ability.Event());
                else abilityButton.onClick.AddListener(() => TargetEvent(abilitiesID[abilityIndex]));
                abilityButton.GetComponent<DisposableButton>().activeCondition = (ability as IActiveAbility).activeCondition;
                abilityButton.GetComponent<DisposableButton>().isAlreadyUsed = !ability.canEvent;
                index++;
            }
        }
        shouldSetUpAbilityUI = false;
    }
    public void TargetEvent(int abilityID)
    {
        player.usingAbilityID = abilityID;
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Ability;
    }
    public void Reset()
    {
        foreach (var ability in abilities)
        {
            ability.Reset();
        }
        for (int i = 0; i < player.abilityCount; i++) // 버튼마다 버튼 이름과 쿨타임에 따른 활성화여부 설정
        {
            Button abilityButton = player.abilityUI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(i).GetComponent<Button>();
            abilityButton.gameObject.SetActive(false);
        }
        shouldSetUpAbilityUI = true;
    }
    public void MoveEvent()
    {
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.MovePassive && ability.canEvent)
            {
                ability.Event();
            }
        }
    }
    public void PostAttackEvent(bool isDead, Enemy hitEnemy)
    {
        targetEnemy = hitEnemy;
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.AttackPassive && ability.canEvent)
            {
                ability.Event();
            }
        }
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
                if (ability.abilityType == EAbilityType.HitPassive && ability.canEvent)
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
            case 1:
                abilities.Add(new AtkUp1(this));
                break;
            case 2:
                abilities.Add(new AtkUp2(this));
                break;
            case 3:
                abilities.Add(new WallUp1(this));
                break;
            case 4:
                abilities.Add(new WallUp2(this));
                break;
            case 5:
                abilities.Add(new WallUp3(this));
                break;
            case 6:
                abilities.Add(new Shield(this));
                break;
            case 7:
                abilities.Add(new ToughSurvival(this));
                break;
            case 8:
                abilities.Add(new Reload(this));
                break;
            case 12:
                abilities.Add(new PrecisionAttack(this));
                break;
            case 13:
                abilities.Add(new SmokeGrenade(this));
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
        player.abilityCount = abilities.Count(ability => ability.abilityType == EAbilityType.InstantActive || ability.abilityType == EAbilityType.TargetActive);
    }
    public void RemoveAbility(int id)
    {
        int index = abilitiesID.IndexOf(id);
        if (index == -1)
        {
            Debug.LogError("Cannot Found Ability");
            return;
        }
        if (abilities[index].abilityType == EAbilityType.ValuePassive) abilities[index].Event();
        abilities.RemoveAt(index);
        abilitiesID.RemoveAt(index);
        player.abilityCount = abilities.Count(ability => ability.abilityType == EAbilityType.InstantActive || ability.abilityType == EAbilityType.TargetActive);
    }
    class AtkUp1 : IAbility // 1.공격력 증가 +1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private bool mbEvent = false;
        private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public AtkUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.atk += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.atk -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class AtkUp2 : IAbility // 2.공격력 증가 +2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private bool mbEvent = false;
        private int mCount = 1;
        private int mValue = 2;

        PlayerAbility thisScript;
        public AtkUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.atk += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.atk -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class WallUp1 : IAbility // 3.벽 소지 +1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private bool mbEvent = false;
        private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public WallUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.maxWallCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class WallUp2 : IAbility // 4.벽 소지 +2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private bool mbEvent = false;
        private int mCount = 1;
        private int mValue = 2;

        PlayerAbility thisScript;
        public WallUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.maxWallCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class WallUp3 : IAbility // 5.벽 소지 +3
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private bool mbEvent = false;
        private int mCount = 1;
        private int mValue = 3;

        PlayerAbility thisScript;
        public WallUp3(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.maxWallCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
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
    class ToughSurvival : IAbility // 7.질긴 생존
    {
        private EAbilityType mAbilityType = EAbilityType.DiePassive;
        private bool mbEvent = false;
        private int mCount = 3;
        private bool mbDidEvent = false;

        PlayerAbility thisScript;
        public ToughSurvival(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            mbDidEvent = true;
            canEvent = false;

            return false;
        }
        public void Reset()
        {
            if (mbDidEvent)
            {
                if (GameManager.Turn == 1)
                {
                    thisScript.RemoveAbility(7);
                    return;
                }
                if (mCount > 0)
                {
                    mCount--;
                }
                else
                {
                    thisScript.player.Die();
                }
            }
            else
            {
                canEvent = true;
            }
        }
    }
    class Reload : IAbility // 8.재장전
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public Reload(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
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
    class PrecisionAttack : IAbility, IActiveAbility // 12.정밀 공격
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private bool mbEvent = true;
        private int mCount = 2;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.Attack;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public PrecisionAttack(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.atk += mValue;

            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            thisScript.player.atk -= mValue;
            if (GameManager.Turn == 1) mCount = 2;
            if (mCount > 0) canEvent = true;
            Debug.Log(canEvent);
        }
    }
    class SmokeGrenade : IAbility, IActiveAbility // 13.연막탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.Attack;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private Vector2Int mTargetPos;
        private List<GameObject> mTargetList = new List<GameObject>();

        PlayerAbility thisScript;
        public SmokeGrenade(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            canEvent = false;

            return false;
        }
        public void Reset()
        {
            canEvent = true;
            foreach (var pos in mAttackScale)
            {
                mTargetList.Add(GameObject.FindGameObjectsWithTag("Enemy").Where(enemyObject => enemyObject.transform.position == GameManager.ChangeCoord(targetPos + pos)).FirstOrDefault());
            }
        }
    }
    class EvasiveManeuver : IAbility // 33.회피 기동
    {
        private EAbilityType mAbilityType = EAbilityType.AttackPassive;
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
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public KnockBack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
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