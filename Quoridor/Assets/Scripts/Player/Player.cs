using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    protected GameManager gameManager;

    public enum ETouchState { None, Began, Moved, Ended }; //모바일과 에디터 모두 가능하게 터치 & 마우스 처리
    public ETouchState touchState = ETouchState.None;
    Vector2 touchPosition;

    [SerializeField]
    public List<Vector2Int> movablePositions = new List<Vector2Int>(); // 플레이어의 가능한 이동 좌표들
    [SerializeField]
    List<Vector2Int> attackablePositions = new List<Vector2Int>(); // 플레이어의 가능한 공격 좌표들
    [SerializeField]
    List<Vector2Int> attackPositions = new List<Vector2Int>() { Vector2Int.zero }; // 플레이어의 가능한 공격 좌표들

    List<Vector2Int> allPositions = new List<Vector2Int>(); // 모든 좌표

    [SerializeField]
    PlayerPrefabs playerPrefabs; // 플레이어 관련 프리팹 모음
    List<GameObject> playerPreviews = new List<GameObject>();
    List<GameObject> playerAttackPreviews = new List<GameObject>();
    List<GameObject> playerAttackHighlights = new List<GameObject>();
    List<GameObject> playerAbilityPreviews = new List<GameObject>();
    List<GameObject> playerAbilityHighlights = new List<GameObject>();
    GameObject playerWallPreview;
    [SerializeField]
    GameObject historyIndexPrefab; // history 형식으로 저장되는 글 양식

    public const int playerOrder = 1; // 플레이어 차례

    // player Status //
    public int atk;

    [SerializeField]
    public int wallCount = 0;
    public int maxWallCount;

    int minWallLength = 1;
    Vector2 wallStartPos = new Vector2(-50, -50);
    int[] wallInfo = new int[3]; // 벽 위치 정보, 회전 정보 저장

    public bool shouldReset = true;

    public bool canAction = true;
    public bool shouldMove = false;
    public bool shouldBuild = false;
    public bool canAttack = true;
    public int abilityCount = 0;
    protected bool canSignAbility = true;
    public int usingAbilityID;

    int[] previousWallInfo = new int[3];
    int[,] tempMapGraph = new int[81, 81];

    /*[디버그용]*/
    [SerializeField]
    GameObject playerUI;
    /*[임시 능력 UI]*/
    public GameObject abilityUI;
    [SerializeField]
    GameObject abilityUIButton;

    GameObject wallStorage;
    PlayerActionUI playerActionUI;

    PlayerAbility playerAbility;

    void Awake()
    {
        playerActionUI = Instantiate(playerPrefabs.actionUI, transform).transform.GetChild(0).GetComponent<PlayerActionUI>();
    }
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        wallStorage = GameObject.Find("WallStorage");
        for (int i = 0; i < movablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerPreviews.Add(Instantiate(playerPrefabs.playerPreview, transform.position, Quaternion.identity));
            playerPreviews[i].SetActive(false);
        }
        for (int i = 0; i < attackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackPreviews.Add(Instantiate(playerPrefabs.attackPreview, transform.position, Quaternion.identity));
            playerAttackPreviews[i].SetActive(false);
        }
        for (int i = 0; i < attackPositions.Count; i++) // 플레이어 공격 하이라이트 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackHighlights.Add(Instantiate(playerPrefabs.attackHighlight, transform.position, Quaternion.identity));
            playerAttackHighlights[i].SetActive(false);
        }
        for (int i = 0; i < 81; i++) // 플레이어 능력 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityPreviews.Add(Instantiate(playerPrefabs.attackPreview, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
            playerAbilityPreviews[i].SetActive(false);
        }
        for (int i = 0; i < 81; i++) // 플레이어 능력 하이라이트 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityHighlights.Add(Instantiate(playerPrefabs.attackHighlight, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
            playerAbilityHighlights[i].SetActive(false);
        }
        for (int i = 0; i < maxWallCount; i++)
        {
            Instantiate(playerPrefabs.wall, wallStorage.transform).SetActive(false);
        }
        for (int i = 0; i < 81; i++)
        {
            allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        }
        playerWallPreview = Instantiate(playerPrefabs.wallPreview, transform.position, Quaternion.identity); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
        playerWallPreview.SetActive(false);
        tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵그래프 저장

        //playerUI = Instantiate(playerUI); // [디버그용]
        abilityUI = Instantiate(abilityUI); // [임시 능력 UI]
        playerAbility = GetComponent<PlayerAbility>();
    }
    public void Initialize()
    {
        transform.position = GameManager.gridSize * new Vector3(0, -4, 0);
        wallCount = 0;

        for (int i = 0; i < wallStorage.transform.childCount; i++)
        {
            wallStorage.transform.GetChild(i).gameObject.SetActive(false);
        }

        Reset();

        previousWallInfo = new int[3];
        tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵그래프 저장
    }
    // Update is called once per frame
    void Update()
    {
        TouchSetUp();
        playerAbility.ResetEvent(PlayerAbility.EResetTime.OnEveryTick);
        if (GameManager.Turn % 2 == playerOrder) // 플레이어 차례인지 확인
        {
            playerAbility.ResetEvent(PlayerAbility.EResetTime.OnPlayerTurnStart);
            playerUI.SetActive(true); // [디버그용]
            {
                Transform canvas = playerUI.transform.GetChild(0);
                canvas.GetChild(5).GetComponent<Text>().text = $"{maxWallCount - wallCount}/{maxWallCount}";
                // [디버그용] //
                canvas.GetChild(1).GetComponent<Button>().interactable = canAction || shouldBuild;  // 건설 버튼
                canvas.GetChild(2).GetComponent<Button>().interactable = canAction || shouldMove;   // 이동 버튼
                canvas.GetChild(0).GetComponent<Button>().interactable = canAttack;                 // 공격 버튼
                canvas.GetChild(3).GetComponent<Button>().interactable = abilityCount == 0;         // 능력 버튼
            }
            if (abilityCount > 0)
            {   //[임시 능력 UI]
                abilityUI.SetActive(true);
                playerAbility.ActiveEvent();
            }

            touchPosition = Camera.main.ScreenToWorldPoint(touchPosition); //카메라에 찍힌 좌표를 월드좌표로
            switch (gameManager.playerControlStatus)
            {
                case GameManager.EPlayerControlStatus.Move:
                    if (canAction || shouldMove) MovePlayer();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Build:
                    if (canAction || shouldBuild) BuildWall();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Attack:
                    if (canAttack) Attack();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Ability:
                    if (abilityCount > 0) UseAbility();
                    break;
                default:
                    break;
            }
            shouldReset = true;
        }
        else // 플레이어 차례가 아니면
        {
            if (shouldReset) Reset();
        }
    }
    protected virtual void Reset()
    {
        playerUI.SetActive(false); // [디버그용]
        abilityUI.SetActive(false); // [임시 능력 UI]
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
        canAction = true;
        canAttack = true;
        ResetPreview();
        playerAbility.ResetEvent(PlayerAbility.EResetTime.OnEnemyTurnStart);
        shouldReset = false;
    }
    // 모바일 or 에디터 마우스 터치좌표 처리
    void TouchSetUp()
    {
#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0)) { if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Began; } }
        else if (Input.GetMouseButton(0)) { if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Moved; } }
        else if (Input.GetMouseButtonUp(0)) { if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Ended; } }
        else touchState = ETouchState.None;
        touchPosition = Input.mousePosition;

