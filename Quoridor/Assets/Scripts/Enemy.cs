﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using System;
using DG.Tweening.Core.Easing;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public UiManager uiManager;
    // enemyValues
    //-------------- Enemy Values --------------//
    public int hp;                                    // 받아야하는 총 체력
    public int maxHp;
    public int[] moveCtrl = new int[3];               // 0 = 요구 행동력, 1 = 현재 채워져 있는 행동력, 2 = 랜덤 행동력 충전 최대치
    public Vector2Int[] moveablePoints;
    public Vector2Int[] attackablePoints;
    public enum EState { Idle, Move, Attack };
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
            if (characteristic == ECharacteristic.Forward)
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

    public GameManager gameManager;

    // A* 알고리즘
    public void GetShortRoad(List<Path> path)
    {
        Vector2 playerPos = GameObject.FindWithTag("Player").transform.position / GameManager.gridSize;
        if (!AttackCanEnemy())
        {
            Vector2 unitPos = transform.position / GameManager.gridSize;
            Vector2 fixPos = new Vector2(0, 0);
            unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

            int count;
            for (count = 1; count < path.Count; count++)
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

            if (count != 1)
            {
                for (int posCount = 0; count < GameManager.enemyPositions.Count; count++)
                {
                    if (GameManager.enemyPositions[posCount] == transform.position)
                    {
                        GameManager.enemyPositions[posCount] = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
                    }
                }
                transform.position = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
            }

            gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

            if (transform.name.Contains("EnemyShieldSoldier")) // 이동 후 다시 벽으로 처리 실시
            {
                int currentShieldPos = (int)(fixPos.x + (fixPos.y * 9)); // mapgraph 형식으로 다듬기
                if (currentShieldPos + 9 < 81) // 방패가 위쪽 벽과 닿지 않았을 때만 실행
                {
                    gameManager.mapGraph[currentShieldPos, currentShieldPos + 9] = 0; // 초기화 1
                    gameManager.mapGraph[currentShieldPos + 9, currentShieldPos] = 0; // 초기화 2
                }
            }

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
    public bool AttackedEnemy(int playerAtk)
    {
        int originHP = hp;
        hp -= playerAtk;
        for(int i = 0; i < GameManager.enemyObjects.Count; i++)
        {
            if (GameManager.enemyObjects[i] == gameObject)
            {
                uiManager.StartCountEnemyHpAnim(i, originHP, hp);
            }
        }
        if(hp <= 0)
        {
            DieEnemy();
            return true;
        }
        return false;
    }
    
    public void DieEnemy()
    {
        foreach (GameObject child in GameManager.enemyObjects)
        {
            if (child == gameObject)
            {
                GameManager.enemyPositions.Remove(child.transform.position);
                GameManager.enemyObjects.Remove(child);
                break;
            }
        }
        Debug.Log("Enemy Dead : " + transform.name);
        Destroy(transform.gameObject);
        EnemyStage.totalEnemyCount--;
    }
    //--------------- Die 종료 ---------------//

    //--------------- Attack 시작 ---------------//
    // playerAttack 함수 Raycast사용
    public void AttackPlayer()
    {
        if (AttackCanEnemy() && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
            RaycastHit2D hitWall = Physics2D.RaycastAll(transform.position, playerPos - (Vector2)transform.position, GameManager.gridSize * Math.Abs((playerPos - (Vector2)transform.position).magnitude), LayerMask.GetMask("Wall")).OrderBy(h => h.distance).Where(h => h.transform.tag == "Wall").FirstOrDefault();
            RaycastHit2D hit = Physics2D.RaycastAll(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Token")).OrderBy(h => h.distance).Where(h => h.transform.tag == "Player").FirstOrDefault(); ; // enemy 위치에서 player까지 ray쏘기
            
            if(!hitWall)
            {
                if (hit.transform.tag == "Player") // 닿은 ray가 Player 태그를 가지고 있다면
                {
                    Debug.Log("Player Dead");
                    
                    Destroy(hit.transform.gameObject);
                }
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


        Vector2 playerPosT = GameObject.FindGameObjectWithTag("Player").transform.position;
        RaycastHit2D hitWall = Physics2D.Raycast(transform.position, playerPosT - (Vector2)transform.position, GameManager.gridSize * Math.Abs((playerPosT - (Vector2)transform.position).magnitude), LayerMask.GetMask("Wall"));

        if (!hitWall)
        {
            for (attackCount = 0; attackCount < attackablePoints.Length; ++attackCount)
            {
                currentAttackPoint = enemyPos + attackablePoints[attackCount];
                if (playerPos == currentAttackPoint)
                {
                    Debug.Log("공격 가능");
                    return true;
                }
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
        uiManager = GameObject.Find("GameManager").GetComponent<UiManager>();
        Material tmpObj = Instantiate(fadeObj, transform.position, Quaternion.identity, transform).GetComponent<SpriteRenderer>().material;
        shakeSequence = DOTween.Sequence()
            .SetAutoKill(false)
            .OnStart(() =>
            {
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
