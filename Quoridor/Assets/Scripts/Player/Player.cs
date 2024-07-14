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
using HM.Utils;
using HM.Physics;

public class Player : MonoBehaviour
{
    protected GameManager gameManager;

    public TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None;
    public Vector2 touchPosition;

    [SerializeField]
    public List<Vector2Int> movablePositions = new List<Vector2Int>(); // 플레이어의 가능한 이동 좌표들
    [SerializeField]
    public List<int[]> moveIndex = new List<int[]>(); // 플레이어 이동 동적 할당 GameManager.playerMoveCordinates 참고
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
    public int playerIndex; // 다수의 플레이어가 있을 경우 나누는 순서

    // player Status //
    public int atk;

    [SerializeField]
    // public static int wallCount = 0;
    // public static int maxWallCount;
    // public int destroyCount = 0;
    // public static int maxDestroyCount;
    public int moveCtrl = 100;

    int minWallLength = 1;
    Vector2 wallStartPos = new Vector2(-50, -50);
    int[] wallInfo = new int[3]; // 벽 위치 정보, 회전 정보 저장

    public bool shouldReset = true;

    // public bool canAction = true;
    // public bool isMoveBuildTogether = true;
    // public int moveCount = 1;
    // public int buildCount = 1;
    // public int attackCount = 1;
    // public bool canAttack = true;
    private bool bMove = true;
    private bool bAttack = true;
    public bool canMove
    {
        get
        {
            return bMove;
        }
        set
        {
            bMove = value;
        }
    }
    public bool canBuild
    {
        get
        {
            return gameManager.playerWallCount < gameManager.playerMaxBuildWallCount && moveCtrl >= 100;
        }
    }
    public bool canAttack
    {
        get
        {
            return bAttack;
        }
        set
        {
            bAttack = value;
        }
    }
    public bool canDestroy
    {
        get
        {
            return gameManager.playerDestroyedWallCount < gameManager.playerMaxDestroyWallCount && moveCtrl >= 100;
        }
    }

    public int buildInteractionDistance = 100;
    public int destroyInteractionDistance = 100;


    public int abilityCount = 0;
    protected bool canSignAbility = true;
    public int usingAbilityID;
    public bool isDisposableMove = false;

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
    GameObject previewStorage;
    public PlayerActionUI playerActionUI;

    // PlayerAbility playerAbility;

    bool playerTurnStartAnchor = true;

