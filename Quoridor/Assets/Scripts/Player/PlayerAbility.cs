using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif
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
public class AdditionalAbilityStat
{
    public int throwingDamage = 0;
    public List<Vector2Int> throwingRange = new List<Vector2Int>();
    public int throwingCount = 0;
}
public class PlayerAbility : MonoBehaviour
{
    public enum EAbilityType { ValuePassive, DiePassive, MovePassive, AttackPassive, HitPassive, KillPassive, InstantActive, TargetActive }
    public enum EResetTime { OnEnemyTurnStart, OnPlayerTurnStart, OnEveryTick }
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

    public AdditionalAbilityStat additionalAbilityStat = new AdditionalAbilityStat();

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
                abilityButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(index * 120f + 70f, 0);
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
    public void ResetEvent(EResetTime resetTime)
    {
        List<IAbility> abilitiesCopy = new List<IAbility>(abilities);
        foreach (var ability in abilitiesCopy)
        {
            if (ability.resetTime == resetTime)
            {
                ability.Reset();
            }
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
        if (abilitiesID.Contains(id))
        {
            Debug.LogError("이미 보유중인 능력");
            return;
        }
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
            case 9:
                abilities.Add(new ChainLightning(this));
                break;
            case 10:
                abilities.Add(new ChaineExplosion(this));
                break;
            case 11:
                abilities.Add(new PenetrateAttack(this));
                break;
            case 12:
                abilities.Add(new PrecisionAttack(this));
                break;
            case 13:
                abilities.Add(new SmokeGrenade(this));
                break;
            case 14:
                abilities.Add(new PoisonBomb(this));
                break;
            case 15:
                abilities.Add(new Grenade(this));
                break;
            case 16:
                abilities.Add(new ThrowingDamageUp1(this));
                break;
            case 17:
                abilities.Add(new ThrowingDamageUp1(this));
                break;
            case 18:
                abilities.Add(new ThrowingRangeUp1(this));
                break;
            case 19:
                abilities.Add(new ThrowingRangeUp2(this));
                break;
            case 20:
                abilities.Add(new ThrowingCountUp1(this));
                break;
            case 21:
                abilities.Add(new ThrowingCountUp2(this));
                break;
            case 22:
                abilities.Add(new AutoTrapSetting(this));
                break;
            case 32:
                abilities.Add(new PrecisionBomb(this));
                break;
            case 33:
                abilities.Add(new EvasiveManeuver(this));
                break;
            case 34:
                abilities.Add(new ConstructionManeuver(this));
                break;
            case 36:
                abilities.Add(new KnockBack(this));
                break;
            case 38:
                abilities.Add(new AnkleAttack(this));
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
    public GameObject FindValuesObj(Vector3 position)
    {
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        foreach (Transform child in enemyBox.transform)
        {
            // Debug.Log(child.position);
            if (child.position == position)
            {
                return child.gameObject;
            }
        }
        //Debug.LogError("enemyManager error : 어떤 Enemy 스크립트를 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }

    public enemyValues FindValues(Vector3 position)
    {
        foreach (enemyValues child in GameManager.enemyValueList)
        {
            if (child.position == position)
            {
                return child;
            }
        }
        //Debug.LogError("enemyManager error : 어떤 EnemyValues도 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }
    class AtkUp1 : IAbility // 1.공격력 증가 +1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public AtkUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.atk += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 2;

        PlayerAbility thisScript;
        public AtkUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.atk += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public WallUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 2;

        PlayerAbility thisScript;
        public WallUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 3;

        PlayerAbility thisScript;
        public WallUp3(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.player.maxWallCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public Shield(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 3;
        private bool mbDidEvent = false;

        PlayerAbility thisScript;
        public ToughSurvival(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;

        PlayerAbility thisScript;
        public Reload(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
    class ChainLightning : IAbility // 9.체인라이트닝
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public ChainLightning(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            float dist = 10000000;
            GameObject enemyObj = null;
            enemyValues enemyValue = null;
            foreach (enemyValues enemysPos in GameManager.enemyValueList) // 죽은 적 기준으로 가장 가까운 적 확인
            {
                if (dist > Vector2.Distance(thisScript.targetEnemy.transform.position, enemysPos.position))
                {
                    dist = Vector2.Distance(thisScript.targetEnemy.transform.position, enemysPos.position);
                    enemyObj = thisScript.FindValuesObj(enemysPos.position);
                    enemyValue = enemysPos;
                }
            }
            // hp 깍아내는 코드 나중에 최적화 필요할듯
            enemyObj.transform.GetComponent<Enemy>().AttackedEnemy(1);

            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
    }
    class ChaineExplosion : IAbility // 10.연쇄폭발
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        private List<Vector2> exploablePosition = new List<Vector2>();
        bool[] result;
        public ChaineExplosion(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            // 초기 폭발 범위 확인 후 리스트 추가
            exploablePosition.Add(new Vector2(0, 1));
            exploablePosition.Add(new Vector2(1, 1));
            exploablePosition.Add(new Vector2(1, 0));
            exploablePosition.Add(new Vector2(1, -1));
            exploablePosition.Add(new Vector2(0, -1));
            exploablePosition.Add(new Vector2(-1, -1));
            exploablePosition.Add(new Vector2(-1, 0));
            exploablePosition.Add(new Vector2(-1, 1));

            for (int i = 0; i < exploablePosition.Count; i++)
            {
                RaycastHit2D outerWallHit = Physics2D.Raycast(thisScript.targetEnemy.transform.position, ((Vector2)exploablePosition[i]).normalized, GameManager.gridSize * exploablePosition[i].magnitude, LayerMask.GetMask("OuterWall")); // 외벽에 의해 완전히 막힘
                RaycastHit2D wallHit = Physics2D.Raycast(thisScript.targetEnemy.transform.position, ((Vector2)exploablePosition[i]).normalized, GameManager.gridSize * exploablePosition[i].magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
                RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(thisScript.targetEnemy.transform.position, ((Vector2)exploablePosition[i]).normalized, GameManager.gridSize * exploablePosition[i].magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘

                bool fullBlock = false;
                // Debug.Log($"{(bool)tokenHit} - {(tokenHit ? tokenHit.collider.gameObject.name : i)}");
                while (true) // 타겟과 폭발범위 사이에 벽이 존재할경우 폭발 범위에서 제외 확인
                {
                    if (outerWallHit)
                    {
                        result = new bool[] { true, false };
                        break;
                    }
                    if (!wallHit)
                    { // 벽에 의해 완전히 막히지 않았고
                        for (int j = 0; j < semiWallHit.Length; j++)
                        { // 반벽이 2개가 겹쳐있을 경우에
                            for (int k = j + 1; k < semiWallHit.Length; k++)
                            {
                                float wallDistance = Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance);
                                if (wallDistance > 0.1f) continue;
                                if (semiWallHit[j].transform.rotation == semiWallHit[k].transform.rotation || Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance) < 0.000001f)
                                {
                                    fullBlock = true; // 완전 막힘으로 처리
                                    break;
                                }
                            }
                            if (fullBlock) break;
                        }
                        if (!fullBlock)
                        { // 완전 막히지 않았고 적이 공격 범주에 있다면 공격한다.
                            result = new bool[] { false, true };
                            break;
                        }
                    }
                    result = new bool[] { false, false };
                    break;
                }
                if (result[0]) // 외부벽에 막혀있는 판정일 경우 리스트에서 제외
                {
                    exploablePosition.RemoveAt(i);
                }
                else if (!result[1]) // 사이에 벽이 있다고 판단될 경우 리스트에서 제외
                {
                    exploablePosition.RemoveAt(i);
                }
                // 최종적으로 벽으로 걸러지지 않은 구역만 리스트안에 들어가게 됨
            } // 폭발 가능한 구역 확인 후 리스트 재구성 ex) 벽으로 막혀있는 구역경우 폭발 범위에서 제외

            for (int i = 0; i < exploablePosition.Count; i++)
            {
                Vector3 explosionPos = thisScript.targetEnemy.transform.position + ((Vector3)exploablePosition[i] * GameManager.gridSize);
                GameObject enemyObj = thisScript.FindValuesObj(explosionPos);
                if (enemyObj) // 만약 폭발 지점에 적이 존재했을 경우
                {
                    foreach (enemyValues child in GameManager.enemyValueList) // 리스트에서 찾아서 hp 다운
                    {
                        if (child.position == enemyObj.transform.position)
                        {
                            enemyObj.transform.GetComponent<Enemy>().AttackedEnemy(1);
                        }
                    }
                }
            }

            return false;
        }
        public void Reset()
        {
            canEvent = true;
            exploablePosition.Clear();
        }
    }
    class PenetrateAttack : IAbility // 11.관통 공격
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public PenetrateAttack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Vector2 enemyTrimPos = (thisScript.targetEnemy.transform.position + new Vector3(4, 4, 0)) / GameManager.gridSize;
            int enemyMapNumber = (int)(enemyTrimPos.x + (enemyTrimPos.y * 9));

            // 예외 처리
            if (enemyMapNumber > 72) // 현재 처치된 적이 위쪽 외벽에 붙어있을 경우
            {
                return false;
            }
            if (thisScript.gameManager.mapGraph[enemyMapNumber, enemyMapNumber + 9] == 0) // 적이 처치됐지만 적 뒤에 벽이 있는 경우
            {
                return false;
            }

            // 실행 코드
            GameObject enemyBackTarget = thisScript.FindValuesObj(((Vector3)(enemyTrimPos) + new Vector3(-4, -4, 0)) * GameManager.gridSize);
            if (enemyBackTarget) // 처치된 적 뒤에 아무런 유닛도 없을 경우
            {
                return false;
            }

            // 뒤에 적이 있는 경우 체력 -1
            enemyBackTarget.GetComponent<Enemy>().AttackedEnemy(1); // 뒤에 있는 적 데미지 감소

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
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 2;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.Attack;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
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
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
        private EResetTime mResetTime = EResetTime.OnEveryTick;
        private bool mbEvent = true;
        private int mCount = 2;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false };
        private Vector2Int mTargetPos;
        private List<AreaAbility> mAreaAbilityList = new List<AreaAbility>();
        private int tempTurn;
        private List<Vector2Int> originAttackRange = new List<Vector2Int>();

        PlayerAbility thisScript;
        public SmokeGrenade(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
            originAttackRange = mAttackRange.ToList();
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mAreaAbilityList.Add(new AreaAbility(2, targetPos));
            canEvent = false;
            mCount--;
            tempTurn = GameManager.Turn;
            return false;
        }
        public void Reset()
        {
            if (canEvent)
            {
                mAttackRange = originAttackRange.Concat(thisScript.additionalAbilityStat.throwingRange).ToList();
                return;
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (GameManager.Turn % 2 == Player.playerOrder && tempTurn != GameManager.Turn)
            {
                List<AreaAbility> tempAreaAbility = mAreaAbilityList.ToList();
                foreach (var areaAbility in mAreaAbilityList)
                {
                    areaAbility.life--;
                    if (areaAbility.life <= 0) tempAreaAbility.Remove(areaAbility);
                }
                mAreaAbilityList = tempAreaAbility.ToList();
                tempTurn = GameManager.Turn;
                if (mCount > 0)
                {
                    canEvent = true;
                }
            }

            foreach (var areaAbility in mAreaAbilityList)
            {
                foreach (var attackPosition in attackScale)
                {
                    bool[] result = thisScript.player.CheckRay(GameManager.ChangeCoord(areaAbility.targetPos), GameManager.ChangeCoord(attackPosition));
                    if (result[0]) continue;
                    if (result[1] || canPenetrate[1])
                    {
                        GameObject targetEnemyObject = thisScript.FindValuesObj(GameManager.ChangeCoord(areaAbility.targetPos + attackPosition));
                        if (targetEnemyObject == null) continue;
                        Enemy targetEnemy = targetEnemyObject.GetComponent<Enemy>();
                        Debug.Log($"{targetEnemyObject.name} 에게 이벤트 발생중!");
                    }
                }
            }
        }
    }
    class PoisonBomb : IAbility, IActiveAbility // 14.독성 폭탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEveryTick;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false };
        private Vector2Int mTargetPos;
        private List<AreaAbility> mAreaAbilityList = new List<AreaAbility>();
        private int tempTurn;
        private List<Vector2Int> originAttackRange = new List<Vector2Int>();

        PlayerAbility thisScript;
        public PoisonBomb(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
            originAttackRange = mAttackRange.ToList();
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mValue = 1 + thisScript.additionalAbilityStat.throwingDamage;
            mAreaAbilityList.Add(new AreaAbility(2, targetPos));
            canEvent = false;
            mCount--;
            tempTurn = GameManager.Turn;
            return false;
        }
        public void Reset()
        {
            if (canEvent)
            {
                mAttackRange = originAttackRange.Concat(thisScript.additionalAbilityStat.throwingRange).ToList();
                return;
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (GameManager.Turn % 2 == Player.playerOrder && tempTurn != GameManager.Turn)
            {
                List<AreaAbility> tempAreaAbility = mAreaAbilityList.ToList();
                foreach (var areaAbility in mAreaAbilityList)
                {
                    areaAbility.life--;
                    if (areaAbility.life <= 0) tempAreaAbility.Remove(areaAbility);
                }
                mAreaAbilityList = tempAreaAbility.ToList();
                tempTurn = GameManager.Turn;
                if (mCount > 0)
                {
                    canEvent = true;
                }
            }

            foreach (var areaAbility in mAreaAbilityList)
            {
                List<GameObject> tempTargetList = areaAbility.targetList.ToList();
                areaAbility.targetList.Clear();
                foreach (var attackPosition in attackScale)
                {
                    bool[] result = thisScript.player.CheckRay(GameManager.ChangeCoord(areaAbility.targetPos), GameManager.ChangeCoord(attackPosition));
                    if (result[0]) continue;
                    if (result[1] || canPenetrate[1])
                    {
                        GameObject targetEnemyObject = thisScript.FindValuesObj(GameManager.ChangeCoord(areaAbility.targetPos + attackPosition));
                        if (targetEnemyObject == null) continue;
                        Enemy targetEnemy = targetEnemyObject.GetComponent<Enemy>();
                        if (tempTargetList.Contains(targetEnemyObject))  // On Stay
                        {
                            targetEnemy.AttackedEnemy(mValue);
                        }
                        else  // On Enter
                        {
                            targetEnemy.moveCtrl[1] = Mathf.Max(targetEnemy.moveCtrl[1] - 2, 0);
                        }
                        areaAbility.targetList.Add(targetEnemyObject);
                    }
                }
                // foreach (GameObject targetEnemyObject in tempTargetList.Except(areaAbility.targetList)) // On Exit
                // { 
                //     if (targetEnemyObject == null) // On Die
                //     {
                //         continue;
                //     }
                // }
            }
        }
    }
    class Grenade : IAbility, IActiveAbility // 15.수류탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEveryTick;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 2;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false };
        private Vector2Int mTargetPos;
        private int tempTurn;
        private List<Vector2Int> originAttackRange = new List<Vector2Int>();

        PlayerAbility thisScript;
        public Grenade(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
            originAttackRange = mAttackRange.ToList();
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mValue = 2 + thisScript.additionalAbilityStat.throwingDamage;
            foreach (var attackPosition in attackScale)
            {
                bool[] result = thisScript.player.CheckRay(GameManager.ChangeCoord(targetPos), GameManager.ChangeCoord(attackPosition));
                if (result[0]) continue;
                if (result[1] || canPenetrate[1])
                {
                    GameObject targetEnemyObject = thisScript.FindValuesObj(GameManager.ChangeCoord(targetPos + attackPosition));
                    if (targetEnemyObject == null) continue;
                    Enemy targetEnemy = targetEnemyObject.GetComponent<Enemy>();
                    targetEnemy.AttackedEnemy(mValue);
                }
            }
            mCount--;
            canEvent = false;
            tempTurn = GameManager.Turn;
            return false;
        }
        public void Reset()
        {
            if (canEvent)
            {
                mAttackRange = originAttackRange.Concat(thisScript.additionalAbilityStat.throwingRange).ToList();
                return;
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (GameManager.Turn % 2 == Player.playerOrder && tempTurn != GameManager.Turn)
            {
                tempTurn = GameManager.Turn;
                if (mCount > 0)
                {
                    canEvent = true;
                }
            }
            // if (GameManager.Turn % 2 == Player.playerOrder && tempTurn != GameManager.Turn)
            // {
            //     mCount--;
            //     tempTurn = GameManager.Turn;
            // }
            // if (mCount <= 0)
            // {
            //     return;
            // }

            // List<GameObject> tempTargetList = mTargetList.ToList();
            // mTargetList.Clear();
            // foreach (var attackPosition in attackScale)
            // {
            //     RaycastHit2D outerWallHit = Physics2D.Raycast(GameManager.ChangeCoord(targetPos), GameManager.ChangeCoord(attackPosition).normalized, GameManager.gridSize * GameManager.ChangeCoord(attackPosition).magnitude, LayerMask.GetMask("OuterWall")); // 외벽에 의해 완전히 막힘
            //     RaycastHit2D wallHit = Physics2D.Raycast(GameManager.ChangeCoord(targetPos), GameManager.ChangeCoord(attackPosition).normalized, GameManager.gridSize * GameManager.ChangeCoord(attackPosition).magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
            //     RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(GameManager.ChangeCoord(targetPos), GameManager.ChangeCoord(attackPosition).normalized, GameManager.gridSize * GameManager.ChangeCoord(attackPosition).magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘
            //     bool[] result = thisScript.player.CheckRay(outerWallHit, wallHit, semiWallHit);
            //     if (result[0]) continue;
            //     if (result[1] || canPenetrate[1])
            //     {
            //         GameObject targetEnemyObject = thisScript.FindValuesObj(GameManager.ChangeCoord(targetPos + attackPosition));
            //         if (targetEnemyObject == null) continue;
            //         Enemy targetEnemy = targetEnemyObject.GetComponent<Enemy>();
            //         if (tempTargetList.Contains(targetEnemyObject))  // On Stay
            //         {
            //             targetEnemy.AttackedEnemy(mValue);
            //         }
            //         else  // On Enter
            //         {
            //             targetEnemy.moveCtrl[1] = Mathf.Max(targetEnemy.moveCtrl[1] - 2, 0);
            //         }
            //         mTargetList.Add(targetEnemyObject);
            //     }
            // }
            // foreach (GameObject targetEnemyObject in tempTargetList.Except(mTargetList)) // On Exit
            // { 
            //     if (targetEnemyObject == null) // On Die
            //     {
            //         continue;
            //     }
            // }
        }
    }
    class ThrowingDamageUp1 : IAbility // 16.투척 데미지 증가1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public ThrowingDamageUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingDamage += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingDamage -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class ThrowingDamageUp2 : IAbility // 17.투척 데미지 증가2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public ThrowingDamageUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingDamage += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingDamage -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class ThrowingRangeUp1 : IAbility // 18.투척 사거리 확장1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        // private int mValue = 1;
        private List<Vector2Int> mRange = new List<Vector2Int>() {
            new Vector2Int(2, 1), new Vector2Int(2, -1), new Vector2Int(-2, 1), new Vector2Int(-2, -1), new Vector2Int(1, 2), new Vector2Int(1, -2), new Vector2Int(-1, 2), new Vector2Int(-1, -2) };

        PlayerAbility thisScript;
        public ThrowingRangeUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingRange = thisScript.additionalAbilityStat.throwingRange.Concat(mRange).ToList();
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingRange = thisScript.additionalAbilityStat.throwingRange.Except(mRange).ToList();

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class ThrowingRangeUp2 : IAbility // 19.투척 사거리 확장2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        // private int mValue = 1;
        private List<Vector2Int> mRange = new List<Vector2Int>() {
            new Vector2Int(2, 2), new Vector2Int(2, -2), new Vector2Int(-2, 2), new Vector2Int(-2, -2) };

        PlayerAbility thisScript;
        public ThrowingRangeUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingRange = thisScript.additionalAbilityStat.throwingRange.Concat(mRange).ToList();
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingRange = thisScript.additionalAbilityStat.throwingRange.Except(mRange).ToList();

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class ThrowingCountUp1 : IAbility // 20.투척 개수 증가1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public ThrowingCountUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class ThrowingCountUp2 : IAbility // 21.투척 개수 증가2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public ThrowingCountUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.throwingCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.throwingCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
    }
    class AutoTrapSetting : IAbility // 22.자동 덫 설치
    {
        private EAbilityType mAbilityType = EAbilityType.MovePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        private int moveCount = 1; // 1인 이유는 이 MovePassive자체가 1번 움직이고 실행되는 코드이기 때문에 미리 1을 더해둠
        private Transform playerBeforePos;

        PlayerAbility thisScript;
        public AutoTrapSetting(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            // 5번 움직였을 경우
            if (moveCount >= 5)
            {
                Instantiate(thisScript.gameManager.autoTrap, playerBeforePos.position, Quaternion.identity); // 덫 설치용
                moveCount = 1; // player 움직인 누적 횟수 초기화
            }
            else
            {
                playerBeforePos = thisScript.player.transform; // 플레이어 움직이고 난 후 transform 저장
                moveCount += 1; // 
            }
            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
    }
    class PrecisionBomb : IAbility, IActiveAbility // 32.정밀 폭격
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        // private int mCount = 1;
        private int mValue = 2;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1)
        };
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;
        private List<GameObject> mTargetList = new List<GameObject>();

        PlayerAbility thisScript;
        public PrecisionBomb(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
        }
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            canEvent = false;

            RaycastHit2D previewHit = Physics2D.Raycast(GameManager.ChangeCoord(targetPos), Vector3.forward, 15f, LayerMask.GetMask("Preview"));
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerAttackPreview")) // 클릭좌표에 플레이어공격미리보기가 있다면
                {
                    foreach (var attackPosition in attackScale)
                    {
                        RaycastHit2D enemyHit = Physics2D.Raycast(GameManager.ChangeCoord(targetPos) + GameManager.ChangeCoord(attackPosition), Vector3.forward, 15f, LayerMask.GetMask("Token"));
                        if (enemyHit)
                        {
                            if (enemyHit.transform.CompareTag("Enemy"))
                            {
                                Enemy enemy = enemyHit.transform.GetComponent<Enemy>();

                                enemy.AttackedEnemy(mValue);
                                Debug.Log($"{enemyHit.transform.name}의 현재 체력 {enemy.hp}");
                            }
                        }
                    }
                }
            }

            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) canEvent = true;
        }
    }
    class EvasiveManeuver : IAbility // 33.회피 기동
    {
        private EAbilityType mAbilityType = EAbilityType.AttackPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;

