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
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine.UIElements;


public class GameManager : MonoBehaviour
{
    public enum EPlayerControlStatus { None, Move, Build, Attack, Ability, Destroy };
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;

    // 회륜 추가
    public TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None;
    public Vector2 touchPosition;
    //

    public static int Turn = 1; // 현재 턴
    public const float gridSize = 1.3f; // 그리드의 크기

    public static List<Vector2Int> playerGridPositionList = new List<Vector2Int>(); // 플레이어 위치 정보 저장

    public static List<Vector3> enemyPositions = new List<Vector3>();    // 모든 적들 위치 정보 저장      폐기처분 예정
    public static List<GameObject> enemyObjects = new List<GameObject>(); // 모든 적 기물 오브젝트 저장   폐기처분 예정
    public static List<EnemyValues> enemyValueList = new List<EnemyValues>();
    public static List<Character> enemyCharacterList = new List<Character>(); // 새로 담을 적 리스트
    public static List<Character> playerCharacterList = new List<Character>(); // 새로 담을 플레이어 리스트
    public static List<PlayerValues> playerValueList = new List<PlayerValues>(); // player 기물을 저장 후 사용 (player obj 존재
    [SerializeField]
    public List<Vector2Int> playeMovementCoordinates = new List<Vector2Int>();

    public WallData wallData = new WallData();
    public GameObject wallPrefab;
    int mapWallCount = 10;
    public int playerWallCount = 0;
    public int playerMaxBuildWallCount = 10;
    public int playerDestroyedWallCount = 0;
    public int playerMaxDestroyWallCount = 10;

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

    //[디버그용]
    public static Messanger messanger;
    public int buildDistance;
    public int destroyDistance;

    private static GameManager _instance;
    private GameManager() { }
    public static GameManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<GameManager>();
                if (_instance == null)
                {
                    GameObject container = new GameObject("GameManager");
                    _instance = container.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }
    void Awake()
    {
        if (DataCommunicator.TryGet("MaxWallData", out messanger))
        {
            Debug.Log("Messanger Loaded");
            mapWallCount = messanger.Get<int>("MapWallCount");
            playerMaxBuildWallCount = messanger.Get<int>("BuildWallCount");
            playerMaxDestroyWallCount = messanger.Get<int>("DestroyWallCount");
            buildDistance = messanger.Get<int>("BuildDistance");
            destroyDistance = messanger.Get<int>("DestroyDistance");
        }
        currentStage = StageManager.currentStage;
        Turn = 1; // 턴 초기화
        playerGridPositionList = new List<Vector2Int>(); // 플레이어 위치 초기화
        wallData.Reset();
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

        GetComponent<EnemyStage>().StartEnemyStage();
        CreateRandomWall(mapWallCount);
    }
    public void Initialize()
    {
        for (int i = 0; i < areaAbilityList.Count; i++)
        {
            DestroyImmediate(areaAbilityList[i].gameObject);
        }
        playerGridPositionList = new List<Vector2Int>();
        playerControlStatus = EPlayerControlStatus.None;
        Turn = 1; // 턴 초기화
        wallData.Reset();
        GetComponent<CrashHandler>().Init();
    }
    void Update()
    {
        TouchUtil.TouchSetUp(ref touchState, ref touchPosition);
        if (playerControlStatus == EPlayerControlStatus.None)
        {
            if (touchState == TouchUtil.ETouchState.Began)
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

        if (Input.GetKeyDown(KeyCode.D)) wallData.PrintMap();
        if (Input.GetKeyDown(KeyCode.R)) SceneManager.LoadScene(0);
    }

    public void PlayerSpawn()
    {
        // RaycastHit2D hit;
        List<int> alreadySpawned = new List<int>();
        Vector2 playerPos;
        for (int count = 0; count < playerCount; count++)
        {
            int x;
            // 좌표 뽑아오기
            do
            {
                x = Random.Range(-4, 5);
            } while (alreadySpawned.Contains(x));
            alreadySpawned.Add(x);
            playerPos = new Vector2(x, -4);

            players.Add(Instantiate(playerCharacters.players[Random.Range(1, playerCharacters.players.Count)], playerPos * gridSize, Quaternion.identity));
            playerGridPositionList.Add(ChangeCoord(playerPos));
            Player player = players[count].GetComponent<Player>();
            player.playerIndex = count;
            player.buildInteractionDistance = buildDistance;
            player.destroyInteractionDistance = destroyDistance;
            playerActionUis.Add(players[count].transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>());
        }
    }

    //적턴이 끝나고 플레이어 턴이 시작될 때 실행될 것들
    public void PlayerTurnSet()
    {
        // 현재 턴에서 enemy턴을 제외한 전체 player갯수중 하나 player가 사망시 예외처리 적용해야함
        uiManager.turnEndButton.SetActive(true);
        foreach (GameObject child in players)
        {
            child.GetComponent<Player>().shouldReset = true;
        }
    }
    void CreateRandomWall(int count)
    {
        List<Vector2Int> wallPosList = new List<Vector2Int>();
        for (int i = 0; i < count; i++)
        {
            Vector2Int wallPos;
            int rotation;
            do
            {
                wallPos = new Vector2Int(Random.Range(-4, 5), Random.Range(-4, 5));
                rotation = Random.Range(0, 2);
            } while (wallPosList.Contains(wallPos) || !(bool)(wallData.CanSetWall(wallPos.x, wallPos.y, rotation, true) ?? false));
            GameObject wallObject = Instantiate(wallPrefab);
            wallData.SetWall(wallPos.x, wallPos.y, rotation, ref wallObject);
            wallPosList.Add(wallPos);

        }

    }
    public static Vector3 ChangeCoord(Vector2Int originVector) { return ((Vector3)(Vector2)originVector * gridSize); }
    public static Vector2Int ChangeCoord(Vector3 originVector) { return new Vector2Int(Mathf.RoundToInt((originVector / gridSize).x), Mathf.RoundToInt((originVector / gridSize).y)); }
}
