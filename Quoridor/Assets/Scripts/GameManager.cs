using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public enum EPlayerControlStatus { None, Move, Build, Attack };
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;

    public static int Turn = 1; // 현재 턴
    public const float gridSize = 1.3f; // 그리드의 크기

    public Vector3 playerPosition = new Vector3(0, -4, 0); // 플레이어의 위치

    public int[,] mapGraph = new int[81, 81]; //DFS용 맵 그래프

    public int currentStage;

    GameObject player;

    public GameObject playerPrefab;

    void Awake()
    {
        Turn = 1; // 턴 초기화
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
        player = Instantiate(playerPrefab, playerPosition * gridSize, Quaternion.identity);
    }
    void Update()
    {
        if (Turn % 2 == 0 && Input.GetKey(KeyCode.Space)) //[디버그용] space 키를 통해 적턴 넘기기
        {
            Turn++;
        }
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
        foreach(Vector3 enemyPosition in Enemy.enemyPositions)
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
                }
                log += rowInfo + '\n';
            }
            log += '\n';
        }
        Debug.Log(log);
    }
}
