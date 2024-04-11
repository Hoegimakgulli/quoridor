﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;
using static Player;

public class EnemyValues
{
    private Vector3 mPosition; // position
    private int mMoveCtrl; // moveCtrl

    public int hp; // 유닛 hp
    public int maxHp; // 유닛 최대 hp
    public int moveCtrl
    {
        get
        {
            return mMoveCtrl;
        }

        set
        {
            // Debug.Log($"SetPreMoveCtrl : {index}: {value}");
            value = Mathf.Max(value, 0);
            // Debug.Log($"SetMoveCtrl : {index}: {value}");
            Enemy correctEnemy = EnemyManager.GetEnemy(mPosition);
            correctEnemy.moveCtrl[1] = value;
            mMoveCtrl = value;
        }
    }
    public int maxMoveCtrl; // 유닛이 가질 수 있는 최대 행동력
    public int uniqueNum; // 어떤 유닛을 생성할지 정하는 번호
    public int index; // 생성 순서, EnemyBox 내 Index
    public Vector3 position // position이 변경될때 일어나는 것
    {
        get
        {
            return mPosition;
        }

        set
        {
            GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
            // enemyBox.transform.GetChild(spawnNum).position = value;
            // mPosition = value;
            foreach (Transform enemyPos in enemyBox.transform)
            {
                Debug.Log($"EV: {value}");
                if (enemyPos.position == mPosition) // 만약 
                {
                    enemyPos.position = value;
                    mPosition = value;
                }
            }
        }
    }

    public EnemyValues(int hp, int moveCtrl, int uniqueNum, int index, Vector3 position)
    {
        this.hp = hp;
        mMoveCtrl = moveCtrl;
        this.uniqueNum = uniqueNum;
        this.index = index;
        mPosition = position;
    }
}

public class GameManager : MonoBehaviour
{
    public enum EPlayerControlStatus { None, Move, Build, Attack, Ability };
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;

    public static int Turn = 1; // 현재 턴
    public const float gridSize = 1.3f; // 그리드의 크기

    public static Vector2Int playerGridPosition = new Vector2Int(0, -4); // 플레이어의 타일 위치

    public static List<Vector3> enemyPositions = new List<Vector3>();    // 모든 적들 위치 정보 저장      폐기처분 예정
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장   폐기처분 예정
    public static List<EnemyValues> enemyValueList = new List<EnemyValues>();

    public int[,] mapGraph = new int[81, 81]; //DFS용 맵 그래프

    public int currentStage;

    public GameObject player;
    public PlayerActionUI playerActionUI;
    public UiManager uiManager;

    public PlayerCharacters playerCharacters;

    public GameObject autoTrap;
    public bool canEnemyTurn = false;

    public List<AreaAbility> areaAbilityList = new List<AreaAbility>();
    public int tempTurn;