    void Awake()
    {
        playerActionUI = Instantiate(playerPrefabs.actionUI, transform).transform.GetChild(0).GetComponent<PlayerActionUI>();
        playerActionUI.GetComponentInParent<Canvas>().sortingLayerName = "Text"; // 임시 적용
    }
    void Start()
    {
        gameManager = GameManager.Instance;
        wallStorage = GameObject.FindGameObjectWithTag("WallStorage");
        previewStorage = GameObject.FindGameObjectWithTag("PreviewStorage");
        // GameManager.playerGridPositionList.Add(GameManager.ChangeCoord(transform.position));
        for (int i = 0; i < movablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerPreviews.Add(Instantiate(playerPrefabs.playerPreview, previewStorage.transform));
            playerPreviews[i].SetActive(false);
        }
        for (int i = 0; i < attackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackPreviews.Add(Instantiate(playerPrefabs.attackPreview, previewStorage.transform));
            playerAttackPreviews[i].SetActive(false);
        }
        for (int i = 0; i < attackPositions.Count; i++) // 플레이어 공격 하이라이트 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackHighlights.Add(Instantiate(playerPrefabs.attackHighlight, previewStorage.transform));
            playerAttackHighlights[i].SetActive(false);
        }
        for (int i = 0; i < 81; i++) // 플레이어 능력 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityPreviews.Add(Instantiate(playerPrefabs.attackPreview, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
            playerAbilityPreviews[i].transform.parent = previewStorage.transform;
            playerAbilityPreviews[i].SetActive(false);
        }
        for (int i = 0; i < 81; i++) // 플레이어 능력 하이라이트 -> 미리소환하여 비활성화 해놓기
        {
            playerAbilityHighlights.Add(Instantiate(playerPrefabs.attackHighlight, new Vector3(i % 9 - 4, i / 9 - 4, 0) * GameManager.gridSize, Quaternion.identity));
            playerAbilityHighlights[i].transform.parent = previewStorage.transform;
            playerAbilityHighlights[i].SetActive(false);
        }
        for (int i = 0; i < gameManager.playerMaxBuildWallCount; i++)
        {
            Instantiate(playerPrefabs.wall, wallStorage.transform).SetActive(false);
        }
        for (int i = 0; i < 81; i++)
        {
            allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        }
        playerWallPreview = Instantiate(playerPrefabs.wallPreview, transform.position, Quaternion.identity); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
        playerWallPreview.SetActive(false);

        //playerUI = Instantiate(playerUI); // [디버그용]
        // abilityUI = Instantiate(abilityUI); // [임시 능력 UI]
        // playerAbility = GetComponent<PlayerAbility>();
        // playerAbility.LoadAbility();
    }
    public void Initialize()
    {
        //transform.position = GameManager.gridSize * new Vector3(0, -4, 0);
        gameManager.playerWallCount = 0;

        for (int i = 0; i < wallStorage.transform.childCount; i++)
        {
            wallStorage.transform.GetChild(i).gameObject.SetActive(false);
        }

        Reset();

        previousWallInfo = new int[3];
    }
    // Update is called once per frame
    void Update()
    {
        TouchUtil.TouchSetUp(ref touchState, ref touchPosition);
        // Debug.Log(touchState.ToString());
        // playerAbility.ResetEvent(PlayerAbility.EResetTime.OnEveryTick);
        // if (playerAbility.NeedSave)
        // playerAbility.SaveAbility();
        GameManager.playerGridPositionList[playerIndex] = GameManager.ChangeCoord(transform.position);
        if (GameManager.Turn % 2 == playerOrder && gameManager.player == gameObject) // 플레이어 차례인지 확인
        {
            if (playerTurnStartAnchor)
            {
                // playerAbility.ResetEvent(PlayerAbility.EResetTime.OnPlayerTurnStart);
                // tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장

                playerTurnStartAnchor = false;
            }
            // playerUI.SetActive(true); // [디버그용]
            // {
            //     Transform canvas = playerUI.transform.GetChild(0);
            //     canvas.GetChild(5).GetComponent<Text>().text = $"{maxWallCount - wallCount}/{maxWallCount}";
            //     // [디버그용] //
            //     canvas.GetChild(1).GetComponent<Button>().interactable = canAction || buildCount > 0 || moveCtrl == 100;  // 건설 버튼
            //     canvas.GetChild(2).GetComponent<Button>().interactable = canAction || moveCount > 0 || moveCtrl == 100;   // 이동 버튼
            //     canvas.GetChild(0).GetComponent<Button>().interactable = canAttack || moveCtrl == 100;                 // 공격 버튼
            //     canvas.GetChild(3).GetComponent<Button>().interactable = abilityCount == 0;         // 능력 버튼
            // }
            if (abilityCount > 0)
            {   //[임시 능력 UI]
                abilityUI.SetActive(true);
                // playerAbility.ActiveEvent();
            }

            touchPosition = Camera.main.ScreenToWorldPoint(touchPosition); //카메라에 찍힌 좌표를 월드좌표로
            switch (gameManager.playerControlStatus)
            {
                case GameManager.EPlayerControlStatus.Move:
                    if (canMove) MovePlayer();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Build:
                    if (canBuild) BuildWall();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Attack:
                    if (canAttack) Attack();
                    else ResetPreview();
                    break;
                // case GameManager.EPlayerControlStatus.Ability:
                //     if (abilityCount > 0) UseAbility();
                //     else ResetPreview();
                //     break;
                case GameManager.EPlayerControlStatus.Destroy:
                    if (canDestroy) Destroy();
                    else ResetPreview();
                    break;

                default:
                    break;
            }
        }
        else // 플레이어 차례가 아니면
        {
            if (shouldReset)
            {
                Reset();
            }
        }
    }
    protected virtual void Reset()
    {
        playerUI.SetActive(false); // [디버그용]
        abilityUI.SetActive(false); // [임시 능력 UI]
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
        // canAction = true;
        canAttack = true;
        // moveCount = 1;
        // buildCount = 1;
        // attackCount = 1;
        MoveCtrlUpCount();
        ResetPreview();
        // isMoveBuildTogether = true;
        // playerAbility.ResetEvent(PlayerAbility.EResetTime.OnEnemyTurnStart);

        shouldReset = false;
        playerTurnStartAnchor = true;
    }
    #region MOVE
    void MoveCtrlUpCount()
    {
        if (moveCtrl < 100) moveCtrl += 50;
        if (moveCtrl > 100) moveCtrl = 100;
    }

    void PlayerMoveSetup()
    {
        for(int moveCount = 0; moveCount < moveIndex.Count; moveCount++)
        {
            
        }
    }

    void MovePlayer()
    {
        SetPreviewPlayer();
        if (touchState == TouchUtil.ETouchState.Began) //화면 클릭시
        {
            RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerPreview")) // 클릭좌표에 플레이어미리보기가 있다면
                {
                    // if (moveCount <= 0)
                    // {
                    //     moveCtrl = 0;
                    // }
                    transform.position = previewHit.transform.position; //플레이어 위치 이동
                    GameManager.playerGridPositionList[playerIndex] = GameManager.ChangeCoord(transform.position); //플레이어 위치정보 저장
                    // moveCount--;
                    // if (isMoveBuildTogether)
                    // {
                    //     buildCount--;
                    //     isMoveBuildTogether = false;
                    // }
                    // if (!isDisposableMove)
                    // {
                    //     if (moveCtrl != 100) canAction = false; // 이동이나 벽 설치 불가
                    // }
                    // else isDisposableMove = false;
                    // playerAbility.MoveEvent();
                    canMove = false;
                    if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.Move) gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                    playerActionUI.ActiveUI(); //플레이어 행동 UI 등장 애니메이션
                    ResetPreview();
                    return;
                }
            }
            else //다른 곳 클릭 시 다시 선택으로
            {
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                // if (isDisposableMove)
                // {
                //     moveCount--;
                //     isDisposableMove = false;
                // }
                playerActionUI.ActiveUI();
                ResetPreview();
            }
        }
    }
    #endregion
    #region BUILD
    void BuildWall()
    {
        if (gameManager.playerWallCount >= gameManager.playerMaxBuildWallCount) return;
        if (touchState == TouchUtil.ETouchState.Began || touchState == TouchUtil.ETouchState.Moved)
            SetPreviewWall();
    }
    public bool BuildComplete()
    {
        if (!playerWallPreview.GetComponent<PreviewWall>().isBlock && playerWallPreview.tag != "CantBuild" && playerWallPreview.activeInHierarchy) //갇혀있거나 겹쳐있거나 비활성화 되어있지않다면
        {
            // if (buildCount <= 0)
            // {
            //     moveCtrl = 0;
            // }
            GameObject playerWall = wallStorage.transform.GetChild(gameManager.playerWallCount).gameObject; // 벽설치
            // playerWall.SetActive(true);
            // playerWall.transform.position = playerWallPreview.transform.position;
            // playerWall.transform.rotation = playerWallPreview.transform.rotation;
            gameManager.wallData.SetWallBasedPreview(playerWallPreview, ref playerWall);
            GameManager.playerGridPositionList[playerIndex] = GameManager.ChangeCoord(transform.position);
            // tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장
            gameManager.playerWallCount++; // 설치한 벽 개수 +1
            moveCtrl -= 100;

            gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
            playerActionUI.ActiveUI();
            ResetPreview();
            // buildCount--;
            // if (isMoveBuildTogether)
            // {
            //     moveCount--;
            //     isMoveBuildTogether = false;
            // }

            // if (moveCtrl != 100) canAction = false; // 이동이나 벽 설치 불가

            return true;
        }
        else return false;
    }
    #endregion
    #region ATTACK
    protected virtual bool? Attack()
    {
        bool? isDead = null;
        SetPreviewAttack();
        SetAttackHighlight();
        if (touchState == TouchUtil.ETouchState.Ended) //화면 클릭시
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
                                // if (attackCount <= 0) moveCtrl = 0;
                                Enemy enemy = enemyHit.transform.GetComponent<Enemy>();

                                //적 체력 줄이기, 사망처리 모두 Enemy에서 관리하도록 수정 (이규빈)
                                isDead = isDead == null ? enemy.AttackedEnemy(atk) : (bool)isDead || enemy.AttackedEnemy(atk);
                                //enemy.hp -= atk;
                                //if (enemy.hp <= 0) enemy.DieEnemy();
                                Debug.Log($"{enemyHit.transform.name}의 현재 체력 {enemy.hp}");
                                // attackCount--;
                                // if (moveCtrl != 100) canAttack = false;
                                // playerAbility.PostAttackEvent((bool)isDead, enemy);
                                canAttack = false;
                                if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.Attack)
                                {
                                    gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                                    playerActionUI.ActiveUI();
                                }
                                ResetPreview();
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
    #endregion
    #region DESTROY
    void Destroy()
    {
        SetPreviewDestroy();
        if (touchState == TouchUtil.ETouchState.Ended)
        {
            RaycastHit2D hit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("WallTouch")); //TODO: 이렇게 하면 벽 터치 UX 그지 같을 예정이므로 나중에 수정해야 할듯.
            if (hit)
            {
                if (MathUtil.GetTaxiDistance(GameManager.ChangeCoord(transform.position), GameManager.ChangeCoord(hit.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize)) > destroyInteractionDistance)
                {
                    Debug.Log("Range Over");
                    return;
                }
                GameObject wallObject = hit.transform.parent.gameObject;
                gameManager.wallData.RemoveWall(ref wallObject);
                wallObject.SetActive(false);
                moveCtrl -= 100;
                gameManager.playerDestroyedWallCount++;
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                playerActionUI.ActiveUI();
                ResetPreview();
            }
            else
            {
                gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
                playerActionUI.ActiveUI();
                ResetPreview();
            }
        }
    }
    #endregion
    #region ABILITY
    // void UseAbility()
    // {
    //     IActiveAbility activeAbility = playerAbility.abilities[playerAbility.abilitiesID.IndexOf(usingAbilityID)] as IActiveAbility;
    //     RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
    //     if (activeAbility.attackRange.Count == 0)
    //     {
    //         List<Vector2Int> attackRange = allPositions.Select(position => position - GameManager.ChangeCoord(transform.position)).ToList();

    //         SetPreviewAbility(attackRange, activeAbility.attackScale, activeAbility.canPenetrate[0]);
    //         SetAbilityHighlight(activeAbility.attackScale, activeAbility.canPenetrate[1]);
    //     }
    //     else
    //     {
    //         SetPreviewAbility(activeAbility.attackRange, activeAbility.attackScale, activeAbility.canPenetrate[0]);
    //         SetAbilityHighlight(activeAbility.attackScale, activeAbility.canPenetrate[1]);
    //     }
    //     if (touchState == TouchUtil.ETouchState.Ended) //화면 클릭시
    //     {
    //         if (previewHit)
    //         {
    //             if (previewHit.transform.CompareTag("PlayerAttackPreview")) // 클릭좌표에 플레이어공격미리보기가 있다면
    //             {
    //                 activeAbility.targetPos = GameManager.ChangeCoord(previewHit.transform.position);
    //                 (activeAbility as IAbility).Event();
    //                 usingAbilityID = 0;
    //                 gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
    //                 playerActionUI.ActiveUI();
    //                 ResetPreview();
    //             }
    //         }
    //         else //다른데 클릭하면 다시 선택화면으로
    //         {
    //             gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
    //             playerActionUI.ActiveUI();
    //             ResetPreview();
    //         }
    //     }
    // }
    #endregion
    #region SET PREVIEW
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
        foreach (var wallObject in gameManager.wallData.wallObjectList)
        {
            wallObject.GetComponent<SpriteRenderer>().color = Color.white;
        }
    }
    #region WALL PREVIEW
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
            if (touchState == TouchUtil.ETouchState.Began)
            {
                // tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장
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
                    touchState = TouchUtil.ETouchState.Began;
                }

                if (MathUtil.GetTaxiDistance(GameManager.ChangeCoord(transform.position), GameManager.ChangeCoord(playerWallPreview.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize)) > buildInteractionDistance)
                {
                    playerWallPreview.SetActive(false);
                    return;
                }
                // Debug.Log(wallStartPos);
            }
            else if (touchState == TouchUtil.ETouchState.Moved)
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
                    }
                    gameManager.wallData.SetWallPreview(wallInfo[0], wallInfo[1], wallInfo[2], ref playerWallPreview);
                    if (MathUtil.GetTaxiDistance(GameManager.ChangeCoord(transform.position), GameManager.ChangeCoord(playerWallPreview.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize)) > buildInteractionDistance)
                    {
                        playerWallPreview.SetActive(false);
                        return;
                    }
                }
            }
            Debug.Log($"{wallInfo[0]}, {wallInfo[1]}");
        }
    }
    #endregion
    #region DESTROY PREVIEW
    void SetPreviewDestroy()
    {
        foreach (var wallObject in gameManager.wallData.wallObjectList)
        {
            if (MathUtil.GetTaxiDistance(GameManager.ChangeCoord(transform.position), GameManager.ChangeCoord(wallObject.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize)) <= destroyInteractionDistance)
            {
                wallObject.GetComponent<SpriteRenderer>().color = Color.red;
            }
            else
            {
                wallObject.GetComponent<SpriteRenderer>().color = Color.white;
            }
        }
    }
    #endregion
    #region PLAYER PREVIEW
    // 플레이어 미리보기 설정
    void SetPreviewPlayer()
    {
        for (int i = 0; i < movablePositions.Count; i++)
        {
            bool[] result = HMPhysics.CheckRay(transform.position, (Vector2)movablePositions[i]);
            if (result[0])
            {
                playerPreviews[i].SetActive(false);
                continue;
            }
            if (result[1])
            {
                if (!result[2])
                {
                    Debug.DrawRay(transform.position, (Vector2)movablePositions[i] * GameManager.gridSize, Color.green, 0.1f);
                    playerPreviews[i].transform.position = transform.position + GameManager.ChangeCoord(movablePositions[i]);
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
    #endregion
    #region ATTACK PREVIEW
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

                bool[] result = HMPhysics.CheckRay(transform.position, (Vector2)attackablePositions[i] + direction);
                if (!result[0]) isOuterWall = false;
                if (result[1])
                {
                    canSetPreview = true;
                    playerAttackPreviews[i].transform.position = transform.position + GameManager.ChangeCoord(attackablePositions[i]);
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
            playerAttackPreviews[i].transform.position = transform.position + GameManager.ChangeCoord(attackablePositions[i]);
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

            bool[] result = HMPhysics.CheckRay(transform.position, atkDirection + direction);
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
    #endregion
    #region ABILITY PREVIEW
    void SetPreviewAbility(List<Vector2Int> abilityRange, List<Vector2Int> abilityScale, bool isPenetration)
    {
        for (int i = 0; i < abilityRange.Count; i++)
        {
            bool canSetPreview = false;
            bool isOuterWall = true;
            for (int h = 0; h < abilityScale.Count; h++)
            {
                Vector2 direction = (Vector2)abilityScale[h];

                bool[] result = HMPhysics.CheckRay(transform.position, (Vector2)abilityRange[i] + direction);
                if (!result[0]) isOuterWall = false;
                if (result[1] || isPenetration)
                {
                    canSetPreview = true;
                    playerAbilityPreviews[i].transform.position = transform.position + GameManager.ChangeCoord(abilityRange[i]);
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
            playerAbilityPreviews[i].transform.position = transform.position + GameManager.ChangeCoord(abilityRange[i]);
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

            bool[] result = HMPhysics.CheckRay(previewHit.transform.position, direction);
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
    #endregion
    #endregion
    public virtual void Die()
    {
        // if (playerAbility.DieEvent()) this.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
    }
}
