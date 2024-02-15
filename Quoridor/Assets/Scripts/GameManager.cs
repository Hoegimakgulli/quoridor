using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class enemyValues
{
    public int hp;
    public int moveCtrl;
    public int uniqueNum;
    public int spawnNum;
    public Vector3 position;

    public enemyValues(int hp, int moveCtrl, int uniqueNum, int spawnNum, Vector3 position)
    {
        this.hp = hp;
        this.moveCtrl = moveCtrl;
        this.uniqueNum = uniqueNum;
        this.spawnNum = spawnNum;
        this.position = position;
    }
}

public class GameManager : MonoBehaviour
{
    public enum EPlayerControlStatus { None, Move, Build, Attack, Ability };
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;

    public static int Turn = 1; // 현재 턴
    public const float gridSize = 1.3f; // 그리드의 크기

    public static Vector3 playerPosition = new Vector3(0, -4, 0); // 플레이어의 위치

    public static List<Vector3> enemyPositions = new List<Vector3>();    // 모든 적들 위치 정보 저장      폐기처분 예정
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장   폐기처분 예정
    public static List<enemyValues> enemyValueList = new List<enemyValues>();

    public int[,] mapGraph = new int[81, 81]; //DFS용 맵 그래프

    public int currentStage;

    GameObject player;

    public PlayerCharacters playerCharacters;

    void Awake()
    {
        Turn = 1; // 턴 초기화
        playerPosition = new Vector3(0, -4, 0); // 플레이어 위치 초기화
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
        player = Instantiate(playerCharacters.players[Random.Range(1, playerCharacters.players.Count)], playerPosition * gridSize, Quaternion.identity);
        Debug.Log(player.transform.position);
    }
    public void Initialize()
    {
        enemyPositions.Clear();
        enemyObjects.Clear();
        playerPosition = new Vector3(0, -4, 0);
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
    }
    void Update()
    {
        if (Turn % 2 == 0 && Input.GetKey(KeyCode.Space)) //[디버그용] space 키를 통해 적턴 넘기기
        {
            Turn++;
        }

        if (Input.GetKeyDown(KeyCode.D)) DebugMap();
    }
    //DFS 알고리즘을 이용한 벽에 갇혀있는지 체크
    public bool CheckStuck()
    {
        bool[] visited = new bool[81];
        int playerGraphPosition = (int)((playerPosition.y + 4) * 9 + playerPosition.x + 4);

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
}
