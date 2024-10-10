using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterDefinition;
using HM.Physics;

public class BaseCharacter
{
    /// <summary>
    /// BaseCharacter의 기본 행동들을 도와줄때 사용할 스크립트 && 데이터 선언
    /// 변경 X -> 불러와 사용만 가능한 데이터. CharacterController에서 바꿀 데이터가 있을 경우 해당 스크립트에서만 관리할 예정
    /// rangeFrame 동일.
    /// </summary>
    protected readonly CharacterController controller;
    protected readonly RangeFrame rangeFrame;
    protected private Dictionary<string, object> dataSet;

    /// <summary>
    /// Player 기물의 범위 표현을 도와주는 게임 오브젝트 모음
    /// </summary>
    #region PreviewObjectsValues
    protected readonly PlayerPrefabs playerPrefabs; // 플레이어 관련 프리팹 모음
    private List<GameObject> playerPreviews          = new List<GameObject>();
    private List<GameObject> playerAttackPreviews    = new List<GameObject>();
    private List<GameObject> playerAttackHighlights  = new List<GameObject>();
    private List<GameObject> playerAbilityPreviews   = new List<GameObject>();
    private List<GameObject> playerAbilityHighlights = new List<GameObject>();
    private GameObject playerWallPreview;
    private GameObject wallStorage;
    private GameObject previewStorage;
    #endregion

    // 적 플레이어 공용 변수 모음 (변경될 수 있음 아직 미정) -> 기획에 따라 변경될 예정
    #region TokenValues
    public int maxHp;
    public Vector2 mPosition;
    private List<Vector2Int> movablePositions = new List<Vector2Int>();
    private List<Vector2Int> attackablePositions = new List<Vector2Int>();

    public enum ECharacterType { Mutu = 0, Mana = 1, Machine = 2 }
    public enum EPositionType { Tanker = 0, Attacker = 1, Supporter = 2 }
    public Sprite characterSprite;
    private int moveCtrl = 0;

    private bool bMove = true;
    private bool bAttack = true;
    public bool canAction = true;
    public bool canAttack
    {
        get
        {
            return bAttack;
        }
        set
        {
            bAttack = value;
        }
    }

    public bool canMove
    {
        get
        {
            return bMove;
        }
        set
        {
            bMove = value;
        }
    }
    #endregion

    // 적 해당 변수 모음 (변경될 수 있음 아직 미정) -> 기획에 따라 변경될 예정
    #region EnemyValues
    #endregion

    // 플레어만 사용되는 변수 모음 (변경될 수 있음 아직 미정) -> 기획에 따라 변경될 예정
    #region PlayerValues
    public bool canBuild
    {
        get
        {
            return GameManager.Instance.playerWallCount < GameManager.Instance.playerMaxBuildWallCount && moveCtrl >= 100;
        }
    }
    public bool canDestroy
    {
        get
        {
            return GameManager.Instance.playerDestroyedWallCount < GameManager.Instance.playerMaxDestroyWallCount && moveCtrl >= 100;
        }
    }
    public int buildInteractionDistance = 100;
    public int destroyInteractionDistance = 100;
    private Vector2 touchPosition = new Vector2(0, 0);
    #endregion

    #region LoadToDataSet
    public int id; // 고유 인덱스
    public bool playerable; // Player True, Enemy False
    public string characterName; // 캐릭터 이름
    public ECharacterType characterType; // 캐릭터 타입
    public EPositionType characterPosition; // 캐릭터 포지션

    public Vector2 position
    {
        get
        {
            return mPosition;
        }

        set
        {
            GameObject currentEnemy = controller.GetObjectToPosition(mPosition);
            if(currentEnemy)
                currentEnemy.transform.position = value * GameManager.gridSize;
            mPosition = value;
        }
    }
    public int attack;
    public int hp;
    public float damageResistance; // 피해저항
    public int turnUpAction; // 증가 행동력
    public int skillIndex;
    public int moveRangeIndex;
    public int attackRangeIndex;
    #endregion

