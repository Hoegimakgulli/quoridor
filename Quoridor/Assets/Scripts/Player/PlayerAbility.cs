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
    public enum EAbilityType { DiePassive, MovePassive, AttackPassive, HitPassive, KillPassive, Active }

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
            case 6:
                abilities.Add(new Shield(this));
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
            case 22:
                abilities.Add(new AutoTrapSetting(this));
                break;
            case 33:
                abilities.Add(new EvasiveManeuver(this));
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

    public GameObject FindValuesObj(Vector3 position)
    {
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        foreach (Transform child in enemyBox.transform)
        {
            Debug.Log(child.position);
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

    class ChainLightning : IAbility // 9.체인라이트닝
    {
        private EAbilityType mAbilityType = EAbilityType.KillPassive;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public ChainLightning(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            float dist = 10000000;
            GameObject enemyObj = null;
            enemyValues enemyValue = null;
            foreach(enemyValues enemysPos in GameManager.enemyValueList) // 죽은 적 기준으로 가장 가까운 적 확인
            {
                if(dist > Vector2.Distance(thisScript.targetEnemy.transform.position, enemysPos.position))
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
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        private List<Vector2> exploablePosition = new List<Vector2>();
        bool[] result;
        public ChaineExplosion(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
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

            for(int i = 0; i < exploablePosition.Count; i++)
            {
                Vector3 explosionPos = thisScript.targetEnemy.transform.position + ((Vector3)exploablePosition[i] * GameManager.gridSize);
                GameObject enemyObj = thisScript.FindValuesObj(explosionPos);
                if (enemyObj) // 만약 폭발 지점에 적이 존재했을 경우
                {
                    foreach(enemyValues child in GameManager.enemyValueList) // 리스트에서 찾아서 hp 다운
                    {
                        if(child.position == enemyObj.transform.position)
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
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public PenetrateAttack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            Vector2 enemyTrimPos = (thisScript.targetEnemy.transform.position + new Vector3(4, 4, 0)) / GameManager.gridSize;
            int enemyMapNumber = (int)(enemyTrimPos.x + (enemyTrimPos.y * 9));
            
            // 예외 처리
            if(enemyMapNumber > 72) // 현재 처치된 적이 위쪽 외벽에 붙어있을 경우
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
    class AutoTrapSetting : IAbility // 22.자동 덫 설치
    {
        private EAbilityType mAbilityType = EAbilityType.MovePassive;
        private bool mbEvent = false;
        private int mCount = 1;

        private int moveCount = 1; // 1인 이유는 이 MovePassive자체가 1번 움직이고 실행되는 코드이기 때문에 미리 1을 더해둠
        private Transform playerBeforePos;

        PlayerAbility thisScript;
        public AutoTrapSetting(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
        public bool canEvent { get { return mbEvent; } set { mbEvent = value; } }
        public bool Event()
        {
            // 5번 움직였을 경우
            if(moveCount >= 5)
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
    class AnkleAttack : IAbility // 38.발목 공격
    {
        private EAbilityType mAbilityType = EAbilityType.HitPassive;
        private bool mbEvent = false;
        private int mCount = 1;

        PlayerAbility thisScript;
        public AnkleAttack(PlayerAbility playerAbility) { thisScript = playerAbility; }

        public EAbilityType abilityType { get { return mAbilityType; } }
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