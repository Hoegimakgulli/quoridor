using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseCharacter
{
    protected readonly CharacterController controller;
    protected private Dictionary<string, object> dataSet;
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
}
