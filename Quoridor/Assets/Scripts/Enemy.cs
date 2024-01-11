using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions  = new List<Vector3>();    // 모든 적들 위치 정보 저장
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장

    //-------------- Enemy Values --------------//
    public int cost;                                  // 소환 시 필요한 비용
    public int hp;                                    // 받아야하는 총 체력
    public int maxHp;
    public int[] moveCtrl = new int[3];               // 0 = 요구 행동력, 1 = 현재 채워져 있는 행동력, 2 = 랜덤 행동력 충전 최대치
    public Vector2Int[] moveablePoints;
    public Vector2Int[] attackablePoints;
    public enum EState { Idle, Move, Attack, Dead };
    public enum EValue { Normal = 0, Champion = 1, Named = 2, Boss = 3 }; // 0 = Normal, 1 = Champion, 2 = Named, 3 = Boss
    // 0 - 전진해 player를 공격, 1 - 뒤 포지션을 잡으면서 플레이어 공격, 2 - 자기 구역을 사수하면서 플레이어를 공격 
    // 추후 유닛 특성 상속받을 때 사용 현재 미사용.
    public enum ECharacteristic { Forward, BackWard, Hold };
    //------------------------------------------//

    public EState state = EState.Idle;
    public ECharacteristic characteristic = ECharacteristic.Forward;
    public EValue value = EValue.Normal;


    //--------------- Move 시작 ---------------//
    // 모든 enemy 객체 동시에 움직임 실시
    public void EnemyMove(List<Path> path)
    {
        // enemy가 Move 상태일 때 유닛의 특징에 따라 움직이는 범위 조정
        if (state == EState.Move)
        {
            if(characteristic == ECharacteristic.Forward)
            {
                GetShortRoad(path);
            }
            else if (characteristic == ECharacteristic.BackWard)
            {
                GetBackRoad();
            }
            else if (characteristic == ECharacteristic.Hold)
            {
                GetHoldRoad();
            }
        }
    }

    // A* 알고리즘
    public void GetShortRoad(List<Path> path)
    {
        Vector2 playerPos = GameObject.FindWithTag("Player").transform.position / GameManager.gridSize;
        if (!AttackCanEnemy())
        {
            Vector2 unitPos = transform.position / GameManager.gridSize;
            Vector2 fixPos = new Vector2(0, 0);
            unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

            for (int count = 1; count < path.Count; count++)
            {
                Vector2 pathPoint = new Vector2(path[count].x, path[count].y);
                int moveCount;
                for (moveCount = 0; moveCount < moveablePoints.Length; ++moveCount)
                {
                    Vector2 currentMovePoint = unitPos + moveablePoints[moveCount];
                    if (pathPoint == currentMovePoint && currentMovePoint != playerPos)
                    {
                        fixPos = currentMovePoint;
                        break;
                    }
                }
                if (moveCount == moveablePoints.Length)
                {
                    break;
                }
            }

            transform.position = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
            state = EState.Attack;
            AttackPlayer();
        }
        else
        {
            state = EState.Attack;
            AttackPlayer();
        }
    }
    
    // 아직 미정
    public void GetBackRoad()
    {

    }
    // 아직 미정
    public void GetHoldRoad()
    {

    }
    //--------------- Move 종료 ---------------//

    //--------------- Die 시작 ---------------//
    // Attack 받았을 때 실행하는 함수
    public void DieEnemy()
    {
        if (state == EState.Dead)
        {
            Debug.Log("Enemy Dead : " + transform.name);
            Destroy(transform.gameObject);
        }
    }
    //--------------- Die 종료 ---------------//

    //--------------- Attack 시작 ---------------//
    // playerAttack 함수 Raycast사용
    public void AttackPlayer()
    {
        if (AttackCanEnemy() && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Token")); // enemy 위치에서 player까지 ray쏘기
            if(hit.transform.tag == "Player") // 닿은 ray가 Player 태그를 가지고 있다면
            {
                Debug.Log("Player Dead");
                Destroy(hit.transform.gameObject);
            }
        }
        else
        {
            state = EState.Idle;
        }
    }

    public bool AttackCanEnemy()
    {
        int attackCount;
        Vector2 currentAttackPoint;
        Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position / GameManager.gridSize;
        playerPos = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
        Vector2 enemyPos = transform.position / GameManager.gridSize;
        enemyPos = new Vector2Int(Mathf.FloorToInt(enemyPos.x), Mathf.FloorToInt(enemyPos.y));

        for (attackCount = 0; attackCount < attackablePoints.Length; ++attackCount)
        {
            currentAttackPoint = enemyPos + attackablePoints[attackCount];
            if(playerPos == currentAttackPoint)
            {
                Debug.Log("공격 가능");
                return true;
            }
        }
        return false;
    }
    //--------------- Attack 종료 ---------------//

    Sequence shakeSequence;
    public GameObject fadeObj;
    public bool useShake = true;

    public void Start()
    {
        Material tmpObj = Instantiate(fadeObj, transform.position, Quaternion.identity, transform).GetComponent<SpriteRenderer>().material;
        shakeSequence = DOTween.Sequence()
            .SetAutoKill(false)
            .OnStart(() => {
                tmpObj.color = new Color(1, 1, 1, 0);
        });
        //shakeSequence.Append(tmpObj.DOFade(0, 1).SetEase(Ease.Linear));
        shakeSequence.Append(tmpObj.DOFade(1f, 1).SetEase(Ease.Linear));
        shakeSequence.Append(tmpObj.DOFade(0, 1).SetEase(Ease.Linear));
        //shakeSequence.Append(tmpObj.DOFade(1f, 1).SetEase(Ease.Linear));
    }

    public void Update()
    {
        if (useShake && GameManager.Turn % 2 == 1 && moveCtrl[1] + moveCtrl[2] >= 10)
        {
            Debug.Log("Start shake");
            useShake = false;
            StartCoroutine(ShakeTokenAction());
        }
    }

    IEnumerator ShakeTokenAction()
    {
        shakeSequence.Restart();
        yield return new WaitForSeconds(shakeSequence.Duration());
        useShake = true;
    }
}
