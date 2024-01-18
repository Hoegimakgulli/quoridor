using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class Path
{
    // �߾� ��ǥ�� (0, 0) �������� x, y ��ǥ
    // G = �������κ��� �̵��� �Ÿ�, H = ����, ���η� ���� �����ϰ� Player���� �̵��� �Ÿ�
    public int x, y, G, H;

    public Path(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public Path ParentNode;

    // F = G, H �� �ջ갪
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
    public List<GameObject> enemyPrefabs; // �⺻ ���� ������Ʈ�� ����Ʈ �־�α�
    public List<GameObject> loyalEnemyPrefabs; // ���� ���� ���� ������Ʈ�� ����Ʈ �־�α�

    // public static int gameManager.currentStage = 0;
    public GameObject warningSignBox; // ��� ǥ�� ��Ƶδ� �ڽ�
    public GameObject enemyUiCanvas;
    public const float gridSize = 1.3f; // �׸����� ũ��

    private bool enemyTurnAnchor = true;
    //private bool enemyWarningSignAnchor = true;

//�̱Ժ� �߰�
    private int sortingNum = 0; //�����Ҷ� ���� int
    public List<int> sortingList = new List<int>(); //�����Ҷ� ���� ����Ʈ

    private UiManager uiM;
    public GameObject EnemyStatePanel;

    private void Awake()
    {
        sortingNum = 0;
        gameManager = transform.gameObject.GetComponent<GameManager>();
        uiM = GetComponent<UiManager>();
        Enemy.enemyObjects.Clear(); // �� ��ġ �� ��ü ���� �ʱ�ȭ
        Enemy.enemyPositions.Clear();
        GameObject enemyUi = Instantiate(enemyUiCanvas);
        Instantiate(EnemyStatePanel, enemyUi.transform);
        gameManager = transform.gameObject.GetComponent<GameManager>();
        GameManager.enemyObjects.Clear(); // �� ��ġ �� ��ü ���� �ʱ�ȭ
        GameManager.enemyPositions.Clear();
    }

    private void Start()
    {
        Instantiate(warningSignBox);
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

        // �� ���϶� (�̵� �� ����Ȯ��)
        if (GameManager.Turn % 2 == 0 && enemyTurnAnchor)
        {
            //enemyWarningSignAnchor = true;
            enemyTurnAnchor = false;
            // ���sign �ʱ�ȭ
            foreach (Transform child in GameObject.FindWithTag("WarningBox").transform)
            {
                Destroy(child.gameObject);
            }
            // ������Ʈ ī��Ʈ �ʱ�ȭ
            /*
            for (int count = 0; count < GameManager.enemyObjects.Count; count++)
            {
                GameManager.enemyObjects[count].transform.GetChild(0).GetComponent<TextMesh>().text = "";
            }*/

            StartCoroutine(StartEnemyTurn());
        }

        // �÷��̾� ���϶� (�� ������ ���)
        /*
        if(GameManager.Turn % 2 == 1 && enemyWarningSignAnchor)
        {
            enemyWarningSignAnchor = false;
           WarningEnemy();
        }
        */
    }

    void SpawnEnemy()
    {
        sortingNum = 0;
        int enemyCost = currentStage + 10;
        while (enemyCost != 0) // enemyCost = totalCost 0�� �Ǳ� ������ ��� Ȯ�� �� ��ȯ
        {
            int randomNumber = Random.Range(0, enemyPrefabs.Count);
            int cost = enemyPrefabs[randomNumber].GetComponent<Enemy>().cost;
            if (enemyCost - cost >= 0)
            {
                Vector3 enemyPosition;
                do
                { 
                    enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0); }
                while (Enemy.enemyPositions.Contains(enemyPosition) && Enemy.enemyPositions.Count != 0); // �̹� ��ȯ�� ���� ��ġ�� �� ��ĥ��
                Enemy.enemyPositions.Add(enemyPosition);
                GameObject currentEnemyObj = Instantiate(enemyPrefabs[randomNumber], GameManager.gridSize * Enemy.enemyPositions[Enemy.enemyPositions.Count - 1], Quaternion.identity);
                Enemy.enemyObjects.Add(currentEnemyObj);
                enemyCost -= cost;

                // ���� �ǳھȿ� �������� �ִ� ���� ������ ������ �ִ� �κ�
                Enemy currentEnemey = currentEnemyObj.GetComponent<Enemy>();

                // �� ���� UI �ǳڿ� ǥ���ϴ� �κ�
                GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("EnemyStateContent").transform);
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = enemyPrefabs[randomNumber].GetComponent<SpriteRenderer>().sprite;
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().color = enemyPrefabs[randomNumber].GetComponent<SpriteRenderer>().color;
                currentEnemyState.transform.GetChild(1).GetComponent<Text>().text = "�ൿ�� " + currentEnemey.cost + " / 10";
                currentEnemey.maxHp = currentEnemey.hp;
                currentEnemyState.transform.GetChild(2).GetComponent<Text>().text = "ü�� " + currentEnemey.hp + " / " + currentEnemey.maxHp;

                //�� �Ʒ����� �̱Ժ� �ۼ���Ʈ
                sortingList.Add(sortingNum);
                sortingNum++;
                if(enemyCost == 0)
                {
                    EnemyStateSort();
                    uiM.EnemyStateSetting();
                   // currentEnemyState.transform.parent.parent.parent.parent.gameObject.SetActive(false);
                }
            }
        }
    }
    
    //���� ����â ������ ���� sortingList�� ����
    public void EnemyStateSort()
    {
        for (int i = 1; i < Enemy.enemyObjects.Count; i++)
        {
            int key = sortingList[i];
            int j = i - 1;

            while (j >= 0 && Enemy.enemyObjects[sortingList[j]].GetComponent<Enemy>().moveCtrl[1] < Enemy.enemyObjects[key].GetComponent<Enemy>().moveCtrl[1])
            {
                sortingList[j + 1] = sortingList[j];
                j--;
            }
            sortingList[j + 1] = key;
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

    // A* �˰�����
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

        // startPos, endPos ���� position�� girdSize�� ������ ����ȭ ������ ���� ��ǥ�� (0, 0)�� ���� �Ʒ� �����ڸ��� �ٲ���
        Vector2 trimPos;
        trimPos = startObj.transform.position / GameManager.gridSize;
        startPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);
        trimPos = endObj.transform.position / GameManager.gridSize;
        targetPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);

        StartNode = PathArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
        TargetNode = PathArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

        // ���۰� �� ���, ��������Ʈ�� ��������Ʈ, ����������Ʈ �ʱ�ȭ
        OpenList = new List<Path>() { StartNode };
        ClosedList = new List<Path>();
        FinalPathList = new List<Path>();

        while (OpenList.Count > 0)
        {
            // ��������Ʈ �� ���� F�� �۰� F�� ���ٸ� H�� ���� �� ������� �ϰ� ��������Ʈ���� ��������Ʈ�� �ű��
            CurNode = OpenList[0];
            for (int i = 1; i < OpenList.Count; i++)
            {
                if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];
            }

            OpenList.Remove(CurNode);
            ClosedList.Add(CurNode);


            // ������
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

                //for (int i = 0; i < FinalPathList.Count; i++) print(i + "��°�� " + FinalPathList[i].x + ", " + FinalPathList[i].y);
                return;
            }

            if (allowDiagonal)
            {
                // ������ �� ��ǥ ����
                OpenListAdd(CurNode.x + 1, CurNode.y + 1); // ������ ��
                OpenListAdd(CurNode.x - 1, CurNode.y + 1); // ���� ��
                OpenListAdd(CurNode.x - 1, CurNode.y - 1); // ���� �Ʒ�
                OpenListAdd(CurNode.x + 1, CurNode.y - 1); // ������ �Ʒ�
            }

            // �� �� �� ��
            OpenListAdd(CurNode.x, CurNode.y + 1); // ��
            OpenListAdd(CurNode.x + 1, CurNode.y); // ������
            OpenListAdd(CurNode.x, CurNode.y - 1); // �Ʒ�
            OpenListAdd(CurNode.x - 1, CurNode.y); // ����
        }

        if (FinalPathList.Count == 0)
        {
            PathFinding(startObj, blockEmemyObj);
        }
    }

    public GameManager gameManager;
    void OpenListAdd(int checkX, int checkY)
    {
        // graph �� (0,0) == 0�� ����
        int startGraphPosition = (int)(CurNode.y * 9 + CurNode.x);
        int endGraphPosition = (int)(checkY * 9 + checkX);
        // �����¿� ������ ����� �ʰ�, ���� �ƴϸ鼭, ��������Ʈ�� ���ٸ�
        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1 && !ClosedList.Contains(PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
        {
            // start �������� ���� end ���� ���̿� ���� �ִ��� Ȯ��
            if (gameManager.mapGraph[startGraphPosition, endGraphPosition] == 0) return;
            if (CheckEnemyPos(new Vector2((checkX - 4) * GameManager.gridSize, (checkY - 4) * GameManager.gridSize))) return;
            // �밢�� ����, �� ���̷� ��� �ȵ�
            if (allowDiagonal)
            {
                if (gameManager.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
                {
                    if (checkY - CurNode.y == 1)
                    {
                        if (gameManager.mapGraph[startGraphPosition, startGraphPosition + 9] == 0) return; // �Ʒ����� ���� �ö󰡴� ���
                    }
                    else if (checkY - CurNode.y == -1)
                    {
                        if (gameManager.mapGraph[startGraphPosition, startGraphPosition - 9] == 0) return; // ������ �Ʒ��� �������� ���
                    }
                }
            }

            // �ڳʸ� �������� ���� ������, �̵� �߿� �������� ��ֹ��� ������ �ȵ�
            if (dontCrossCorner)
            {
                //if (PathArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall || PathArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;
            }

            // �̿���忡 �ְ�, ������ 10, �밢���� 14���
            Path NeighborNode = PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);

            // �̵������ �̿����G���� �۰ų� �Ǵ� ��������Ʈ�� �̿���尡 ���ٸ� G, H, ParentNode�� ���� �� ��������Ʈ�� �߰�
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
        foreach (GameObject enemy in GameManager.enemyObjects)
        {
            Vector2 enemyPos = enemy.transform.position;
            if (currentPos == enemyPos && currentPos != new Vector2((TargetNode.x - 4) * GameManager.gridSize, (TargetNode.y - 4) * GameManager.gridSize))
            {
                blockEmemyObj = enemy;
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








    /*
    void MoveCtrlUpdate()
    {
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < GameManager.enemyObjects.Count; count++)
        {
            currentEnemyState = GameManager.enemyObjects[count].GetComponent<Enemy>();

            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "�� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += currentEnemyState.moveCtrl[2]; // �������� ������ ������ �ൿ�� 0 ~ �� �ൿ�� ȸ�� �ִ�ġ
            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "�� ���� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);

            //currentEnemyState.moveCtrl[1] += 10; // test �� �߰�

            if (currentEnemyState.moveCtrl[0] <= currentEnemyState.moveCtrl[1])
            {
                GameObject currenEnemy = GameManager.enemyObjects[count];
                GameObject player = GameObject.FindWithTag("Player");
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                currentEnemyState.moveCtrl[1] = 0; // ���� �ൿ�� �ʱ�ȭ
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
  /*              if (!turnCheck)
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
 */









    void testMove()
    {
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < GameManager.enemyObjects.Count; count++)
        {
            currentEnemyState = GameManager.enemyObjects[count].GetComponent<Enemy>();

            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "�� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += currentEnemyState.moveCtrl[2]; // ȸ���ϴ� �� �ൿ��
            Debug.Log("iter " + count + " : " + GameManager.enemyObjects[count] + "�� ���� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);
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
        StartCoroutine(MoveCtrlUpdate());
        yield return new WaitForSeconds(2);
        //MoveCtrlUpdate();
        enemyTurnAnchor = true;
    }





    IEnumerator MoveCtrlUpdate()
    {
        uiM.popLock = true; //////////////////////////////////////////////////////////// �ӽ�
        List<int> originSortingList = new List<int>();
        int originCost;  //���� �ൿ��. �ڽ�Ʈ�� �ൿ������ ������.
        for(int i = 0; i < sortingList.Count; i++)
        {
            originSortingList.Add(sortingList[i]);
        }
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < Enemy.enemyObjects.Count; count++)
        {
            currentEnemyState = Enemy.enemyObjects[originSortingList[count]].GetComponent<Enemy>();
            originCost = currentEnemyState.moveCtrl[1];

            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[sortingList[count]] + "�� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += Random.Range(1, (currentEnemyState.moveCtrl[2] + 1)); // �������� ������ ������ �ൿ�� 0 ~ �� �ൿ�� ȸ�� �ִ�ġ
            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[sortingList[count]] + "�� ���� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);

            EnemyStateSort();
            yield return StartCoroutine(uiM.MovectrlCountAnim(originSortingList[count], originCost, currentEnemyState.moveCtrl[1]));
            originCost = currentEnemyState.moveCtrl[1]; //���⼭���� originCost�� ���� �ൿ��

            originCost = currentEnemyState.moveCtrl[1]; //���⼭���� originCost�� ���� �ൿ��

            if (currentEnemyState.moveCtrl[0] <= currentEnemyState.moveCtrl[1])
            {
                GameObject currenEnemy = Enemy.enemyObjects[originSortingList[count]];
                GameObject player = GameObject.FindWithTag("Player");
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                //currentEnemyState.moveCtrl[1] = 0; // ���� �ൿ�� �ʱ�ȭ
                Debug.Log("�ٲ�� �� �ൿ��"+currentEnemyState.moveCtrl[1]);
                //currentEnemyState.moveCtrl[1] -= currentEnemyState.moveCtrl[0]; //�ൿ�� ����
                Debug.Log("���ҵ� �ൿ�� " + currentEnemyState.moveCtrl[0]);
                Debug.Log("�ٲ� �� �ൿ�·�   "+ currentEnemyState.moveCtrl[1]);
                currentEnemyState.moveCtrl[1] = -1;
                EnemyStateSort();
                currentEnemyState.moveCtrl[1] = originCost - currentEnemyState.moveCtrl[0];
                yield return StartCoroutine(uiM.ReloadState(originSortingList[count], currentEnemyState.moveCtrl[1]));

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

        uiM.popLock = false; /////////////////////////////////////////�ӽ�
    }
}
