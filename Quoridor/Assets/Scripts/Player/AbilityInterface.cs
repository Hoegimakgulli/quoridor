using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IAbility   // 능력 인터페이스
{
    PlayerAbility.EAbilityType abilityType { get; }  // 능력 타입
    bool canEvent { get; set; }  // 이벤트 가능 여부 (쿨다운 관련)
    bool Event(); // 이벤트 (능력 함수 적는 부분)
    PlayerAbility.EResetTime resetTime { get; }  // 리셋 주기
    void Reset(); // 리셋 (리셋, 쿨다운 관련, 턴이 넘어갈때마다 실행)
    string Save();
    void Load(string data);
}
public interface ISaveLoad
{
    string Save();
    void Load(string data);
}
public interface IActiveAbility  // 액티브 능력 인터페이스
{
    DisposableButton.ActiveCondition activeCondition { get; } // 능력 사용 전제 조건 설정
    List<Vector2Int> attackRange { get; } // 타깃일 경우 공격 사거리(길이가 0일경우 전범위)
    List<Vector2Int> attackScale { get; } // 타깃일 경우 공격 범위
    bool[] canPenetrate { get; }           // 공격 관통 여부  [0]:플레이어to타깃 관통 [1]:투사체 중심 부터 다른 범위
    Vector2Int targetPos { get; set; } // 투척 좌표
}
public interface IAreaAbility // 지속&배치형 능력 인터페이스
{
    EnterEvent enterEvent { get; } // Enter 이벤트
    StayEvent stayEvent { get; }   // Stay 이벤트
    ExitEvent exitEvent { get; }   // Exit 이벤트
}
// public class AreaAbility // 지속형 능력 class
// {
//     public int life;  // 지속 기간
//     public Vector2Int targetPos;  // 타깃 좌표
//     public List<GameObject> targetList = new List<GameObject>(); // 타깃 오브젝트
//     public AreaAbility(int life, Vector2Int targetPos) { this.life = life; this.targetPos = targetPos; } // 생성자
// }