#else
        if(Input.touchCount > 0) {
        
            Touch touch = Input.GetTouch(0);
            if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) == true) return;
            if (touch.phase == TouchPhase.Began)  touchState = ETouchState.Began;
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) touchState = ETouchState.Moved;
            else if (touch.phase == TouchPhase.Ended)touchState = ETouchState.Ended;
            touchPosition = touch.position;
        }else touchState = ETouchState.None;
#endif
    }
    void MovePlayer()
    {
        SetPreviewPlayer();
        if (touchState == ETouchState.Began) //화면 클릭시
        {
            RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerPreview")) // 클릭좌표에 플레이어미리보기가 있다면
                {
                    transform.position = previewHit.transform.position; //플레이어 위치 이동
                    GameManager.playerPosition = transform.position / GameManager.gridSize; //플레이어 위치정보 저장
                    canAction = false; // 이동이나 벽 설치 불가
                    playerAbility.MoveEvent();
                    if (shouldMove) shouldMove = false;
                    playerActionUI.ActiveUI(); //플레이어 행동 UI 등장 애니메이션
                    return;
                }
            }
            else //다른 곳 클릭 시 다시 선택으로
            {
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                playerActionUI.ActiveUI();
                ResetPreview();
            }
        }
    }
    void BuildWall()
    {
        if (wallCount >= maxWallCount) return;
        if (touchState == ETouchState.Began || touchState == ETouchState.Moved)
            SetPreviewWall();
    }
    public bool BuildComplete()
    {
        if (!playerWallPreview.GetComponent<PreviewWall>().isBlock && playerWallPreview.tag != "CantBuild" && playerWallPreview.activeInHierarchy) //갇혀있거나 겹쳐있거나 비활성화 되어있지않다면
        {
            GameObject playerWall = wallStorage.transform.GetChild(wallCount).gameObject; // 벽설치
            playerWall.SetActive(true);
            playerWall.transform.position = playerWallPreview.transform.position;
            playerWall.transform.rotation = playerWallPreview.transform.rotation;
            GameManager.playerPosition = transform.position / GameManager.gridSize;
            tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장
            wallCount++; // 설치한 벽 개수 +1
            canAction = false; // 이동이나 벽 설치 불가
            shouldBuild = false;
            return true;
        }
        else return false;
    }
    protected virtual bool? Attack()
    {
        bool? isDead = null;
        SetPreviewAttack();
        SetAttackHighlight();
        if (touchState == ETouchState.Ended) //화면 클릭시
        {
            RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerAttackPreview")) // 클릭좌표에 플레이어공격미리보기가 있다면
                {
                    foreach (var attackPosition in attackPositions)
                    {
                        Vector2 direction = (Vector2)(previewHit.transform.position - transform.position).normalized;
                        RaycastHit2D enemyHit = Physics2D.Raycast((Vector3)touchPosition + Quaternion.AngleAxis(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, Vector3.forward) * (Vector3)(Vector2)attackPosition * GameManager.gridSize, transform.forward, 15f, LayerMask.GetMask("Token"));
                        if (enemyHit)
                        {
                            if (enemyHit.transform.CompareTag("Enemy"))
                            {
                                Enemy enemy = enemyHit.transform.GetComponent<Enemy>();

                                //적 체력 줄이기, 사망처리 모두 Enemy에서 관리하도록 수정 (이규빈)
                                isDead = isDead == null ? enemy.AttackedEnemy(atk) : (bool)isDead || enemy.AttackedEnemy(atk);
                                //enemy.hp -= atk;
                                //if (enemy.hp <= 0) enemy.DieEnemy();
                                Debug.Log($"{enemyHit.transform.name}의 현재 체력 {enemy.hp}");
                                canAttack = false;
                                playerActionUI.ActiveUI(); //플레이어 행동 UI 등장 애니메이션
                                playerAbility.PostAttackEvent((bool)isDead, enemy);
                            }
                        }
                    }
                }
            }
            else //다른데 클릭하면 다시 선택화면으로
            {
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                playerActionUI.ActiveUI();
                ResetPreview();
            }
        }
        return isDead;
    }
    void UseAbility()
    {
        IActiveAbility activeAbility = playerAbility.abilities[playerAbility.abilitiesID.IndexOf(usingAbilityID)] as IActiveAbility;
        RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
        if (activeAbility.attackRange.Count == 0)
        {
            List<Vector2Int> attackRange = allPositions.Select(position => position - GameManager.ChangeCoord(transform.position)).ToList();

            SetPreviewAbility(attackRange, activeAbility.attackScale, activeAbility.canPenetrate[0]);
            SetAbilityHighlight(activeAbility.attackScale, activeAbility.canPenetrate[1]);
        }
        else
        {
            SetPreviewAbility(activeAbility.attackRange, activeAbility.attackScale, activeAbility.canPenetrate[0]);
            SetAbilityHighlight(activeAbility.attackScale, activeAbility.canPenetrate[1]);
        }
        if (touchState == ETouchState.Ended) //화면 클릭시
        {
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerAttackPreview")) // 클릭좌표에 플레이어공격미리보기가 있다면
                {
                    activeAbility.targetPos = new Vector2Int(Mathf.RoundToInt(previewHit.transform.position.x / GameManager.gridSize), Mathf.RoundToInt(previewHit.transform.position.y / GameManager.gridSize));
                    (activeAbility as IAbility).Event();
                    usingAbilityID = 0;
                    gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                    playerActionUI.ActiveUI();
                    ResetPreview();
                }
            }
            else //다른데 클릭하면 다시 선택화면으로
            {
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                playerActionUI.ActiveUI();
                ResetPreview();
            }
        }
    }
    // 미리보기 벽 설치
    void SetPreviewWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Ground"));
        // Debug.DrawRay(Camera.main.ScreenToWorldPoint(touchPosition), transform.forward * 15f, Color.red, 0.1f);

        if (hit) // 마우스 위치가 땅 위라면
        {
            // Debug.Log(Mathf.Floor((touchPosition / GameManager.gridSize).x));
            Vector2 touchGridPosition = touchPosition / GameManager.gridSize;
            float[] touchPosFloor = { Mathf.Floor(touchGridPosition.x), Mathf.Floor(touchGridPosition.y) }; // 벽 좌표
            if (touchState == ETouchState.Began)
            {
                tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장
                playerWallPreview.SetActive(false); // 비활성화
                if (touchPosFloor[0] < -4 || touchPosFloor[0] > 3 || touchPosFloor[1] < -4 || touchPosFloor[1] > 3) // 벽 좌표가 땅 밖이라면
                {
                    playerWallPreview.SetActive(false); // 비활성화
                    return;
                }
                if (Mathf.Abs(Mathf.Round(touchGridPosition.x) - touchGridPosition.x) > 0.2f) // 마우스 x 위치가 일정 범위 안이면
                {
                    playerWallPreview.transform.position = GameManager.gridSize * new Vector3(Mathf.Floor(touchGridPosition.x) + 0.5f, Mathf.Floor(touchGridPosition.y) + 0.5f, 0);
                    playerWallPreview.transform.rotation = Quaternion.Euler(0, 0, 0); // 위치 이동 및 회전
                                                                                      // 벽 위치 정보, 회전 정보 저장
                    wallStartPos.x = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).x);
                    wallStartPos.y = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).y);
                    // playerWallPreview.SetActive(true); // 활성화
                }
                else if (Mathf.Abs(Mathf.Round(touchGridPosition.y) - touchGridPosition.y) > 0.2f) // 마우스 y 위치가 일정 범위 안이면
                {
                    playerWallPreview.transform.position = GameManager.gridSize * new Vector3(Mathf.Floor(touchGridPosition.x) + 0.5f, Mathf.Floor(touchGridPosition.y) + 0.5f, 0);
                    playerWallPreview.transform.rotation = Quaternion.Euler(0, 0, 90);// 위치 이동 및 회전
                                                                                      // 벽 위치 정보, 회전 정보 저장
                    wallStartPos.x = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).x);
                    wallStartPos.y = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).y);

                    // playerWallPreview.SetActive(true); // 활성화
                }
                else // 그 외일땐
                {
                    playerWallPreview.SetActive(false); //비활성화
                    touchState = ETouchState.Began;
                }
                // Debug.Log(wallStartPos);
            }
            else if (touchState == ETouchState.Moved)
            {
                if (wallStartPos == new Vector2(-50, -50)) return;
                Vector2 wallVector = touchGridPosition - wallStartPos;
                float wallLength = wallVector.magnitude;
                if (wallLength >= minWallLength)
                {
                    if (Mathf.Abs(wallVector.y / wallVector.x) < 1)
                    {
                        if (wallVector.x >= 0) // 우향
                        {
                            wallInfo[0] = Mathf.FloorToInt(wallStartPos.x + 1);
                            wallInfo[1] = Mathf.FloorToInt(wallStartPos.y);
                            wallInfo[2] = 1;
                        }
                        else // 좌향
                        {
                            wallInfo[0] = Mathf.FloorToInt(wallStartPos.x - 1);
                            wallInfo[1] = Mathf.FloorToInt(wallStartPos.y);
                            wallInfo[2] = 1;
                        }
                    }
                    else
                    {
                        if (wallVector.y >= 0) // 상향
                        {
                            wallInfo[0] = Mathf.FloorToInt(wallStartPos.x);
                            wallInfo[1] = Mathf.FloorToInt(wallStartPos.y + 1);
                            wallInfo[2] = 0;
                        }
                        else //하향
                        {
                            wallInfo[0] = Mathf.FloorToInt(wallStartPos.x);
                            wallInfo[1] = Mathf.FloorToInt(wallStartPos.y - 1);
                            wallInfo[2] = 0;
                        }
                        playerWallPreview.transform.position = new Vector3(wallInfo[0] + 0.5f, wallInfo[1] + 0.5f, 0) * GameManager.gridSize;
                        playerWallPreview.transform.rotation = Quaternion.Euler(0, 0, wallInfo[2] * 90);
                        playerWallPreview.SetActive(true);
                    }
                    if (wallInfo[0] < -4 || wallInfo[0] > 3 || wallInfo[1] < -4 || wallInfo[1] > 3) // 벽 좌표가 땅 밖이라면
                    {
                        playerWallPreview.SetActive(false); // 비활성화
                        return;
                    }
                }
            }
            // Debug.Log($"{wallInfo[0]}, {wallInfo[1]}");
            if (!wallInfo.SequenceEqual(previousWallInfo)) // 벽 위치가 바뀐다면
            {
                Debug.Log($"{wallInfo[0]}, {wallInfo[1]}, {wallInfo[2]}");
                gameManager.mapGraph = (int[,])tempMapGraph.Clone(); // 맵 그래프 원상태로
                if (wallInfo[2] == 0) // 세로 벽이면
                {
                    int wallGraphPosition = (wallInfo[1] + 4) * 9 + wallInfo[0] + 4; // 벽좌표를 그래프좌표로 변환
                    // 벽 넘어로 못넘어가게 그래프에서 설정
                    gameManager.mapGraph[wallGraphPosition, wallGraphPosition + 1] = 0;
                    gameManager.mapGraph[wallGraphPosition + 1, wallGraphPosition] = 0;
                    gameManager.mapGraph[wallGraphPosition + 9, wallGraphPosition + 10] = 0;
                    gameManager.mapGraph[wallGraphPosition + 10, wallGraphPosition + 9] = 0;
                }
                if (wallInfo[2] == 1) // 가로 벽이면
                {
                    int wallGraphPosition = (wallInfo[1] + 4) * 9 + wallInfo[0] + 4;// 벽좌표를 그래프좌표로 변환
                    // 벽 넘어로 못넘어가게 그래프에서 설정
                    gameManager.mapGraph[wallGraphPosition, wallGraphPosition + 9] = 0;
                    gameManager.mapGraph[wallGraphPosition + 9, wallGraphPosition] = 0;
                    gameManager.mapGraph[wallGraphPosition + 1, wallGraphPosition + 10] = 0;
                    gameManager.mapGraph[wallGraphPosition + 10, wallGraphPosition + 1] = 0;
                }
                // gameManager.DebugMap();
                playerWallPreview.GetComponent<PreviewWall>().isBlock = !gameManager.CheckStuck(); // Stuck 결과를 벽미리보기로 전송
            }
            previousWallInfo = (int[])wallInfo.Clone(); // 현재 벽정보를 이전벽정보로 저장
        }
    }
    // 플레이어 미리보기 설정
    void SetPreviewPlayer()
    {
        for (int i = 0; i < movablePositions.Count; i++)
        {
            RaycastHit2D tokenHit = Physics2D.RaycastAll(transform.position, ((Vector2)movablePositions[i]).normalized, GameManager.gridSize * movablePositions[i].magnitude, LayerMask.GetMask("Token")).OrderBy(h => h.distance).Where(h => h.transform.tag == "Enemy").FirstOrDefault(); // 적에 의해 완전히 막힘

            bool[] result = CheckRay(transform.position, (Vector2)movablePositions[i]);
            if (result[0])
            {
                playerPreviews[i].SetActive(false);
                continue;
            }
            if (result[1])
            {
                if (!tokenHit)
                {
                    Debug.DrawRay(transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.green, 0.1f);
                    playerPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)movablePositions[i];
                    playerPreviews[i].SetActive(true);
                }
                else
                {
                    Debug.DrawRay(transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.yellow, 0.1f);
                }
                continue;
            }
            else
            {
                Debug.DrawRay(transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.red, 0.1f);
            }
        }
    }
    // 공격 미리보기
    void SetPreviewAttack()
    {
        for (int i = 0; i < attackablePositions.Count; i++)
        {
            bool canSetPreview = false;
            bool isOuterWall = true;
            for (int h = 0; h < attackPositions.Count; h++)
            {
                Vector2 direction = Quaternion.AngleAxis(Mathf.Atan2(attackablePositions[i].y, attackablePositions[i].x) * Mathf.Rad2Deg, Vector3.forward) * (Vector2)attackPositions[h];

                bool[] result = CheckRay(transform.position, (Vector2)attackablePositions[i] + direction);
                if (!result[0]) isOuterWall = false;
                if (result[1])
                {
                    canSetPreview = true;
                    playerAttackPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)attackablePositions[i];
                    playerAttackPreviews[i].GetComponent<SpriteRenderer>().color = Color.red;
                    playerAttackPreviews[i].GetComponent<BoxCollider2D>().enabled = true;
                    playerAttackPreviews[i].SetActive(true);
                    break;
                }
            }
            if (isOuterWall)
            {
                playerAttackPreviews[i].SetActive(false);
                continue;
            }
            if (canSetPreview) continue;
            playerAttackPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)attackablePositions[i];
            playerAttackPreviews[i].GetComponent<SpriteRenderer>().color = Color.grey;
            playerAttackPreviews[i].GetComponent<BoxCollider2D>().enabled = false;
            playerAttackPreviews[i].SetActive(true);
        }
    }
    void SetAttackHighlight()
    {
        RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
        if (!previewHit)
        {
            foreach (var highlight in playerAttackHighlights) highlight.SetActive(false);
            return;
        }
        for (int i = 0; i < attackPositions.Count; i++)
        {
            Vector2 atkDirection = ((Vector2)(previewHit.transform.position - transform.position) / GameManager.gridSize).normalized;
            Vector2 direction = Quaternion.AngleAxis(Mathf.Atan2(atkDirection.y, atkDirection.x) * Mathf.Rad2Deg, Vector3.forward) * (Vector2)attackPositions[i];
            // Debug.Log((atkDirection + direction).normalized);

            bool[] result = CheckRay(transform.position, atkDirection + direction);
            if (result[0])
            {
                playerAttackHighlights[i].SetActive(false);
                continue;
            }
            playerAttackHighlights[i].transform.position = previewHit.transform.position + GameManager.gridSize * new Vector3(Mathf.Round(direction.x), Mathf.Round(direction.y), 0);
            playerAttackHighlights[i].SetActive(true);
            Debug.DrawRay(transform.position, (atkDirection + direction).normalized * (previewHit.transform.position - transform.position).magnitude, playerAttackHighlights[i].GetComponent<SpriteRenderer>().color, 0.1f);
            if (result[1])
            {
                playerAttackHighlights[i].GetComponent<SpriteRenderer>().color = Color.cyan;
                continue;
            }
            else
            {
                playerAttackHighlights[i].GetComponent<SpriteRenderer>().color = Color.grey;
            }

        }
    }
    void SetPreviewAbility(List<Vector2Int> abilityRange, List<Vector2Int> abilityScale, bool isPenetration)
    {
        for (int i = 0; i < abilityRange.Count; i++)
        {
            bool canSetPreview = false;
            bool isOuterWall = true;
            for (int h = 0; h < abilityScale.Count; h++)
            {
                Vector2 direction = (Vector2)abilityScale[h];

                bool[] result = CheckRay(transform.position, (Vector2)abilityRange[i] + direction);
                if (!result[0]) isOuterWall = false;
                if (result[1] || isPenetration)
                {
                    canSetPreview = true;
                    playerAbilityPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)abilityRange[i];
                    playerAbilityPreviews[i].GetComponent<SpriteRenderer>().color = Color.red;
                    playerAbilityPreviews[i].GetComponent<BoxCollider2D>().enabled = true;
                    playerAbilityPreviews[i].SetActive(true);
                    break;
                }
            }
            if (isOuterWall)
            {
                playerAbilityPreviews[i].SetActive(false);
                continue;
            }
            if (canSetPreview) continue;
            playerAbilityPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)abilityRange[i];
            playerAbilityPreviews[i].GetComponent<SpriteRenderer>().color = Color.grey;
            playerAbilityPreviews[i].GetComponent<BoxCollider2D>().enabled = false;
            playerAbilityPreviews[i].SetActive(true);
        }
    }
    void SetAbilityHighlight(List<Vector2Int> abilityScale, bool isPenetration)
    {
        RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
        if (!previewHit)
        {
            foreach (var highlight in playerAbilityHighlights) highlight.SetActive(false);
            return;
        }
        for (int i = 0; i < abilityScale.Count; i++)
        {
            Vector2 direction = (Vector2)abilityScale[i];

            bool[] result = CheckRay(previewHit.transform.position, direction);
            if (result[0])
            {
                playerAbilityHighlights[i].SetActive(false);
                continue;
            }
            playerAbilityHighlights[i].transform.position = previewHit.transform.position + GameManager.gridSize * new Vector3(Mathf.Round(direction.x), Mathf.Round(direction.y), 0);
            playerAbilityHighlights[i].SetActive(true);
            Debug.DrawRay(previewHit.transform.position, direction, playerAbilityHighlights[i].GetComponent<SpriteRenderer>().color, 0.1f);
            if (result[1] || isPenetration)
            {
                playerAbilityHighlights[i].GetComponent<SpriteRenderer>().color = Color.cyan;
                continue;
            }
            else
            {
                playerAbilityHighlights[i].GetComponent<SpriteRenderer>().color = Color.grey;
            }

        }
    }
    public bool[] CheckRay(Vector3 start, Vector3 direction) // return [isOuterWall, canSetPreview]
    {
        RaycastHit2D outerWallHit = Physics2D.Raycast(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("OuterWall")); // 외벽에 의해 완전히 막힘
        RaycastHit2D wallHit = Physics2D.Raycast(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
        RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘
        bool fullBlock = false;
        // Debug.Log($"{(bool)tokenHit} - {(tokenHit ? tokenHit.collider.gameObject.name : i)}");
        if (outerWallHit)
        {
            return new bool[] { true, false };
        }
        if (!wallHit)
        { // 벽에 의해 완전히 막히지 않았고
            for (int j = 0; j < semiWallHit.Length; j++)
            { // 반벽이 2개가 겹쳐있을 경우에
                for (int k = j + 1; k < semiWallHit.Length; k++)
                {
                    float wallDistance = Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance);
                    if (wallDistance > 0.1f) continue;
                    if (semiWallHit[j].transform.rotation == semiWallHit[k].transform.rotation || Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance) < 0.000001f)
                    {
                        fullBlock = true; // 완전 막힘으로 처리
                        break;
                    }
                }
                if (fullBlock) break;
            }
            if (!fullBlock)
            { // 완전 막히지 않았고 적이 공격 범주에 있다면 공격한다.
                return new bool[] { false, true };
            }
        }
        return new bool[] { false, false };
    }
    public void ResetPreview()
    {
        // 미리보기들 비활성화
        for (int i = 0; i < playerPreviews.Count; i++)
        {
            playerPreviews[i].SetActive(false);
        }
        playerWallPreview.SetActive(false);
        for (int i = 0; i < playerAttackPreviews.Count; i++)
        {
            playerAttackPreviews[i].SetActive(false);
        }
        for (int i = 0; i < playerAttackHighlights.Count; i++)
        {
            playerAttackHighlights[i].SetActive(false);
        }
        foreach (GameObject preview in playerAbilityPreviews)
        {
            preview.SetActive(false);
        }
        foreach (GameObject highlight in playerAbilityHighlights)
        {
            highlight.SetActive(false);
        }
    }
    public virtual void Die()
    {
        if (playerAbility.DieEvent()) Destroy(this.gameObject);
    }
}