    public BaseCharacter(CharacterController controller)
    {
        this.controller = controller;
        playerPrefabs = controller.playerPrefabs;
        rangeFrame = playerPrefabs.rangeFrame.GetComponent<RangeFrame>();
    }

    // 캐릭터 position마다 정해져있는 알고리즘에 맞게 구성
    /// <summary>
    /// 상속 함수 같은 경우 개별로 처리해야하는 기능이 없는 경우 공통 함수로 변경 예정
    /// </summary>
    
    public virtual void Start()
    {
        if (playerable) PlayerStart();
    }

    public virtual void Update()
    {
        if (playerable) PlayerUpdate();
    }

    public virtual void Reset()
    {

    }

    public virtual void Move()
    {
        Debug.LogFormat("{0} 캐릭터 Move함수 실행", characterName);
    }

    public virtual void Attack()
    {
        Debug.LogFormat("{0} 캐릭터 Attack함수 실행", characterName);
    }

    public virtual void Build()
    {
        Debug.LogFormat("{0} 캐릭터 Build함수 실행", characterName);
    }

    public virtual void Ability()
    {
        Debug.LogFormat("{0} 캐릭터 Ability함수 실행", characterName);
    }

    public virtual void HealthRecovery(int recovery)
    {
        Debug.LogFormat("{0} 캐릭터 HealthRecovery함수 실행", characterName);
        hp += recovery;       
    }

    public virtual void TakeDamage(BaseCharacter baseCharacter, int damage = 0)
    {
        Debug.LogFormat("{0} 캐릭터 TakeDamage함수 실행", characterName);
        if (baseCharacter == null)
        {
            return;
        }
        // 일정 데미지를 입력하지 않았을 경우 (능력으로 인한 데미지 예외처리)
        if (damage == 0) damage = baseCharacter.attack;

        if (Random.Range(0.0f, 1.0f) < damageResistance)
        {
            Debug.LogFormat("캐릭터 이름 {0}이 공격을 회피했습니다.", characterName);
        }
        else
        {
            Debug.LogFormat("캐릭터 이름 {0}이 데미지({1})를 입었습니다. 현재 체력 : {2}", characterName, damage, hp - damage);
            hp -= damage;
        }
    }

    public void SetData(Dictionary<string, object> dataSet)
    {
        this.dataSet        = dataSet;
        playerable          = bool.Parse(dataSet["playable"].ToString());
        characterName       = (string)dataSet["ch_name"];
        characterType       = (ECharacterType)dataSet["ch_type"];
        characterPosition   = (EPositionType)dataSet["position"];
        maxHp               = (int)dataSet["hp"];
        hp                  = maxHp;
        attack              = (int)dataSet["atk"];
        damageResistance    = (float)dataSet["rs"];
        turnUpAction        = (int)dataSet["tia"];
        skillIndex          = (int)dataSet["skill_id"];
        moveRangeIndex      = (int)dataSet["mov_rg"];
        attackRangeIndex    = (int)dataSet["atk_rg"];
        characterSprite     = playerable ? Resources.Load<Sprite>("Sprites/Player/" + characterName) : Resources.Load<Sprite>("Sprites/Enemy/" + characterName); // 스프라이트 가져오기
                            
        movablePositions    = rangeFrame.SelectFieldProperty(EPlayerRangeField.Move, moveRangeIndex);
        attackablePositions = rangeFrame.SelectFieldProperty(EPlayerRangeField.Attack, moveRangeIndex);
        Debug.LogFormat("{0}이 생성되었습니다.", characterName);
    }

    public Dictionary<string, object> SendData()
    {
        return dataSet;
    }

    public BaseCharacter DeepCopy()
    {
        BaseCharacter baseCharacter = new BaseCharacter(controller);
        baseCharacter.playerable    = playerable;
        baseCharacter.characterName = characterName;
        baseCharacter.characterType = characterType;
        baseCharacter.characterPosition = characterPosition;
        baseCharacter.maxHp = maxHp;
        baseCharacter.hp = hp;
        baseCharacter.attack = attack;
        baseCharacter.damageResistance = damageResistance;
        baseCharacter.turnUpAction = turnUpAction;
        baseCharacter.skillIndex = skillIndex;
        baseCharacter.moveRangeIndex = moveRangeIndex;
        baseCharacter.attackRangeIndex = attackRangeIndex;
        baseCharacter.characterSprite = characterSprite;
        baseCharacter.movablePositions = movablePositions;
        baseCharacter.attackablePositions = attackablePositions;
        return baseCharacter;
    }

