﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HM.Containers;

[System.Serializable]
public class Path
{
    // 중앙 좌표상 (0, 0) 시작으로 x, y 좌표
    // G = 시작으로부터 이동한 거리, H = 가로, 세로로 벽을 무시하고 Player까지 이동한 거리
    public int x, y, G, H;

    public Path(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public Path ParentNode;

    // F = G, H 총 합산값
    public int F
    {
        get
        {
            return G + H;
        }
    }
}

public class EnemyManager : MonoBehaviour
{
    public List<GameObject> enemyPrefabs; // 모든 유닛들 통합으로 관리

    public GameObject enemyBoxPrefab; // 경고 표기 담아두는 박스
    static GameObject EnemyBox;
    public GameObject enemyUiCanvas;

    private bool enemyTurnAnchor = true;

    private void Awake()
    {
        SetEnemyBox();
        Debug.Log("ui Spawned");
        gameManager = GameManager.Instance;
        GameManager.enemyValueList.Clear();
        GameManager.enemyObjects.Clear(); // 적 위치 및 객체 정보 초기화
        GameManager.enemyPositions.Clear();
    }

    private void Start()
    {
    }

    void Update()
    {
        // 적 턴일때 (이동 및 공격확인)
        if (GameManager.Turn % 2 == 0 && enemyTurnAnchor && gameManager.canEnemyTurn)
        {
            enemyTurnAnchor = false;

            //EnemyMoveStart();
            StartCoroutine(StartEnemyTurn());
        }
    }

    public List<Path> FinalPathList;
    public Vector2Int bottomLeft, topRight, startPos, targetPos;
    public Vector2Int topLeft, bottomRight;
    public bool allowDiagonal, dontCrossCorner;

    int sizeX, sizeY;
    Path[,] PathArray;
    Path StartNode, TargetNode, CurNode;
    List<Path> OpenList, ClosedList;

    // A* 알고리즘
    public void PathFinding(GameObject startObj, GameObject endObj)
    {
        if (startObj.name.Contains("EnemyShieldSoldier")) // 만약 이동하는 객체가 방패병일 경우 벽처리로 해놨던 방패를 비활성화 후 이동 실시
        {
            int currentShieldPos = Mathf.FloorToInt(startObj.transform.position.x / GameManager.gridSize) + 4 + ((Mathf.FloorToInt(startObj.transform.position.y / GameManager.gridSize) + 4) * 9); // mapgraph 형식으로 다듬기
            if (currentShieldPos + 9 < 81 && startObj.GetComponent<Enemy>().ShieldTrue == true) // 방패가 위쪽 벽과 닿지 않았을 때만 실행
            {
                gameManager.wallData.mapGraph[currentShieldPos, currentShieldPos + 9] = 1; // 초기화 1
                gameManager.wallData.mapGraph[currentShieldPos + 9, currentShieldPos] = 1; // 초기화 2
                startObj.GetComponent<Enemy>().ShieldTrue = false;
            }
        }

        sizeX = topRight.x - bottomLeft.x + 1;
        sizeY = topRight.y - bottomLeft.y + 1;
        PathArray = new Path[sizeX, sizeY];

        for (int i = 0; i < sizeX; i++)
        {
            for (int j = 0; j < sizeY; j++)
            {
                PathArray[i, j] = new Path(i + bottomLeft.x, j + bottomLeft.y);
            }
        }

        // startPos, endPos 기존 position을 girdSize로 나눠서 정수화 시켜준 다음 좌표계 (0, 0)을 왼쪽 아래 가장자리로 바꿔줌
        Vector2 trimPos;
        trimPos = startObj.transform.position / GameManager.gridSize;
        startPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);
        trimPos = endObj.transform.position / GameManager.gridSize;
        targetPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);

        StartNode = PathArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        TargetNode = PathArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
        OpenList = new List<Path>() { StartNode };
        ClosedList = new List<Path>();
        FinalPathList = new List<Path>();

