using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCharacter
{
    protected readonly CharacterController controller;
    protected private Dictionary<string, object> dataSet;

    protected readonly PlayerPrefabs playerPrefabs; // 플레이어 관련 프리팹 모음
    private List<GameObject> playerPreviews = new List<GameObject>();
    private List<GameObject> playerAttackPreviews = new List<GameObject>();
    private List<GameObject> playerAttackHighlights = new List<GameObject>();
    private List<GameObject> playerAbilityPreviews = new List<GameObject>();
    private List<GameObject> playerAbilityHighlights = new List<GameObject>();
    GameObject playerWallPreview;

    public int maxHp;
    public int hp;
    public Vector2 Position;

    public enum ECharacterType { Mutu = 0, Mana = 1, Machine = 2 }
    public enum EPositionType { Tanker = 0, Attacker = 1, Supporter = 2 }

    public int id; // 고유 인덱스
    public bool playerable; // Player True, Enemy False
    public string characterName; // 캐릭터 이름
    public ECharacterType characterType; // 캐릭터 타입
    public EPositionType characterPosition; // 캐릭터 포지션

    public Vector2 position
    {
        get
        {
            return Position;
        }

        set
        {
            GameObject currentEnemy = controller.GetObjectToPosition(Position);
            currentEnemy.transform.position = value * GameManager.gridSize;
            Position = value;
        }
    }
    public Sprite characterSprite;
    public int attack;
    public float damageResistance; // 피해저항
    public int turnUpAction; // 증가 행동력
    public int skillIndex;
    public int moveRangeIndex;
    public int attackRangeIndex;

    public BaseCharacter(CharacterController controller)
    {
        this.controller = controller;
        playerPrefabs = controller.playerPrefabs;
    }

    // 캐릭터 position마다 정해져있는 알고리즘에 맞게 구성
    /// <summary>
    /// 상속 함수 같은 경우 개별로 처리해야하는 기능이 없는 경우 공통 함수로 변경 예정
    /// </summary>
    
    public virtual void Start()
    {

    }

    public virtual void Update()
    {
        
    }

    public virtual void Reset()
    {

    }

    public virtual void Move(Vector2 targetPos)
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
        this.dataSet = dataSet;
        playerable = bool.Parse(dataSet["playable"].ToString());
        characterName = (string)dataSet["ch_name"];
        characterType = (ECharacterType)dataSet["ch_type"];
        characterPosition = (EPositionType)dataSet["position"];
        maxHp = (int)dataSet["hp"];
        hp = maxHp;
        attack = (int)dataSet["atk"];
        damageResistance = (float)dataSet["rs"];
        turnUpAction = (int)dataSet["tia"];
        skillIndex = (int)dataSet["skill_id"];
        moveRangeIndex = (int)dataSet["mov_rg"];
        attackRangeIndex = (int)dataSet["atk_rg"];
        characterSprite = playerable ? Resources.Load<Sprite>("Sprites/Player/" + characterName) : Resources.Load<Sprite>("Sprites/Enemy/" + characterName); // 스프라이트 가져오기
        Debug.LogFormat("{0}이 생성되었습니다.", characterName);
    }

    public Dictionary<string, object> SendData()
    {
        return dataSet;
    }

    private void PlayerStart()
    {
        //gameManager = GameManager.Instance;
        //wallStorage = GameObject.FindGameObjectWithTag("WallStorage");
        //previewStorage = GameObject.FindGameObjectWithTag("PreviewStorage");

        ////testState = StateManager.randPlayer();
        ////Debug.Log(testState.characterName);
        //// GameManager.playerGridPositionList.Add(GameManager.ChangeCoord(transform.position));
        //for (int i = 0; i < movablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerPreviews.Add(Instantiate(playerPrefabs.playerPreview, previewStorage.transform));
        //    playerPreviews[i].SetActive(false);
        //}
        //for (int i = 0; i < attackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerAttackPreviews.Add(Instantiate(playerPrefabs.attackPreview, previewStorage.transform));
        //    playerAttackPreviews[i].SetActive(false);
        //}
        //for (int i = 0; i < attackPositions.Count; i++) // 플레이어 공격 하이라이트 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerAttackHighlights.Add(Instantiate(playerPrefabs.attackHighlight, previewStorage.transform));
        //    playerAttackHighlights[i].SetActive(false);
        //}
        //for (int i = 0; i < 81; i++) // 플레이어 능력 미리보기 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerAbilityPreviews.Add(Instantiate(playerPrefabs.attackPreview, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
        //    playerAbilityPreviews[i].transform.parent = previewStorage.transform;
        //    playerAbilityPreviews[i].SetActive(false);
        //}
        //for (int i = 0; i < 81; i++) // 플레이어 능력 하이라이트 -> 미리소환하여 비활성화 해놓기
        //{
        //    playerAbilityHighlights.Add(Instantiate(playerPrefabs.attackHighlight, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
        //    playerAbilityHighlights[i].transform.parent = previewStorage.transform;
        //    playerAbilityHighlights[i].SetActive(false);
        //}
        //for (int i = 0; i < gameManager.playerMaxBuildWallCount; i++)
        //{
        //    Instantiate(playerPrefabs.wall, wallStorage.transform).SetActive(false);
        //}
        //for (int i = 0; i < 81; i++)
        //{
        //    allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        //}
        //playerWallPreview = Instantiate(playerPrefabs.wallPreview, transform.position, Quaternion.identity); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
        //playerWallPreview.SetActive(false);
    }
}
