using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions  = new List<Vector3>(); // 모든 적들 위치 정보 저장
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장

    //-------------- Enemy Values --------------//
    public int cost;
    public int hp;
    public int[] moveCtrl = new int[2];
    public int characteristic;
    enum EState { Idle, Move, Attack, Dead };
    enum ECharacteristic { Forward, BackWard, Hold }; // 0 - 전진해 player를 공격, 1 - 뒤 포지션을 잡으면서 플레이어 공격, 2 - 자기 구역을 사수하면서 플레이어를 공격
    //------------------------------------------//

    EState state = EState.Idle;

    // A* 알고리즘
    public void GetShortRoad()
    {

    }
    // 아직 미정
    public void GetBackRoad()
    {

    }
    // 아직 미정
    public void GetHoldRoad()
    {

    }

    private bool canAttack = false;
    // playerAttack 함수 Raycast사용
    public void AttackPlayer()
    {
        if (canAttack && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.Find("Player").transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Wall")); // enemy 위치에서 player까지 ray쏘기
            if(hit == false) // 벽과 부딪치치 않았다면
            {
                Debug.Log("Player Dead");
                Destroy(hit.transform.gameObject);
            }
        }
    }

    // Attack 받았을 때 실행하는 함수
    public void DieEnemy()
    {
        if (state == EState.Dead)
        {
            Debug.Log("Enemy Dead : " + transform.name);
            Destroy(transform.gameObject);
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
}