        while (OpenList.Count > 0)
        {
            // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
            {
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];
            }

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);


            // 마지막
            if (CurNode == TargetNode)
            {
                Path TargetCurNode = TargetNode.ParentNode;
                while (TargetCurNode != StartNode)
                {
                    FinalPathList.Add(TargetCurNode);
                    TargetCurNode = TargetCurNode.ParentNode;
                }
                FinalPathList.Add(StartNode);
                FinalPathList.Reverse();

                //for (int i = 0; i < FinalPathList.Count; i++) print(i + "번째는 " + FinalPathList[i].x + ", " + FinalPathList[i].y);
                return;
            }

            if (allowDiagonal)
            {
                // 다음에 들어갈 좌표 전달
                OpenListAdd(CurNode.x + 1, CurNode.y + 1); // 오른쪽 위
                OpenListAdd(CurNode.x - 1, CurNode.y + 1); // 왼쪽 위
                OpenListAdd(CurNode.x - 1, CurNode.y - 1); // 왼쪽 아래
                OpenListAdd(CurNode.x + 1, CurNode.y - 1); // 오른쪽 아래
            }

            // ↑ → ↓ ←
            OpenListAdd(CurNode.x, CurNode.y + 1); // 위
            OpenListAdd(CurNode.x + 1, CurNode.y); // 오른쪽
            OpenListAdd(CurNode.x, CurNode.y - 1); // 아래
            OpenListAdd(CurNode.x - 1, CurNode.y); // 왼쪽
        }

        if (FinalPathList.Count == 0)
        {
            PathFinding(startObj, blockEmemyObj);
        }
    }

    public GameManager gameManager;
    void OpenListAdd(int checkX, int checkY)
    {
        // graph 상 (0,0) == 0과 같음
        int startGraphPosition = (int)(CurNode.y * 9 + CurNode.x);
        int endGraphPosition = (int)(checkY * 9 + checkX);
        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1 && !ClosedList.Contains(PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
        {
            // start 지점으로 부터 end 지점 사이에 벽이 있는지 확인
            if (gameManager.wallData.mapGraph[startGraphPosition, endGraphPosition] == 0) return;
            if (CheckEnemyPos(new Vector2((checkX - 4) * GameManager.gridSize, (checkY - 4) * GameManager.gridSize))) return;
            // 대각선 허용시, 벽 사이로 통과 안됨
            if (allowDiagonal)
            {
                if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
                {
                    if (checkY - CurNode.y == 1)
                    {
                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + 9] == 0) return; // 아래에서 위로 올라가는 경우
                    }
                    else if (checkY - CurNode.y == -1)
                    {
                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition - 9] == 0) return; // 위에서 아래로 내려가는 경우
                    }
                }
            }

            // 코너를 가로질러 가지 않을시, 이동 중에 수직수평 장애물이 있으면 안됨
            if (dontCrossCorner)
            {
                //if (PathArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall || PathArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;
            }

            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
            Path NeighborNode = PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);

            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
            {
                NeighborNode.G = MoveCost;
                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                NeighborNode.ParentNode = CurNode;

                OpenList.Add(NeighborNode);
            }
        }
    }

    private GameObject blockEmemyObj;
    private bool CheckEnemyPos(Vector2 currentPos)
    {
        foreach (Transform enemy in EnemyBox.transform)
        {
            Vector2 enemyPos = enemy.position;
            if (currentPos == enemyPos && currentPos != new Vector2((TargetNode.x - 4) * GameManager.gridSize, (TargetNode.y - 4) * GameManager.gridSize))
            {
                blockEmemyObj = enemy.gameObject;
                return true;
            }
        }
        return false;
    }

    static public bool turnCheck = false;

    IEnumerator StartEnemyTurn()
    {
        //yield return new WaitForSeconds(0.01f);
        //MoveCtrlUpdate();

        yield return StartCoroutine(MoveCtrlUpdateCoroutine());
        UiManager uiManager = GetComponent<UiManager>();
        //while (uiManager.)
        //enemyTurnAnchor = true;
    }

    //적 턴이 전부 끝났을 때 호출됨.
    public void EnemyTurnAnchorTrue()
    {
        enemyTurnAnchor = true;
        GameManager.Turn++;
        gameManager.PlayerTurnSet(); //플레이어 턴이 시작됨을 알림
    }

    // 하나하나 적 오브젝트들이 순차적으로 움직이면서 경로를 탐색하는 함수
    //void EnemyMoveStart()
    //{
    //    GameObject moveEnemyObj = GetEnemyObject(GameManager.enemyValueList[(GameManager.Turn / 2) % GameManager.enemyValueList.Count].position);
    //    GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
    //    List<Path> shortPath = new List<Path>();

    //    bool isPlayer = GameObject.FindWithTag("PlayerDummy") ? false : true;
    //    PathFinding(moveEnemyObj, GameObject.FindWithTag("PlayerDummy") ? GameObject.FindWithTag("PlayerDummy") : players[0]);
    //    shortPath = FinalPathList;
    //    for (int count = 0; count < players.Length; count++)
    //    {
    //        PathFinding(moveEnemyObj, players[count]);

    //        // 모든 player랑 비교해서 가장 가까운 player 위치로 이동함
    //        if (shortPath.Count > FinalPathList.Count)
    //        {
    //            isPlayer = true;
    //            shortPath = FinalPathList;
    //        }
    //    }

    //    Enemy currentEnemy = GetEnemy(moveEnemyObj.transform.position);
    //    currentEnemy.state = Enemy.EState.Move;
    //    currentEnemy.EnemyMove(shortPath, isPlayer);
    //    currentEnemy.UpdateTurn();
    //    EnemyTurnAnchorTrue();
    //}

    //적 움직임 상태창 애니메이션에 맞춰 순차적으로 움직이도록 수정 (이규빈)
    IEnumerator MoveCtrlUpdateCoroutine()
    {
        UiManager uiManager = GetComponent<UiManager>();
        List<int> originSortingList = new List<int>(); //적들이 움직이기 전에 행동력 순서로 분류되어있던 리스트
        for (int i = 0; i < uiManager.sortingList.Count; i++)
        {
            originSortingList.Add(uiManager.sortingList[i]);
        }

        Enemy currentEnemyState;
        int count;
        int originMoveCtrl;  //원래 행동력.
        for (count = 0; count < GameManager.enemyValueList.Count; count++)
        {
            currentEnemyState = GetEnemy(GameManager.enemyValueList[originSortingList[count]].position);
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl;

            if (currentEnemyState.debuffs[Enemy.EDebuff.CantMove] == 0)
            {
                GameManager.enemyValueList[originSortingList[count]].moveCtrl += currentEnemyState.moveCtrl[2]; // 랜덤으로 들어오는 무작위 행동력 0 ~ 적 행동력 회복 최대치
            }

            uiManager.SortEnemyStates(); //행동력에 따라 적 상태창 순서 정렬
            yield return StartCoroutine(uiManager.CountMovectrlAnim(originSortingList[count], originMoveCtrl, GameManager.enemyValueList[originSortingList[count]].moveCtrl, false)); //원래 행동력에서 바뀐 행동력까지 숫자가 바뀌는 애니메이션
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl; //여기서부터 originMoveCtrl은 바뀐 후의 행동력
            if (count == GameManager.enemyValueList.Count - 1)
            {
                if (currentEnemyState.moveCtrl[0] > GameManager.enemyValueList[originSortingList[count]].moveCtrl)
                {
                    EnemyTurnAnchorTrue();
                }
            }
            if (currentEnemyState.moveCtrl[0] <= GameManager.enemyValueList[originSortingList[count]].moveCtrl)
            {
                bool isPlayer = true; // true == player, false == dump
                GameObject currenEnemy = GetEnemyObject(GameManager.enemyValueList[originSortingList[count]].position);
                ///////////////////요 윗부분 originalSortingList랑 그 안에 어쩌구 뭐 있었는데 테스트하느라 지웠다함!!! 문제생기면 여기일듯??ㅁㅁㅇㅁㄴㄻㄴㅇ훠ㅑㅁㅈ둬모ㅓ몬ㅇ 

                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                List<Path> shortPath = new List<Path>();

                isPlayer = GameObject.FindWithTag("PlayerDummy") ? false : true;
                PathFinding(currenEnemy, GameObject.FindWithTag("PlayerDummy") ? GameObject.FindWithTag("PlayerDummy") : players[0]);
                shortPath = FinalPathList;
                for (int sortCount = 0; sortCount < players.Length; sortCount++)
                {
                    PathFinding(currenEnemy, players[sortCount]);

                    // 모든 player랑 비교해서 가장 가까운 player 위치로 이동함
                    if (shortPath.Count > FinalPathList.Count)
                    {
                        isPlayer = true;
                        shortPath = FinalPathList;
                    }
                }

                currentEnemyState.state = Enemy.EState.Move;
                currentEnemyState.EnemyMove(shortPath, isPlayer);

                GameManager.enemyValueList[originSortingList[count]].moveCtrl = -1; //상태창 순서를 행동력 순으로 정렬했을 때, 방금 이동한 적의 순서가 가장 아래로 내려오도록 행동력을 마이너스로 수정.
                uiManager.SortEnemyStates(); //방금 이동한 적의 상태창이 아래로 내려오도록 리스트를 재정렬
                GameManager.enemyValueList[originSortingList[count]].moveCtrl = originMoveCtrl - currentEnemyState.moveCtrl[0]; //적의 행동력 감소
                currentEnemyState.moveCtrl[1] = GameManager.enemyValueList[originSortingList[count]].moveCtrl; // 현재 이동한 유닛 행동력 변경
                yield return StartCoroutine(uiManager.ReloadState(originSortingList[count], GameManager.enemyValueList[originSortingList[count]].moveCtrl, count));
            }
            currentEnemyState.UpdateTurn(); // 매턴마다 시행 - 동현
        }
    }

    public void SetEnemyBox()
    {
        if (EnemyBox != null) Destroy(EnemyBox);
        EnemyBox = Instantiate(enemyBoxPrefab);
        // Debug.Log(EnemyBox.name);
    }
    public static GameObject GetEnemyObject(Vector3 position, bool shouldLog = true)
    {
        foreach (Transform child in EnemyBox.transform)
        {
            // Debug.Log(child.position);
            if (child.position == position)
            {
                return child.gameObject;
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 Enemy 오브젝트를 찾지 못했습니다.");
        return null;
    }
    public static EnemyValues GetEnemyValues(Vector3 position, bool shouldLog = true)
    {
        foreach (EnemyValues child in GameManager.enemyValueList)
        {
            if (child.position == position)
            {
                return child;
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 EnemyValues도 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }
    public static Enemy GetEnemy(Vector3 position, bool shouldLog = true)
    {
        foreach (Transform child in EnemyBox.transform)
        {
            // Debug.Log(child.position);
            if (child.position == position)
            {
                return child.GetComponent<Enemy>();
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 Enemy 스크립트를 찾지 못했습니다.");
        return null;
    }
    public static GameObject GetEnemyObject(out GameObject enemyObject, Vector3 position, bool shouldLog = true)
    {
        return enemyObject = GetEnemyObject(position, shouldLog);
    }
    public static EnemyValues GetEnemyValues(out EnemyValues enemyValues, Vector3 position, bool shouldLog = true)
    {
        return enemyValues = GetEnemyValues(position, shouldLog);
    }
    public static Enemy GetEnemy(out Enemy enemy, Vector3 position, bool shouldLog = true)
    {
        return enemy = GetEnemy(position, shouldLog);
    }
    public static GameObject GetEnemyObject(int index)
    {
        return EnemyBox.transform.GetChild(index).gameObject;
    }
    public static EnemyValues GetEnemyValues(int index)
    {
        return GameManager.enemyValueList[index];
    }
    public static Enemy GetEnemy(int index)
    {
        return EnemyBox.transform.GetChild(index).GetComponent<Enemy>();
    }
}
