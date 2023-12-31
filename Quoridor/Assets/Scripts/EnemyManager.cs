using System.Collections;
using System.Collections.Generic;
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
    public List<GameObject> enemyPrefabs; // 기본 유닛 오브젝트들 리스트 넣어두기
    public List<GameObject> loyalEnemyPrefabs; // 상위 고급 유닛 오브젝트들 리스트 넣어두기

    public int currentStage = 0;
    public GameObject enemyStatePrefab; // 적 기물 상태 판넬안에 들어가는 기본 빵틀 이라고 생각.
    public GameObject warningSignBox; // 경고 표기 담아두는 박스
    public const float gridSize = 1.3f; // 그리드의 크기

    private bool enemyTurnAnchor = true;
    private bool enemyWarningSignAnchor = true;

    private void Awake()
    {
        gameManager = transform.gameObject.GetComponent<GameManager>();
        Enemy.enemyObjects.Clear(); // 적 위치 및 객체 정보 초기화
        Enemy.enemyPositions.Clear();
    }

    private void Start()
    {
        Instantiate(warningSignBox);
        SpawnEnemy(); // 적 코스트에 따라 소환
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            CheckEnemyInfo();
        }

        if (Input.GetKeyDown(KeyCode.W))
        {
            MoveCtrlUpdate();
        }

        // 적 턴일때 (이동 및 공격확인)
        if(GameManager.Turn % 2 == 0 && enemyTurnAnchor)
        {
            enemyWarningSignAnchor = true;
            enemyTurnAnchor = false;
            // 경고sign 초기화
            foreach(Transform child in GameObject.FindWithTag("WarningBox").transform)
            {
                Destroy(child.gameObject);
            }
            // 오브젝트 카운트 초기화
            for(int count = 0; count < Enemy.enemyObjects.Count; count++)
            {
                Enemy.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text = "";
            }

            StartCoroutine(StartEnemyTurn());
        }

        // 플레이어 턴일때 (적 움직임 경고)
        if(GameManager.Turn % 2 == 1 && enemyWarningSignAnchor)
        {
            enemyWarningSignAnchor = false;
            WarningEnemy();
        }
    }

    void SpawnEnemy()
    {
        int enemyCost = currentStage + 2;
        while (enemyCost != 0) // enemyCost = totalCost 0이 되기 전까지 계속 확인 후 소환
        {
            int randomNumber = Random.Range(0, enemyPrefabs.Count);
            int cost = enemyPrefabs[randomNumber].GetComponent<Enemy>().cost;
            if (enemyCost - cost >= 0)
            {
                Vector3 enemyPosition;
                do
                { 
                    enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0); }
                while (Enemy.enemyPositions.Contains(enemyPosition) && Enemy.enemyPositions.Count != 0); // 이미 소환된 적의 위치랑 안 겹칠때
                Enemy.enemyPositions.Add(enemyPosition);
                GameObject currentEnemyObj = Instantiate(enemyPrefabs[randomNumber], GameManager.gridSize * Enemy.enemyPositions[Enemy.enemyPositions.Count - 1], Quaternion.identity);
                Enemy.enemyObjects.Add(currentEnemyObj);
                enemyCost -= cost;

                // 유닛 판넬안에 보드위에 있는 적들 데이터 정보를 넣는 부분
                Enemy currentEnemey = currentEnemyObj.GetComponent<Enemy>();

                // 적 정보 UI 판넬에 표시하는 부분
                GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("EnemyStateContent").transform);
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = enemyPrefabs[randomNumber].GetComponent<SpriteRenderer>().sprite;
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().color = enemyPrefabs[randomNumber].GetComponent<SpriteRenderer>().color;
                currentEnemyState.transform.GetChild(1).GetComponent<Text>().text = "행동력 " + currentEnemey.cost + " / 10";
                currentEnemey.maxHp = currentEnemey.hp;
                currentEnemyState.transform.GetChild(2).GetComponent<Text>().text = "체력 " + currentEnemey.hp + " / " + currentEnemey.maxHp;
            }
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
                Path TargetCurNode = TargetNode;
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
            if (CheckEnemyPos(new Vector2(checkX, checkY))) return;
            // 대각선 허용시, 벽 사이로 통과 안됨
            if (allowDiagonal)
            {
                if(gameManager.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
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

    private bool CheckEnemyPos(Vector2 currentPos)
    {
        foreach(Vector2 enemyPos in Enemy.enemyPositions)
        {
            if(currentPos == enemyPos)
            {
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
        for (count = 0; count < Enemy.enemyObjects.Count; count++)
        {
            currentEnemyState = Enemy.enemyObjects[count].GetComponent<Enemy>();

            Debug.Log("iter " + count + " : " + Enemy.enemyObjects[count] + "의 행동력은 → " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += Random.Range(0, (currentEnemyState.moveCtrl[2] + 1)); // 랜덤으로 들어오는 무작위 행동력 0 ~ 적 행동력 회복 최대치
            Debug.Log("iter " + count + " : " + Enemy.enemyObjects[count] + "의 변동 행동력은 → " + currentEnemyState.moveCtrl[1]);

            //currentEnemyState.moveCtrl[1] += 10; // test 용 추가

            if (currentEnemyState.moveCtrl[0] <= currentEnemyState.moveCtrl[1])
            {
                GameObject currenEnemy = Enemy.enemyObjects[count];
                GameObject player = GameObject.FindWithTag("Player");
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                currentEnemyState.moveCtrl[1] = 0; // 현재 행동력 초기화
                /*
                foreach(Transform child in GameObject.FindWithTag("WarningBox").transform)
                {
                    if(child.GetComponent<Text>().text == Enemy.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text)
                    {
                        child
                    }
                }
                Enemy.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text = "";
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

    public GameObject enemyWarningPrefab;
    public void WarningEnemy()
    {
        int count;
        int index = 1;
        GameObject player = GameObject.FindWithTag("Player");
        Vector2 playerPos = player.transform.position / GameManager.gridSize;

        for (count = 0; count < Enemy.enemyObjects.Count; count++)
        {
            Enemy currentEnemy = Enemy.enemyObjects[count].GetComponent<Enemy>();
            GameObject currentEnemyObj = Enemy.enemyObjects[count];
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
    }

    void CheckEnemyInfo()
    {
        for (int count = 0; count < Enemy.enemyPositions.Count; count++)
        {
            Debug.Log((count + 1) + "iter - enemyPos : " + Enemy.enemyPositions[count] + " - enemyObj : " + Enemy.enemyObjects[count]);
        }
    }

    void CheckPlayerFromEnemy(List<Path> path)
    {
        for(int i = 0; i < path.Count; i++)
        {
            Debug.Log("NodePos : " + path[i].x + "," + path[i].y + " | G : " + path[i].G + " | H : " + path[i].H + " | F : " + path[i].F);
        }
        Debug.Log("End Check --------------------------------------------------");
    }

     IEnumerator StartEnemyTurn()
    {
        yield return new WaitForSeconds(2);
        MoveCtrlUpdate();
        enemyTurnAnchor = true;
    }
}
