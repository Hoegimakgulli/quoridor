using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public List<GameObject> enemyPrefabs;
    public int currentStage = 0;
    public const float gridSize = 1.3f; // �׸����� ũ��

    private void Awake()
    {
        gameManager = transform.gameObject.GetComponent<GameManager>();
        Enemy.enemyObjects.Clear(); // �� ��ġ �� ��ü ���� �ʱ�ȭ
        Enemy.enemyPositions.Clear();
        SpawnEnemy(); // �� �ڽ�Ʈ�� ���� ��ȯ
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
    }

    void SpawnEnemy()
    {
        int enemyCost = currentStage + 2;
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
                Enemy.enemyObjects.Add(Instantiate(enemyPrefabs[randomNumber], GameManager.gridSize * Enemy.enemyPositions[Enemy.enemyPositions.Count - 1], Quaternion.identity));
                enemyCost -= cost;
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
                Path TargetCurNode = TargetNode;
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
            // �밢�� ����, �� ���̷� ��� �ȵ�
            if (allowDiagonal)
            {
                if(gameManager.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
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

    /*
    void OnDrawGizmos()
    {
        if (FinalPathList.Count != 0) for (int i = 0; i < FinalPathList.Count - 1; i++)
                Gizmos.DrawLine(new Vector2(FinalPathList[i].x, FinalPathList[i].y), new Vector2(FinalPathList[i + 1].x, FinalPathList[i + 1].y));
    }
    */
    void MoveCtrlUpdate()
    {
        Enemy currentEnemyState;
        int count;
        for (count = 0; count < Enemy.enemyObjects.Count; count++)
        {
            currentEnemyState = Enemy.enemyObjects[count].GetComponent<Enemy>();
            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[count] + "�� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);
            currentEnemyState.moveCtrl[1] += Random.Range(0, 3); // �������� ������ ������ �ൿ�� 0 ~ 2
            //Debug.Log("iter " + count + " : " + Enemy.enemyObjects[count] + "�� ���� �ൿ���� �� " + currentEnemyState.moveCtrl[1]);

            if (currentEnemyState.moveCtrl[0] <= currentEnemyState.moveCtrl[1])
            {
                GameObject currenEnemy = Enemy.enemyObjects[count];
                GameObject player = GameObject.Find("Player(Clone)");
                //Debug.Log(Enemy.enemyObjects[count] + " ready move"); // �̺κп��� ���� ������Ʈ�� State�� Move�� �ٲٰ� �� ���� Move�Լ� ��ü ����
                currentEnemyState.state = Enemy.EState.Move;
                PathFinding(currenEnemy, player);
                currentEnemyState.EnemyMove(FinalPathList);
                currentEnemyState.moveCtrl[1] = 0; // ���� �ൿ�� �ʱ�ȭ
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
}