    void Awake()
    {
        Turn = 1; // 턴 초기화
        playerGridPosition = new Vector2Int(0, -4); // 플레이어 위치 초기화
        for (int i = 0; i < mapGraph.GetLength(0); i++) // 맵 그래프 초기화
        {
            int row = i / 9;
            int col = i % 9;
            if (row > 0)
            {
                mapGraph[i, (row - 1) * 9 + col] = 1;
            }
            if (row < 8)
            {
                mapGraph[i, (row + 1) * 9 + col] = 1;
            }
            if (col > 0)
            {
                mapGraph[i, row * 9 + (col - 1)] = 1;
            }
            if (col < 8)
            {
                mapGraph[i, row * 9 + (col + 1)] = 1;
            }
        }
        // DebugMap();
        player = Instantiate(playerCharacters.players[Random.Range(1, playerCharacters.players.Count)], ChangeCoord(playerGridPosition), Quaternion.identity);
        playerActionUI = player.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>();
        uiManager = GetComponent<UiManager>();
    }
    private void Start()
    {
        // 프레임 60fps로 설정
#if UNITY_ANDROID
        Application.targetFrameRate = 60;
#endif
    }
    public void Initialize()
    {
        enemyPositions.Clear();
        enemyObjects.Clear();
        playerGridPosition = new Vector2Int(0, -4);
        playerControlStatus = EPlayerControlStatus.None;
        Turn = 1; // 턴 초기화
        mapGraph = new int[81, 81];
        for (int i = 0; i < mapGraph.GetLength(0); i++) // 맵 그래프 초기화
        {
            int row = i / 9;
            int col = i % 9;
            if (row > 0)
            {
                mapGraph[i, (row - 1) * 9 + col] = 1;
            }
            if (row < 8)
            {
                mapGraph[i, (row + 1) * 9 + col] = 1;
            }
            if (col > 0)
            {
                mapGraph[i, row * 9 + (col - 1)] = 1;
            }
            if (col < 8)
            {
                mapGraph[i, row * 9 + (col + 1)] = 1;
            }
        }
        GetComponent<CrashHandler>().Init();
    }
    void Update()
    {
        if (playerControlStatus == EPlayerControlStatus.None)
        {
            if (player.GetComponent<Player>().touchState == ETouchState.Began)
            {
                Vector2 clickPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                RaycastHit2D hit = Physics2D.Raycast(player.GetComponent<Player>().touchPosition, Vector3.forward, 15f, LayerMask.GetMask("Token"));

                if (hit.collider != null && hit.collider.gameObject.tag == "Enemy")
                {
                    hit.collider.gameObject.GetComponent<Enemy>().EnemyActionInfo();
                }
                else
                {
                    uiManager.PassiveEnemyInfoUI();
                }
            }
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            EnemyManager.GetEnemyObject(0).transform.position += new Vector3(0, -1, 0);
            Debug.Log(enemyValueList[0].position);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            for (int i = 0; i < enemyValueList.Count; i++)
            {
                enemyValueList[i].position = ChangeCoord(new Vector2Int(0, 4 - i));
            }
        }

        if (Turn % 2 == 0 && Input.GetKey(KeyCode.Space)) //[디버그용] space 키를 통해 적턴 넘기기
        {
            Turn++;
        }

        if (Turn % 2 != Player.playerOrder)
        {
            if (tempTurn != Turn)
            {
                canEnemyTurn = false;
                tempTurn = Turn;
            }
            canEnemyTurn = areaAbilityList.All(areaAbility => areaAbility.canDone);
        }

        if (Input.GetKeyDown(KeyCode.D)) DebugMap();
    }
    //DFS 알고리즘을 이용한 벽에 갇혀있는지 체크
    public bool CheckStuck()
    {
        bool[] visited = new bool[81];
        int playerGraphPosition = (int)((playerGridPosition.y + 4) * 9 + playerGridPosition.x + 4);

        void DFS(int now)
        {
            visited[now] = true;
            for (int next = 0; next < 81; next++)
            {
                if (mapGraph[now, next] == 0)
                    continue;
                if (visited[next])
                    continue;
                DFS(next);
            }
        }
        DFS(playerGraphPosition);
        // Debug.Log(visited[enemyGraphPosition]);
        foreach (Vector3 enemyPosition in enemyPositions)
        {
            int enemyGraphPosition = (int)((enemyPosition.y + 4) * 9 + enemyPosition.x + 4);
            if (!visited[enemyGraphPosition]) return false;
        }
        return true;
    }
    //[디버그용] 맵그래프 출력
    public void DebugMap()
    {
        string log = "";
        for (int i = 0; i < mapGraph.GetLength(0); i++)
        {
            for (int row = 8; row >= 0; row--)
            {
                string rowInfo = "";
                for (int col = 0; col < 9; col++)
                {
                    rowInfo = rowInfo + " " + mapGraph[i, row * 9 + col].ToString();
                    if (mapGraph[i, row * 9 + col] == 1)
                    {
                        Vector3 start = new Vector3((i % 9) - 4, (i / 9) - 4, 0) * gridSize;
                        Vector3 end = new Vector3(col - 4, row - 4, 0) * gridSize;
                        Vector3 dir = end - start;
                        Vector3 interval = (i % 2 == 0) ? Vector3.zero : new Vector3(0.1f, 0.1f, 0);
                        Debug.DrawRay(start + interval, dir.normalized, Color.green, 1f);
                    }
                }
                log += rowInfo + '\n';
            }
            log += '\n';
        }
        Debug.Log(log);
    }

    //적턴이 끝나고 플레이어 턴이 시작될 때 실행될 것들
    public void PlayerTurnSet()
    {
        playerActionUI.ActiveUI();
        uiManager.turnEndButton.SetActive(true);
    }
    public static Vector3 ChangeCoord(Vector2Int originVector) { return ((Vector3)(Vector2)originVector * gridSize); }
    public static Vector2Int ChangeCoord(Vector3 originVector) { return new Vector2Int(Mathf.RoundToInt((originVector / gridSize).x), Mathf.RoundToInt((originVector / gridSize).y)); }
}