    public void ResetPreview()
    {

    }

    private void PlayerStart()
    {
        wallStorage = GameObject.FindGameObjectWithTag("WallStorage");
        previewStorage = GameObject.FindGameObjectWithTag("PreviewStorage");

        for (int i = 0; i < movablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerPreviews.Add(controller.SetObjectToParent(playerPrefabs.playerPreview, previewStorage));
            playerPreviews[i].SetActive(false);
        }
        for (int i = 0; i < attackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackPreviews.Add(controller.SetObjectToParent(playerPrefabs.attackPreview, previewStorage));
            playerAttackPreviews[i].SetActive(false);
        }
        //for (int i = 0; i < attackPositions.Count; i++) // 플레이어 공격 하이라이트 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerAttackHighlights.Add(controller.SetObjectToParent(playerPrefabs.attackHighlight, previewStorage));
        //    playerAttackHighlights[i].SetActive(false);
        //}
        for (int i = 0; i < 81; i++) // 플레이어 능력 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityPreviews.Add(controller.SetObjectToParent(playerPrefabs.attackPreview, previewStorage, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize));
            //playerAbilityPreviews[i].transform.parent = previewStorage.transform;
            playerAbilityPreviews[i].SetActive(false);
        }
        for (int i = 0; i < 81; i++) // 플레이어 능력 하이라이트 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityHighlights.Add(controller.SetObjectToParent(playerPrefabs.attackHighlight, previewStorage, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize));
            //playerAbilityHighlights[i].transform.parent = previewStorage.transform;
            playerAbilityHighlights[i].SetActive(false);
        }
        for (int i = 0; i < GameManager.Instance.playerMaxBuildWallCount; i++)
        {
            controller.SetObjectToParent(playerPrefabs.wall, wallStorage).SetActive(false);
        }
        playerWallPreview = controller.SetObjectToParent(playerPrefabs.wallPreview, null, position); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
        playerWallPreview.SetActive(false);
    }

    // CharacterController에서 currentPlayer가 누군지에 따라 해당 BaseCharacter 스크립트안에 실행되는 playerUpdate 결정
    // BaseCharacter에서 결정 X
    private void PlayerUpdate()
    {
        touchPosition = controller.touchPos;
        switch (controller.playerControlStatus)
        {
            case EPlayerControlStatus.Move:
                if (canMove) Move();
                else ResetPreview();
                break;
            case EPlayerControlStatus.Build:
                if (canBuild) Build();
                else ResetPreview();
                break;
            case EPlayerControlStatus.Attack:
                if (canAttack) Attack();
                else ResetPreview();
                break;
            // case GameManager.EPlayerControlStatus.Ability:
            //     if (abilityCount > 0) UseAbility();
            //     else ResetPreview();
            //     break;
            //case EPlayerControlStatus.Destroy:
            //    if (canDestroy) Destroy();
            //    else ResetPreview();
            //    break;

            default:
                break;
        }
    }

    
    private void SetPreviewPlayer()
    {
        for (int i = 0; i < movablePositions.Count; i++)
        {
            bool[] result = HMPhysics.CheckRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i]);
            if (result[0])
            {
                playerPreviews[i].SetActive(false);
                continue;
            }
            if (result[1])
            {
                if (!result[2])
                {
                    Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.green, 0.1f);
                    playerPreviews[i].transform.position = controller.currentCtrlCharacter.transform.position + GameManager.ChangeCoord(movablePositions[i]);
                    playerPreviews[i].SetActive(true);
                }
                else
                {
                    Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.yellow, 0.1f);
                }
                continue;
            }
            else
            {
                Debug.DrawRay(controller.currentCtrlCharacter.transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.red, 0.1f);
            }
        }
    }
}
