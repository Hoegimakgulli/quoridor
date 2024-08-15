using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;

#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
using UnityEditor.SceneManagement;

#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
// using static Player;
using HM.Utils;
using HM.Containers;


public class GameManager : MonoBehaviour
{
    public enum EPlayerControlStatus { None, Move, Build, Attack, Ability };
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;

    // 회륜 추가
    public TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None;
    public Vector2 touchPosition;
    //

    public static int Turn = 1; // 현재 턴
    public const float gridSize = 1.3f; // 그리드의 크기

    public static Vector2Int playerGridPosition = new Vector2Int(0, -4); // 플레이어의 타일 위치

    public static List<Vector3> enemyPositions = new List<Vector3>();    // 모든 적들 위치 정보 저장      폐기처분 예정
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장   폐기처분 예정
    public static List<EnemyValues> enemyValueList = new List<EnemyValues>();

    public int[,] mapGraph = new int[81, 81]; //DFS용 맵 그래프

    public int currentStage;

    public int playerCount;
    public List<GameObject> players;
    public List<PlayerActionUI> playerActionUis;
    public GameObject player;
    public PlayerActionUI playerActionUI;
    public UiManager uiManager;

    public PlayerCharacters playerCharacters;

    public GameObject autoTrap;
    public bool canEnemyTurn = false;

    public List<AreaAbility> areaAbilityList = new List<AreaAbility>();
    public int tempTurn;

    // public static GameManager Instance;
    // GameManager() { }
    void Awake()
    {
        currentStage = StageManager.currentStage;
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
        PlayerSpawn();
        //player = Instantiate(playerCharacters.players[Random.Range(1, playerCharacters.players.Count)], ChangeCoord(playerGridPosition), Quaternion.identity);
        //playerActionUI = player.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>();
        uiManager = GetComponent<UiManager>();
        Debug.Log("GameManager Awake");

    }
    private void Start()
    {
        // 프레임 60fps로 설정
#if UNITY_ANDROID
        Application.targetFrameRate = 60;
#endif
        Debug.Log("GameManager Start");
    }
    public void Initialize()
    {
        enemyPositions.Clear();
        enemyObjects.Clear();
        for (int i = 0; i < areaAbilityList.Count; i++)
        {
            DestroyImmediate(areaAbilityList[i].gameObject);
        }
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
        TouchUtil.TouchSetUp(ref touchState, ref touchPosition);
        if (playerControlStatus == EPlayerControlStatus.None)
        {
            if(touchState == TouchUtil.ETouchState.Began)
            {
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(touchPosition), Vector3.forward, 15f, LayerMask.GetMask("Token"));

                if (hit.collider != null && hit.collider.gameObject.tag == "Player")
                {
                    player = hit.transform.gameObject;
                    playerActionUI = player.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>();
                    foreach (GameObject child in players)
                    {
                        if (child == player)
                        {
                            child.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>().ActiveUI();
                        }
                        else
                        {
                            child.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>().PassiveUI();
                        }
                    }
                }

                else if (hit.collider != null && hit.collider.gameObject.tag == "Enemy")
                {
                    hit.collider.gameObject.GetComponent<Enemy>().EnemyActionInfo();
                }

                else
                {
                    player = null;
                    foreach(PlayerActionUI playerUi in playerActionUis)
                    {
                        playerUi.PassiveUI();
                    }
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
                player = null;
                tempTurn = Turn;
            }
            canEnemyTurn = areaAbilityList.All(areaAbility => areaAbility.canDone);
        }

        if (Input.GetKeyDown(KeyCode.D)) DebugMap();
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(0);
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

    public void PlayerSpawn()
    {
        RaycastHit2D hit;
        Vector2 playerPos;
        for(int count = 0; count < playerCount; count++)
        {
            // 좌표 뽑아오기
            do
            {
                playerPos = new Vector2Int(Random.Range(-4, 5), -4);
                hit = Physics2D.Raycast(playerPos * gridSize, Vector3.forward, 15f, LayerMask.GetMask("Token"));
            } while (hit);

            players.Add(Instantiate(playerCharacters.players[Random.Range(1, playerCharacters.players.Count)], playerPos * gridSize, Quaternion.identity));
            players[count].GetComponent<Player>().playerIndex = count;
            playerActionUis.Add(players[count].transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>());
        }
    }

    //적턴이 끝나고 플레이어 턴이 시작될 때 실행될 것들
    public void PlayerTurnSet()
    {
        // 현재 턴에서 enemy턴을 제외한 전체 player갯수중 하나 player가 사망시 예외처리 적용해야함
        uiManager.turnEndButton.SetActive(true);
        foreach(GameObject child in players)
        {
            child.GetComponent<Player>().shouldReset = true;
        }
    }
    public static Vector3 ChangeCoord(Vector2Int originVector) { return ((Vector3)(Vector2)originVector * gridSize); }
    public static Vector2Int ChangeCoord(Vector3 originVector) { return new Vector2Int(Mathf.RoundToInt((originVector / gridSize).x), Mathf.RoundToInt((originVector / gridSize).y)); }
}
