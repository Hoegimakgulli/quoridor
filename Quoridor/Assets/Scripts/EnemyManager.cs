using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

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

    // public static int gameManager.currentStage = 0;
    public GameObject enemyBox; // 경고 표기 담아두는 박스
    public GameObject enemyUiCanvas;
    public const float gridSize = 1.3f; // 그리드의 크기

    private bool enemyTurnAnchor = true;
    //private bool enemyWarningSignAnchor = true;

    //public GameObject EnemyStatePanel;

    private void Awake()
    {
        Instantiate(enemyBox);
        //GameObject enemyUi = Instantiate(enemyUiCanvas);
        //Instantiate(EnemyStatePanel, enemyUi.transform);
        Debug.Log("ui Spawned");
        gameManager = transform.gameObject.GetComponent<GameManager>();
        GameManager.enemyValueList.Clear();
        GameManager.enemyObjects.Clear(); // 적 위치 및 객체 정보 초기화
        GameManager.enemyPositions.Clear();
    }

    private void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CheckEnemyInfo();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            testMove();
        }

        // 적 턴일때 (이동 및 공격확인)
        if (GameManager.Turn % 2 == 0 && enemyTurnAnchor)
        {
            //enemyWarningSignAnchor = true;
            enemyTurnAnchor = false;
            // 오브젝트 카운트 초기화
            /*      
            for (int count = 0; count < GameManager.enemyObjects.Count; count++)
            {
                GameManager.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text = "";
            }*/

            StartCoroutine(StartEnemyTurn());
        }

        // 플레이어 턴일때 (적 움직임 경고)
        /*
        if(GameManager.Turn % 2 == 1 && enemyWarningSignAnchor)
        {
            enemyWarningSignAnchor = false;
           WarningEnemy();
        }
        */
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
                gameManager.mapGraph[currentShieldPos, currentShieldPos + 9] = 1; // 초기화 1
                gameManager.mapGraph[currentShieldPos + 9, currentShieldPos] = 1; // 초기화 2
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
            if (gameManager.mapGraph[startGraphPosition, endGraphPosition] == 0) return;
            if (CheckEnemyPos(new Vector2((checkX - 4) * GameManager.gridSize, (checkY - 4) * GameManager.gridSize))) return;
            // 대각선 허용시, 벽 사이로 통과 안됨
            if (allowDiagonal)
            {
                if (gameManager.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
                {
                    if (checkY - CurNode.y == 1)
                    {
                        if (gameManager.mapGraph[startGraphPosition, startGraphPosition + 9] == 0) return; // 아래에서 위로 올라가는 경우
                    }
                    else if (checkY - CurNode.y == -1)
                    {
                        if (gameManager.mapGraph[startGraphPosition, startGraphPosition - 9] == 0) return; // 위에서 아래로 내려가는 경우
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
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        foreach (Transform enemy in enemyBox.transform)
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

    /*
    void OnDrawGizmos()
    {
        if (FinalPathList.Count != 0) for (int i = 0; i < FinalPathList.Count - 1; i++)
                Gizmos.DrawLine(new Vector2(FinalPathList[i].x, FinalPathList[i].y), new Vector2(FinalPathList[i + 1].x, FinalPathList[i + 1].y));
    }
    */
    static public bool turnCheck = false;
    void MoveCtrlUpdate()
    {
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < GameManager.enemyObjects.Count; count++)
        {
            currentEnemyState = GameManager.enemyObjects[count].GetComponent<Enemy>();

            //Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "의 행동력은 → " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += currentEnemyState.moveCtrl[2]; // 랜덤으로 들어오는 무작위 행동력 0 ~ 적 행동력 회복 최대치
            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "의 변동 행동력은 → " + currentEnemyState.moveCtrl[1]);

            //currentEnemyState.moveCtrl[1] += 10; // test 용 추가

            if (currentEnemyState.moveCtrl[0] <= currentEnemyState.moveCtrl[1])
            {
                GameObject currenEnemy = GameManager.enemyObjects[count];
                GameObject player = GameObject.FindWithTag("Player");
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                currentEnemyState.moveCtrl[1] = 0; // 현재 행동력 초기화
                /*
                foreach(Transform child in GameObject.FindWithTag("WarningBox").transform)
                {
                    if(child.GetComponent<Text>().text == GameManager.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text)
                    {
                        child
                    }
                }
                GameManager.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text = "";
                */
                if (!turnCheck)
                {
                    turnCheck = true;
                    GameManager.Turn++;
                }
            }

            else
            {
                if (!turnCheck)
                {
                    turnCheck = true;
                    GameManager.Turn++;

                }
            }
        }
    }

    void testMove()
    {
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < GameManager.enemyObjects.Count; count++)
        {
            currentEnemyState = GameManager.enemyObjects[count].GetComponent<Enemy>();

            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "의 행동력은 → " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += currentEnemyState.moveCtrl[2]; // 회복하는 적 행동력
            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "의 변동 행동력은 → " + currentEnemyState.moveCtrl[1]);
        }
    }

    /*
    public GameObject enemyWarningPrefab;
    public void WarningEnemy()
    {
        int count;
        int index = 1;
        GameObject player = GameObject.FindWithTag("Player");
        Vector2 playerPos = player.transform.position / GameManager.gridSize;

        for (count = 0; count < GameManager.enemyObjects.Count; count++)
        {
            Enemy currentEnemy = GameManager.enemyObjects[count].GetComponent<Enemy>();
            GameObject currentEnemyObj = GameManager.enemyObjects[count];
            if (currentEnemy.moveCtrl[1] + currentEnemy.moveCtrl[2] >= 10)
            {
                currentEnemyObj.transform.GetChild(0).GetComponent<TextMesh>().text = "" + index;
                PathFinding(currentEnemyObj, player);

                Vector2 unitPos = currentEnemyObj.transform.position / GameManager.gridSize;
                Vector2 fixPos = new Vector2(0, 0);
                unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

                for (int pathCount = 1; pathCount < FinalPathList.Count; pathCount++)
                {
                    Vector2 pathPoint = new Vector2(FinalPathList[pathCount].x, FinalPathList[pathCount].y);
                    int moveCount;
                    for (moveCount = 0; moveCount < currentEnemy.moveablePoints.Length; ++moveCount)
                    {
                        Vector2 currentMovePoint = unitPos + currentEnemy.moveablePoints[moveCount];
                        if (pathPoint == currentMovePoint && currentMovePoint != playerPos)
                        {
                            fixPos = currentMovePoint;
                            break;
                        }
                    }
                    if (moveCount == currentEnemy.moveablePoints.Length)
                    {
                        break;
                    }
                }
             
                GameObject sign = Instantiate(enemyWarningPrefab, new Vector2((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize), Quaternion.identity, GameObject.FindWithTag("WarningBox").transform);
                sign.transform.GetChild(0).GetComponent<TextMesh>().text = "" + index;
                index++;
            }
        }
    }*/

    void CheckEnemyInfo()
    {
        for (int count = 0; count < GameManager.enemyPositions.Count; count++)
        {
            Debug.Log((count + 1) + "iter - enemyPos : " + GameManager.enemyPositions[count] + " - enemyObj : " + GameManager.enemyObjects[count]);
        }
    }

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
            currentEnemyState = FindValuesObj(GameManager.enemyValueList[originSortingList[count]].position).GetComponent<Enemy>();
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl;

            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[sortingList[count]] + "의 행동력은 → " + currentEnemyState.moveCtrl[1]);
            GameManager.enemyValueList[originSortingList[count]].moveCtrl += currentEnemyState.moveCtrl[2]; // 랜덤으로 들어오는 무작위 행동력 0 ~ 적 행동력 회복 최대치
            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[sortingList[count]] + "의 변동 행동력은 → " + currentEnemyState.moveCtrl[1]);

            uiManager.SortEnemyStates(); //행동력에 따라 적 상태창 순서 정렬
            yield return StartCoroutine(uiManager.CountMovectrlAnim(originSortingList[count], originMoveCtrl, GameManager.enemyValueList[originSortingList[count]].moveCtrl, false)); //원래 행동력에서 바뀐 행동력까지 숫자가 바뀌는 애니메이션
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl; //여기서부터 originMoveCtrl은 바뀐 후의 행동력
            if(count == GameManager.enemyValueList.Count-1)
            {
                if (currentEnemyState.moveCtrl[0] > GameManager.enemyValueList[originSortingList[count]].moveCtrl)
                {
                    EnemyTurnAnchorTrue();
                }
            }
            if (currentEnemyState.moveCtrl[0] <= GameManager.enemyValueList[originSortingList[count]].moveCtrl)
            {
                //GameObject currenEnemy = FindValuesObj(GameManager.enemyValueList[count].position);
                GameObject currenEnemy = FindValuesObj(GameManager.enemyValueList[originSortingList[count]].position);
                ///////////////////요 윗부분 originalSortingList랑 그 안에 어쩌구 뭐 있었는데 테스트하느라 지웠다함!!! 문제생기면 여기일듯??ㅁㅁㅇㅁㄴㄻㄴㅇ훠ㅑㅁㅈ둬모ㅓ몬ㅇ 
                
                GameObject player = GameObject.FindWithTag("Player");
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                GameManager.enemyValueList[originSortingList[count]].moveCtrl = -1; //상태창 순서를 행동력 순으로 정렬했을 때, 방금 이동한 적의 순서가 가장 아래로 내려오도록 행동력을 마이너스로 수정.
                uiManager.SortEnemyStates(); //방금 이동한 적의 상태창이 아래로 내려오도록 리스트를 재정렬
                GameManager.enemyValueList[originSortingList[count]].moveCtrl = originMoveCtrl - currentEnemyState.moveCtrl[0]; //적의 행동력 감소
                yield return StartCoroutine(uiManager.ReloadState(originSortingList[count], GameManager.enemyValueList[originSortingList[count]].moveCtrl, count));

                /*if (!turnCheck)
                {
                    turnCheck = true;
                    GameManager.Turn++;
                }*/
            }

            /*else
            {
                if (!turnCheck)
                {
                    turnCheck = true;
                    GameManager.Turn++;

                }
            }*/
        }
    }

    public GameObject FindValuesObj(Vector3 position)
    {
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        foreach(Transform child in enemyBox.transform)
        {
            Debug.Log(child.position);
            if(child.position == position)
            {
                return child.gameObject;
            }
        }
        Debug.LogError("enemyManager error : 어떤 Enemy 스크립트를 찾지 못했습니다.");
        return null;
    }



    public GameObject GetEnemyObject(int num)
    {
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        return enemyBox.transform.GetChild(num).gameObject;
    }
}
