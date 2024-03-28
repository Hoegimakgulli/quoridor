using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
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

    public int placeDamage = 0;
    public int placeCount = 0;
}
public class PlayerAbility : MonoBehaviour
{
    public enum EAbilityType { ValuePassive, DiePassive, MovePassive, AttackPassive, HitPassive, KillPassive, InstantActive, TargetActive }
    public enum EResetTime { OnEnemyTurnStart, OnPlayerTurnStart, OnEveryTick }
    Player player;
    GameManager gameManager;
    EnemyManager enemyManager;

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


    private bool needSave = true;
    public bool NeedSave { get { return needSave; } }
    private void Start()
    {
        player = GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        enemyManager = gameManager.gameObject.GetComponent<EnemyManager>();

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
        Transform ContentBox = player.abilityUI.transform.GetChild(0).GetChild(0).GetChild(0); //실제 버튼들이 담겨있는 박스

        ContentBox.GetComponent<RectTransform>().sizeDelta = new Vector2(player.abilityCount * 190f, 0); // 패널 크기 설정

        int index = 0;
        foreach (var ability in abilities)
        {
            if (ability.abilityType == EAbilityType.InstantActive || ability.abilityType == EAbilityType.TargetActive) //액티브 능력이 있다면
            {
                Button abilityButton = ContentBox.GetChild(index).GetComponent<Button>();
                abilityButton.gameObject.SetActive(true);//버튼의 갯수만큼 활성화
                abilityButton.GetComponent<RectTransform>().anchoredPosition = new Vector2(index * 120f + 70f, 0); //버튼 위치 설정
                int abilityIndex = abilities.IndexOf(ability);
                abilityButton.transform.GetChild(0).GetComponent<Text>().text = abilitiesID[abilityIndex].ToString(); //버튼의 텍스트를 버튼의 고유 아이디로
                abilityButton.GetComponent<DisposableButton>().activeAbility = ability as IActiveAbility;
                index++;
            }
        }
        shouldSetUpAbilityUI = false;
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
        if (resetTime == EResetTime.OnEnemyTurnStart)
        {
            for (int i = 0; i < player.abilityCount; i++) // 버튼마다 버튼 이름과 쿨타임에 따른 활성화여부 설정
            {
                Button abilityButton = player.abilityUI.transform.GetChild(0).GetChild(0).GetChild(0).GetChild(i).GetComponent<Button>();
                abilityButton.gameObject.SetActive(false);
            }
            shouldSetUpAbilityUI = true;
        }
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
        GameObject.Find("임시 캔버스").transform.GetChild(0).GetChild(0).GetComponent<Text>().text = GameObject.Find("임시 캔버스").transform.GetChild(0).GetChild(0).GetComponent<Text>().text + "  " + id;
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
            case 23:
                abilities.Add(new KnifeMine(this));
                break;
            case 24:
                abilities.Add(new SlipperyJelly(this));
                break;
            case 25:
                abilities.Add(new PlaceDamageUp1(this));
                break;
            case 26:
                abilities.Add(new PlaceDamageUp2(this));
                break;
            case 27:
                abilities.Add(new PlaceCountUp1(this));
                break;
            case 28:
                abilities.Add(new PlaceCountUp2(this));
                break;
            case 29:
                abilities.Add(new PlaceDummy(this));
                break;
            case 30:
                abilities.Add(new ArtilleryFire(this));
                break;
            case 31:
                abilities.Add(new RequestSniping(this));
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
            case 35:
                abilities.Add(new FastManeuver(this));
                break;
            case 36:
                abilities.Add(new KnockBack(this));
                break;
            case 37:
                abilities.Add(new Pressure(this));
                break;
            case 38:
                abilities.Add(new AnkleAttack(this));
                break;
            case 39:
                abilities.Add(new Imprison(this));
                break;
            case 40:
                abilities.Add(new MindControl(this));
                break;
            case 41:
                abilities.Add(new ReRoll(this));
                break;
            case 42:
                abilities.Add(new PoisonousCocktail(this));
                break;
            case 43:
                abilities.Add(new Conciliation(this));
                break;
            case 44:
                abilities.Add(new SleepingSand(this));
                break;
            default:
                Debug.LogError("Invalid Ability Id");
                isSuccess = false;
                break;
        }
        if (isSuccess) abilitiesID.Add(id);
        player.abilityCount = abilities.Count(ability => ability.abilityType == EAbilityType.InstantActive || ability.abilityType == EAbilityType.TargetActive);
        needSave = true;
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
        needSave = true;
    }
    public AreaAbility SetAreaAbility(AreaAbility.ELifeType lifeType, int life, Vector2Int targetPos, List<Vector2Int> areaPositionList, bool canPenetrate, EnterEvent enterEvent, StayEvent stayEvent, ExitEvent exitEvent)
    {
        GameObject areaAbilityObject = Instantiate(abilityPrefabs.AreaAbilityPrefab, GameManager.ChangeCoord(targetPos), Quaternion.identity);
        AreaAbility areaAbility = areaAbilityObject.GetComponent<AreaAbility>();
        areaAbility.lifeType = lifeType;
        areaAbility.life = life;
        areaAbility.areaPositionList = areaPositionList;
        areaAbility.canPenetrate = canPenetrate;
        areaAbility.enterEvent = enterEvent;
        areaAbility.stayEvent = stayEvent;
        areaAbility.exitEvent = exitEvent;
        areaAbility.SetUp();
        return areaAbility;
    }
    public void SaveAbility()
    {
        string filePath = Application.persistentDataPath + "/ability.json";
        List<object> jsonList = new List<object>();
        for (int i = 0; i < abilities.Count; i++)
        {
            int index = abilitiesID[i];
            string result = abilities[i].Save();
            if (result == string.Empty)
                continue;
            Debug.Log($"[{index}] {result}");
            jsonList.Add(result);
        }
        if (jsonList.Count < 1)
            return;

        var json = JsonConvert.SerializeObject(jsonList);
        Debug.Log($"Final: {json}, in {filePath}");
        File.WriteAllText(filePath, json);
        needSave = false;
    }
    public bool LoadAbility()
    {
        string filePath = Application.persistentDataPath + "/ability.json";
        if (!File.Exists(filePath)) return false;

        string json = File.ReadAllText(filePath);
        List<object> jsonList = JsonConvert.DeserializeObject<List<object>>(json);
        for (int i = 0; i < jsonList.Count; i++)
        {
            var abilityData = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonList[i].ToString());

            if (abilityData.ContainsKey("AbilityID"))
            {
                int index = int.Parse(abilityData["AbilityID"].ToString());
                Debug.Log($"[{index}]: {jsonList[i]}");
                //해당 index에 대응하는 Load(jsonList[i]) 호출
            }
            else
            {
                Debug.LogError($"ContainsKey(AbilityID) false: {jsonList[i]}");
            }
        }

        return true;
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
        public string Save()
        {
            string result = string.Empty;
            Dictionary<string, object> data = new Dictionary<string, object>();

            data.Add("AbilityID", 1);
            data.Add("canEvent", canEvent);
            data.Add("mValue", mValue);

            result = JsonConvert.SerializeObject(data);
            return result;
        }
        public void Load(string data)
        {
            var root = JsonConvert.DeserializeObject<Dictionary<string, object>>(data);

            bool canEvent = (bool)root["canEvent"];
            int mValue = (int)root["mValue"];

            Debug.Log($"Load: {canEvent}");
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            Vector2Int newGridPosition = GameManager.playerGridPosition + new Vector2Int(0, -2);

            bool[] result = thisScript.player.CheckRay(thisScript.transform.position, new Vector2(0, -2));

            if (result[0] || !result[1] || result[2])
            {
                result = thisScript.player.CheckRay(thisScript.transform.position, new Vector2(0, -1));

                if (result[0] || !result[1] || result[2])
                {
                    newGridPosition = GameManager.playerGridPosition;
                }
                else
                {
                    newGridPosition = GameManager.playerGridPosition + new Vector2Int(0, -1);
                }
            }
            else
            {
                newGridPosition = GameManager.playerGridPosition + new Vector2Int(0, -2);
            }


            thisScript.transform.position = GameManager.ChangeCoord(newGridPosition);
            GameManager.playerGridPosition = newGridPosition;

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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class ToughSurvival : IAbility // 7.질긴 생존
    {
        private EAbilityType mAbilityType = EAbilityType.DiePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 2;
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            EnemyValues enemyValue = null;
            foreach (EnemyValues enemysPos in GameManager.enemyValueList) // 죽은 적 기준으로 가장 가까운 적 확인
            {
                if (dist > Vector2.Distance(thisScript.targetEnemy.transform.position, enemysPos.position) && thisScript.targetEnemy.transform.position != enemysPos.position)
                {
                    dist = Vector2.Distance(thisScript.targetEnemy.transform.position, enemysPos.position);
                    enemyObj = EnemyManager.GetEnemyObject(enemysPos.position);
                    enemyValue = enemysPos;
                }
            }
            // hp 깍아내는 코드 나중에 최적화 필요할듯
            if (enemyObj)
            {
                enemyObj.transform.GetComponent<Enemy>().AttackedEnemy(1);
            }

            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
                result = thisScript.player.CheckRay(thisScript.targetEnemy.transform.position, exploablePosition[i]);

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
                GameObject enemyObj = EnemyManager.GetEnemyObject(explosionPos);
                if (enemyObj) // 만약 폭발 지점에 적이 존재했을 경우
                {
                    enemyObj.transform.GetComponent<Enemy>().AttackedEnemy(1);
                }
            }

            return false;
        }
        public void Reset()
        {
            canEvent = true;
            exploablePosition.Clear();
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            Vector2Int enemyTrimPos = GameManager.ChangeCoord(thisScript.targetEnemy.transform.position) + new Vector2Int(4, 4);
            int enemyMapNumber = enemyTrimPos.x + (enemyTrimPos.y * 9);

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
            GameObject enemyBackTarget = EnemyManager.GetEnemyObject(thisScript.targetEnemy.transform.position + new Vector3(0, 1.3f, 0));
            if (!enemyBackTarget) // 처치된 적 뒤에 아무런 유닛도 없을 경우
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        private bool bUsed;

        PlayerAbility thisScript;
        public PrecisionAttack(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
            bUsed = false;
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
            bUsed = true;
            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (bUsed) { thisScript.player.atk -= mValue; bUsed = false; }
            if (GameManager.Turn == 1) mCount = 2;
            if (mCount > 0) canEvent = true;
            Debug.Log(canEvent);
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class SmokeGrenade : IAbility, IActiveAbility, IAreaAbility // 13.연막탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.debuffs[Enemy.EDebuff.CantAttack] = 1; }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { enemy.debuffs[Enemy.EDebuff.CantAttack] = 1; }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { enemy.debuffs[Enemy.EDebuff.CantAttack] = 0; }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            // mAreaAbilityList.Add(new AreaAbility(2, targetPos));
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Turn, 2, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
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
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (mCount > 0)
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PoisonBomb : IAbility, IActiveAbility, IAreaAbility // 14.독성 폭탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { EnemyManager.GetEnemyValues(enemy.transform.position).moveCtrl -= 2; }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mValue = 1 + thisScript.additionalAbilityStat.throwingDamage;
            // mAreaAbilityList.Add(new AreaAbility(2, targetPos));
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Turn, 2, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
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
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (mCount > 0)
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class Grenade : IAbility, IActiveAbility, IAreaAbility // 15.수류탄
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); ; }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mValue = 2 + thisScript.additionalAbilityStat.throwingDamage;
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
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
            }
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.throwingCount;
            }
            if (mCount > 0)
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class AutoTrapSetting : IAbility, IAreaAbility // 22.자동 덫 설치
    {
        private EAbilityType mAbilityType = EAbilityType.MovePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        private int moveCount = 1; // 1인 이유는 이 MovePassive자체가 1번 움직이고 실행되는 코드이기 때문에 미리 1을 더해둠
        private Vector3 playerBeforePos;

        PlayerAbility thisScript;
        public AutoTrapSetting(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public EnterEvent enterEvent
        {
            get
            {
                return (Enemy enemy) =>
                {
                    // enemy.moveCtrl[1] -= 3;
                    EnemyManager.GetEnemyValues(enemy.transform.position).moveCtrl -= 3;
                    enemy.AttackedEnemy(1);
                };
            }
        }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            // 5번 움직였을 경우
            if (moveCount >= 5)
            {
                // Instantiate(thisScript.gameManager.autoTrap, playerBeforePos.position, Quaternion.identity); // 덫 설치용
                thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, GameManager.ChangeCoord(playerBeforePos), new List<Vector2Int>() { Vector2Int.zero }, true, enterEvent, stayEvent, exitEvent);
                moveCount = 1; // player 움직인 누적 횟수 초기화
            }
            else
            {
                playerBeforePos = thisScript.player.transform.position; // 플레이어 움직이고 난 후 transform 저장
                moveCount += 1; // 
            }
            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class KnifeMine : IAbility, IActiveAbility, IAreaAbility // 23. 칼날 지뢰
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0),
            new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2),
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 1),new Vector2Int(2, 2),
            new Vector2Int(1, -1), new Vector2Int(1, -2), new Vector2Int(2, -1),new Vector2Int(2, -2),
            new Vector2Int(-1, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1),new Vector2Int(-2, 2),
            new Vector2Int(-1, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1),new Vector2Int(-2, -2)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public KnifeMine(PlayerAbility playerAbility)
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); ; }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            // Debug.Log($"{targetPos}");
            mValue = 2 + thisScript.additionalAbilityStat.placeDamage;
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.placeCount;
            }
            if (mCount > 0)
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class SlipperyJelly : IAbility, IActiveAbility, IAreaAbility // 24. 미끌 젤리
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive; // 클릭 후 조준 후 바로 시작
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart; // 적 턴이 시작했을 때 리셋
        private bool mbEvent = true; // 초기 이벤트 설정 true
        private int mCount = 1; // 능력 사용 횟수
        private int mValue = 1; // 변수? 아마 적이 밟을 수 있는 최대 횟수를 저장해둔 것으로 추측
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None; // 능력 사용 조건(Player 기준) 제약 없음
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){ // 능력 사용 가능 범위
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0),
            new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2),
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 1),new Vector2Int(2, 2),
            new Vector2Int(1, -1), new Vector2Int(1, -2), new Vector2Int(2, -1),new Vector2Int(2, -2),
            new Vector2Int(-1, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1),new Vector2Int(-2, 2),
            new Vector2Int(-1, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1),new Vector2Int(-2, -2)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){ // 능력이 퍼짐 거리인데 추후 벽에 막힘에 따라 범위가 설정되어야 하므로 0,0 초기값 설정
            new Vector2Int(0, 0)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false }; // 엑티브 능력 중 설치 지속에 해당하는 배열을 가진건 알겠는데 각각 뭘 뜻하는지 잘 모르겠음
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public SlipperyJelly(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            playerAbility.shouldSetUpAbilityUI = true;
        }
        // 이전에 저장해둔 변수 초기값을 그대로 이월
        public DisposableButton.ActiveCondition activeCondition { get { return mActiveCondition; } }
        public List<Vector2Int> attackRange { get { return mAttackRange; } }
        public List<Vector2Int> attackScale { get { return mAttackScale; } }
        public bool[] canPenetrate { get { return bCanPenetrate; } }
        public Vector2Int targetPos { get { return mTargetPos; } set { mTargetPos = value; } }
        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); ; }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            mValue = 1 + thisScript.additionalAbilityStat.placeDamage; // 초기 데미지 1 + 데미지 증가 능력을 골랐을 때 더해줌
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) // 스테이지가 변경되었을 때 1회 실행
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.placeCount; // 초기 사용 가능 횟수 1회에서 placeCount를 더해 횟수 조정
            }
            if (mCount > 0) // 미끌젤리 사용 횟수가 남아있을 경우
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PlaceDamageUp1 : IAbility // 25.설치 데미지 증가1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public PlaceDamageUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.placeDamage += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.placeDamage -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PlaceDamageUp2 : IAbility // 26.설치 데미지 증가2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public PlaceDamageUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.placeDamage += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.placeDamage -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PlaceCountUp1 : IAbility // 27.설치 개수 증가1
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public PlaceCountUp1(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.placeCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.placeCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PlaceCountUp2 : IAbility // 28.설치 개수 증가2
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int mValue = 1;

        PlayerAbility thisScript;
        public PlaceCountUp2(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.additionalAbilityStat.placeCount += mValue;
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            thisScript.additionalAbilityStat.placeCount -= mValue;

            return false;
        }
        public void Reset()
        {
            return;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PlaceDummy : IAbility, IActiveAbility, IAreaAbility // 29. 더미 설치
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(-1, 0), new Vector2Int(-2, 0),
            new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(0, -1), new Vector2Int(0, -2),
            new Vector2Int(1, 1), new Vector2Int(1, 2), new Vector2Int(2, 1),new Vector2Int(2, 2),
            new Vector2Int(1, -1), new Vector2Int(1, -2), new Vector2Int(2, -1),new Vector2Int(2, -2),
            new Vector2Int(-1, 1), new Vector2Int(-1, 2), new Vector2Int(-2, 1),new Vector2Int(-2, 2),
            new Vector2Int(-1, -1), new Vector2Int(-1, -2), new Vector2Int(-2, -1),new Vector2Int(-2, -2)
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0)
        };
        private bool[] bCanPenetrate = new bool[2] { true, false };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public PlaceDummy(PlayerAbility playerAbility)
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            thisScript.SetAreaAbility(AreaAbility.ELifeType.Dummy, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1)
            {
                canEvent = true;
                mCount = 1 + thisScript.additionalAbilityStat.placeCount;
            }
            if (mCount > 0)
            {
                canEvent = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class ArtilleryFire : IAbility, IActiveAbility, IAreaAbility // 30.포병 화력
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        // private int mCount = 1;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(-1, 0),  new Vector2Int(0, 1), new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1),new Vector2Int(-1, -1)
        };
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public ArtilleryFire(PlayerAbility playerAbility)
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            canEvent = false;

            AreaAbility areaAbility = thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);
            BoxCollider2D[] boxColliders = areaAbility.transform.GetComponents<BoxCollider2D>().Where(boxCollider => boxCollider.enabled).ToArray();
            int[] randomIndex = new int[5];
            if (boxColliders.Length >= 5)
            {
                for (int i = 0; i < 5; i++)
                {
                    int index;
                    do
                    {
                        index = UnityEngine.Random.Range(0, boxColliders.Length);
                    } while (randomIndex.Contains(index));
                    randomIndex[i] = index;
                    boxColliders[index].enabled = false;
                }
            }


            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class RequestSniping : IAbility, IActiveAbility, IAreaAbility // 31.저격 요청
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        // private int mCount = 1;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>(){
            new Vector2Int(0, 0)
        };
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public RequestSniping(PlayerAbility playerAbility)
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            canEvent = false;

            thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);


            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PrecisionBomb : IAbility, IActiveAbility, IAreaAbility // 32.정밀 폭격
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
        public EnterEvent enterEvent { get { return (Enemy enemy) => { enemy.AttackedEnemy(mValue); }; } }
        public StayEvent stayEvent { get { return (Enemy enemy) => { }; } }
        public ExitEvent exitEvent { get { return (Enemy enemy) => { }; } }
        public bool Event()
        {
            Debug.Log($"{targetPos}");
            canEvent = false;

            thisScript.SetAreaAbility(AreaAbility.ELifeType.Count, 1, targetPos, attackScale, canPenetrate[1], enterEvent, stayEvent, exitEvent);

            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class EvasiveManeuver : IAbility // 33.회피 기동
    {
        private EAbilityType mAbilityType = EAbilityType.AttackPassive;
        private EResetTime mResetTime = EResetTime.OnEveryTick;
        private bool mbEvent = false;
        // private int mCount = 1;
        private int tempTurn;

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
            thisScript.gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Move;

            thisScript.player.movablePositions = newMovablePositions.ToList();
            thisScript.player.moveCount++;
            thisScript.player.isDisposableMove = true;
            canEvent = false;
            tempTurn = GameManager.Turn;
            return false;
        }
        public void Reset()
        {
            if (!canEvent)
            {
                if (!thisScript.player.isDisposableMove)
                {
                    canEvent = true;
                    thisScript.player.movablePositions = originMovablePositions.ToList();
                }
                if (GameManager.Turn == 1 && !canEvent)
                {
                    canEvent = true;
                    thisScript.player.isDisposableMove = false;
                    thisScript.player.movablePositions = originMovablePositions.ToList();
                }
                if (tempTurn != GameManager.Turn)
                {
                    canEvent = true;
                    thisScript.player.isDisposableMove = false;
                    thisScript.player.movablePositions = originMovablePositions.ToList();
                }
            }

        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            return false;
        }
        public void Reset()
        {
            thisScript.player.buildCount++;
            thisScript.player.moveCount++;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class FastManeuver : IAbility, IActiveAbility // 35.빠른 기동
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 2;
        private int mValue = 1;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public FastManeuver(PlayerAbility playerAbility)
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
            thisScript.player.moveCount++;
            mCount--;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) mCount = 2;
            if (mCount > 0) canEvent = true;
            Debug.Log(canEvent);
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            Vector3 newPosition = thisScript.targetEnemy.transform.position + GameManager.ChangeCoord(new Vector2Int(0, 1));
            bool[] result = thisScript.player.CheckRay(thisScript.targetEnemy.transform.position, Vector2.up);
            if (!result[0] && !result[2])
            {
                if (result[1])
                {
                    EnemyManager.GetEnemyValues(thisScript.targetEnemy.transform.position).position = newPosition;
                    return false;
                }
                if (thisScript.targetEnemy.name.Contains("EnemyShieldSoldier"))
                {
                    if (!Physics2D.RaycastAll(thisScript.targetEnemy.transform.position, Vector2.up, GameManager.gridSize, LayerMask.GetMask("Wall")).Any(h => h.transform.name.Contains("PlayerWall")))
                    {
                        EnemyManager.GetEnemyValues(thisScript.targetEnemy.transform.position).position = newPosition;
                        return false;
                    }
                }
            }
            // Debug.Log(newPosition);

            return false;
        }
        public void Reset()
        {
            canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
                // foreach (GameObject enemyObject in GameObject.FindGameObjectsWithTag("Enemy"))
                // {
                //     Enemy enemy = enemyObject.GetComponent<Enemy>();
                //     enemy.moveCtrl[1] = Mathf.Max(enemy.moveCtrl[1] - 3, 0);
                // }
                foreach (EnemyValues enemyValues in GameManager.enemyValueList)
                {
                    enemyValues.moveCtrl -= 3;
                }
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
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
            EnemyManager.GetEnemyValues(thisScript.targetEnemy.transform.position).moveCtrl -= 3;
            // thisScript.targetEnemy.GetComponent<Enemy>().moveCtrl[1] -= 3;
            return false;
        }

        public void Reset()
        {
            canEvent = true;
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class Imprison : IAbility, IActiveAbility // 39.수감
    {
        private EAbilityType mAbilityType = EAbilityType.TargetActive;
        private EResetTime mResetTime = EResetTime.OnEveryTick;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>() { Vector2Int.zero };
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;
        private Vector2Int tempPosition;
        private int tempTurn = 1;
        private bool shouldSetRange = true;

        PlayerAbility thisScript;
        public Imprison(PlayerAbility playerAbility)
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
            EnemyManager.GetEnemy(GameManager.ChangeCoord(targetPos)).debuffs[Enemy.EDebuff.CantMove] = 2;
            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (tempTurn != GameManager.Turn)
            {
                if (GameManager.Turn == 1)
                {
                    canEvent = true;
                }
                shouldSetRange = true;
                tempTurn = GameManager.Turn;
            }
            if (canEvent & GameManager.Turn == Player.playerOrder)
            {
                if (shouldSetRange)
                {
                    attackRange.Clear();
                    foreach (var enemyValues in GameManager.enemyValueList)
                    {
                        attackRange.Add(GameManager.ChangeCoord(enemyValues.position) - GameManager.playerGridPosition);
                    }
                    tempPosition = GameManager.playerGridPosition;
                }
                if (tempPosition != GameManager.playerGridPosition) shouldSetRange = true;
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class MindControl : IAbility, IActiveAbility // 40.정신 조종
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 1;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public MindControl(PlayerAbility playerAbility)
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
            Enemy targetEnemy = EnemyManager.GetEnemy(GameManager.enemyValueList.OrderBy(enemyValue => (enemyValue.position - GameManager.ChangeCoord(GameManager.playerGridPosition)).magnitude).ToList()[0].position);
            Tuple<Enemy, Vector2Int> closestEnemy = null;
            foreach (var attackablePoint in targetEnemy.attackablePoints)
            {
                bool[] result = thisScript.player.CheckRay(targetEnemy.transform.position, attackablePoint);
                if (result[0]) continue;
                if (result[1] || canPenetrate[1])
                {
                    if (result[2])
                    {
                        if (closestEnemy == null) closestEnemy = new Tuple<Enemy, Vector2Int>(EnemyManager.GetEnemy(targetEnemy.transform.position + GameManager.ChangeCoord(attackablePoint)), attackablePoint);
                        else if (attackablePoint.magnitude < closestEnemy.Item2.magnitude)
                        {
                            closestEnemy = new Tuple<Enemy, Vector2Int>(EnemyManager.GetEnemy(targetEnemy.transform.position + GameManager.ChangeCoord(attackablePoint)), attackablePoint);
                        }
                    }
                }
            }
            if (closestEnemy == null) targetEnemy.AttackedEnemy(mValue);
            else closestEnemy.Item1.AttackedEnemy(mValue);
            mCount--;
            canEvent = false;
            thisScript.RemoveAbility(40);
            return false;
        }
        public void Reset()
        {
            if (GameManager.Turn == 1) mCount = 1;
            if (mCount > 0) canEvent = true;
            Debug.Log(canEvent);
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class ReRoll : IAbility // 41.리롤
    {
        private EAbilityType mAbilityType = EAbilityType.ValuePassive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public ReRoll(PlayerAbility playerAbility)
        {
            thisScript = playerAbility;
            thisScript.gameManager.GetComponent<AbilitySelect>().AbilitySelectStart();
            thisScript.RemoveAbility(41);
        }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public EResetTime resetTime { get { return mResetTime; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            return false;
        }

        public void Reset()
        {
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class PoisonousCocktail : IAbility, IActiveAbility // 42.약독주
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 3;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public PoisonousCocktail(PlayerAbility playerAbility)
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

            canEvent = false;
            return false;
        }
        public void Reset()
        {
            if (!canEvent)
            {
                mCount--;
                if (mCount == 1)
                {
                    thisScript.player.atk -= mValue + 1;
                }
                else if (mCount == 0)
                {
                    thisScript.player.atk += 1;
                    thisScript.RemoveAbility(42);
                }
            }
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class Conciliation : IAbility, IActiveAbility // 43.회유
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 3;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>(){
            new Vector2Int(1, 0), new Vector2Int(-1, 0),new Vector2Int(0, 1),new Vector2Int(0, -1),
            new Vector2Int(1, 1), new Vector2Int(-1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1),
        };
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public Conciliation(PlayerAbility playerAbility)
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
            foreach (var attackPosition in attackRange)
            {
                if (EnemyManager.GetEnemy(out Enemy enemy, GameManager.ChangeCoord(GameManager.playerGridPosition + attackPosition), false) != null)
                {
                    enemy.AttackedEnemy(enemy.hp);
                }
            }
            canEvent = false;
            thisScript.RemoveAbility(43);
            return false;
        }
        public void Reset()
        {
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
    class SleepingSand : IAbility, IActiveAbility // 44.수면 모래
    {
        private EAbilityType mAbilityType = EAbilityType.InstantActive;
        private EResetTime mResetTime = EResetTime.OnEnemyTurnStart;
        private bool mbEvent = true;
        private int mCount = 3;
        private int mValue = 3;
        private DisposableButton.ActiveCondition mActiveCondition = DisposableButton.ActiveCondition.None;
        private List<Vector2Int> mAttackRange = new List<Vector2Int>();
        private List<Vector2Int> mAttackScale = new List<Vector2Int>();
        private bool[] bCanPenetrate = new bool[2] { true, true };
        private Vector2Int mTargetPos;

        PlayerAbility thisScript;
        public SleepingSand(PlayerAbility playerAbility)
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
            foreach (Transform child in GameObject.FindWithTag("EnemyBox").transform)
            {
                Enemy enemy = child.GetComponent<Enemy>();
                enemy.debuffs[Enemy.EDebuff.Sleep] = 1;
                enemy.debuffs[Enemy.EDebuff.CantMove] = 1;
            }
            canEvent = false;
            thisScript.RemoveAbility(44);
            return false;
        }
        public void Reset()
        {
        }
        public string Save() { return string.Empty; }
        public void Load(string data) { }
    }
}