        private List<Vector2Int> originMovablePositions;
        private List<Vector2Int> newMovablePositions = new List<Vector2Int>() { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        PlayerAbility thisScript;
        public EvasiveManeuver(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            originMovablePositions = playerAbility.player.movablePositions;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
    class ConstructionManeuver : IAbility // 34.건설 기동
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;


        PlayerAbility thisScript;
        public ConstructionManeuver(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.player.shouldBuild = false;
            thisScript.player.shouldMove = false;
            return false;
        }
        public void Reset()
        {
            thisScript.player.shouldBuild = true;
            thisScript.player.shouldMove = true;
        }
    }
    class KnockBack : IAbility // 36.넉백
    {
        private EAbilityType mAbilityType = EAbilityType.HitPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;

        PlayerAbility thisScript;
        public KnockBack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
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
    class Pressure : IAbility // 37.위압감
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;


        PlayerAbility thisScript;
        public Pressure(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1)
            {
                foreach (GameObject enemyObject in GameObject.FindGameObjectsWithTag("Enemy"))
                {
                    Enemy enemy = enemyObject.GetComponent<Enemy>();
                    enemy.moveCtrl[1] = Mathf.Max(enemy.moveCtrl[1] - 3, 0);
                }
            }
        }
    }
    class AnkleAttack : IAbility // 38.발목 공격
    {
        private EAbilityType mAbilityType = EAbilityType.HitPassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public AnkleAttack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            // 피격받은 적 오브젝트 행동력 3 감소
            thisScript.FindValues(thisScript.targetEnemy.transform.position).moveCtrl -= 3;
            thisScript.targetEnemy.GetComponent<Enemy>().moveCtrl[1] -= 3;
            return false;
        }

        public void Reset()
        {
            canEvent = true;
        }
    }
}