using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions  = new List<Vector3>();    // 모든 적들 위치 정보 저장
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장

    //-------------- Enemy Values --------------//
    public int cost;                                  // 소환 시 필요한 비용
    public int hp;                                    // 받아야하는 총 체력
    public int[] moveCtrl = new int[2];               // 0 = 요구 행동력, 1 = 현재 채워져 있는 행동력
    public int[] unitMoveVector = new int[8];         // 0 = 위, 1 = 오른쪽, 2 = 아래, 3 = 왼쪽, 4 = 왼쪽위, 5 = 오른쪽위, 6 = 오른쪽아래, 7 = 왼쪽아래
    //public int[] unitMoveVector = new int[2];         // 0 = + , 1 = x
    public enum EState { Idle, Move, Attack, Dead };
    public enum ECharacteristic { Forward, BackWard, Hold }; // 0 - 전진해 player를 공격, 1 - 뒤 포지션을 잡으면서 플레이어 공격, 2 - 자기 구역을 사수하면서 플레이어를 공격
    //------------------------------------------//

    public EState state = EState.Idle;
    public ECharacteristic characteristic = ECharacteristic.Forward;

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
        int anchor = 0;
        Vector2 unitPos = transform.position / GameManager.gridSize;
        Vector2 fixPos = new Vector2(0, 0);
        unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

        for(int count = 1; count < path.Count; count++)
        {
            Vector2Int movePath = new Vector2Int(Mathf.FloorToInt(path[count].x - unitPos.x), Mathf.FloorToInt(path[count].y - unitPos.y));
            if(movePath.x == 0 && (anchor == 0 || anchor == 1)) // 위 아래
            {
                if(movePath.y < 0) // 아래
                {
                    if(Mathf.Abs(movePath.y) <= unitMoveVector[2])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else // 위
                {
                    if (Mathf.Abs(movePath.y) <= unitMoveVector[0])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            else if(movePath.y == 0 && (anchor == 0 || anchor == 2)) // 좌 우
            {
                if(movePath.x < 0) // 왼쪽
                {
                    if (Mathf.Abs(movePath.x) <= unitMoveVector[3])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 2;
                    }
                    else
                    {
                        break;
                    }
                }
                
                else // 오른쪽
                {
                    if (Mathf.Abs(movePath.y) <= unitMoveVector[1])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 2;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            else if(anchor == 0 || anchor == 3) // 대각
            {
                Debug.Log("대각 이동");
            }

            else // 방향이 바뀌거나 범위 밖일경우
            {
                break;
            }
        }

        transform.position = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
        if (canAttack == true)
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
    private bool canAttack = false;
    // playerAttack 함수 Raycast사용
    public void AttackPlayer()
    {
        if (canAttack && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.Find("Player").transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Player")); // enemy 위치에서 player까지 ray쏘기
            if(hit != false) // 벽과 부딪치치 않았다면
            {
                Debug.Log("Player Dead");
                Destroy(hit.transform.gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.name == "Player")
        {
            canAttack = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.name == "Player")
        {
            canAttack = false;
        }
    }
    //--------------- Attack 종료 ---------------//
}
