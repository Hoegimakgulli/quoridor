using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Android.Types;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    GameManager gameManager;

    public enum ETouchState { None, Began, Moved, Ended, Waiting }; //모바일과 에디터 모두 가능하게 터치 & 마우스 처리
    public ETouchState touchState = ETouchState.None;
    Vector2 touchPosition;

    [SerializeField]
    List<Vector2Int> playerMovablePositions = new List<Vector2Int>(); // 플레이어의 가능한 이동 좌표들
    [SerializeField]
    List<Vector2Int> playerAttackablePositions = new List<Vector2Int>(); // 플레이어의 가능한 공격 좌표들
    [SerializeField]
    GameObject playerPreviewPrefab; // 플레이어 위치 미리보기
    List<GameObject> playerPreviews = new List<GameObject>();
    [SerializeField]
    GameObject playerAttackPreviewPrefab;
    List<GameObject> playerAttackPreviews = new List<GameObject>();
    [SerializeField]
    GameObject playerWallPreviewPrefab; // 플레이어 설치벽 위치 미리보기
    GameObject playerWallPreview;
    [SerializeField]
    GameObject playerWallPrefab; // 플레이어 설치벽
    [SerializeField]
    GameObject historyIndexPrefab; // history 형식으로 저장되는 글 양식

    public int playerOrder = 1; // 플레이어 차례

    [SerializeField]
    int wallCount = 0;
    public int maxWallCount;

    bool canAction = true;
    bool canAttack = true;

    int[] previousWallInfo = new int[3];
    int[,] tempMapGraph = new int[81, 81];

    /*[디버그용]*/
    [SerializeField]
    GameObject playerUI;

    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        transform.position = GameManager.gridSize * gameManager.playerPosition; //플레이어 위치 초기화 (처음위치는 게임메니저에서 설정)
        for (int i = 0; i < playerMovablePositions.Count; i++) // 플레이어 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerPreviews.Add(Instantiate(playerPreviewPrefab, transform.position, Quaternion.identity));
            playerPreviews[i].SetActive(false);
        }
        for (int i = 0; i < playerAttackablePositions.Count; i++) // 플레이어 공격 미리보기 -> 미리소환하여 비활성화 해놓기
        {
            playerAttackPreviews.Add(Instantiate(playerAttackPreviewPrefab, transform.position, Quaternion.identity));
            playerAttackPreviews[i].SetActive(false);
        }
        playerWallPreview = Instantiate(playerWallPreviewPrefab, transform.position, Quaternion.identity); // 플레이어 벽 미리보기 -> 미리소환하여 비활성화 해놓기
        playerWallPreview.SetActive(false);
        tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵그래프 저장

        playerUI = Instantiate(playerUI); // [디버그용]
    }

    // Update is called once per frame
    void Update()
    {
        TouchSetUp();
        if (GameManager.Turn % 2 == playerOrder) // 플레이어 차례인지 확인
        {
            playerUI.SetActive(true); // [디버그용]
            {
                Transform canvas = playerUI.transform.GetChild(0);
                canvas.GetChild(4).GetComponent<Text>().text = $"{maxWallCount - wallCount}/{maxWallCount}";
                if (!canAction) // [디버그용]
                {
                    canvas.GetChild(1).GetComponent<Button>().interactable = false;
                    canvas.GetChild(2).GetComponent<Button>().interactable = false;
                }
                else
                {
                    canvas.GetChild(1).GetComponent<Button>().interactable = true;
                    canvas.GetChild(2).GetComponent<Button>().interactable = true;
                }
                if (!canAttack) // [디버그용]
                {
                    canvas.GetChild(0).GetComponent<Button>().interactable = false;
                }
                else
                {
                    canvas.GetChild(0).GetComponent<Button>().interactable = true;
                }
            }

            touchPosition = Camera.main.ScreenToWorldPoint(touchPosition); //카메라에 찍힌 좌표를 월드좌표로
            switch (gameManager.playerControlStatus)
            {
                case GameManager.EPlayerControlStatus.Move:
                    if (canAction) MovePlayer();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Build:
                    if (canAction) BuildWall();
                    else ResetPreview();
                    break;
                case GameManager.EPlayerControlStatus.Attack:
                    if (canAttack) Attack();
                    else ResetPreview();
                    break;
                default:
                    break;
            }
        }
        else // 플레이어 차례가 아니면
        {
            playerUI.SetActive(false); // [디버그용]
            gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
            canAction = true;
            canAttack = true;
            ResetPreview();
        }
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
                    gameManager.playerPosition = transform.position / GameManager.gridSize; //플레이어 위치정보 저장
                    canAction = false; // 이동이나 벽 설치 불가
                    return;
                }
            }
        }
    }
    void BuildWall()
    {
        if (wallCount >= maxWallCount) return;
        SetPreviewWall();
        if (touchState == ETouchState.Began) //화면 클릭시
        {
            if (!playerWallPreview.GetComponent<PreviewWall>().isBlock && playerWallPreview.tag != "CantBuild" && playerWallPreview.activeInHierarchy) //갇혀있거나 겹쳐있거나 비활성화 되어있지않다면
            {
                Instantiate(playerWallPrefab, playerWallPreview.transform.position, playerWallPreview.transform.rotation); // 벽설치
                gameManager.playerPosition = transform.position / GameManager.gridSize;
                tempMapGraph = (int[,])gameManager.mapGraph.Clone(); // 맵정보 새로저장
                wallCount++; // 설치한 벽 개수 +1
                canAction = false; // 이동이나 벽 설치 불가
                return;
            }

        }
    }
    void Attack()
    {
        SetPreviewAttack();
        if (touchState == ETouchState.Began) //화면 클릭시
        {
            RaycastHit2D previewHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Preview"));
            if (previewHit)
            {
                if (previewHit.transform.CompareTag("PlayerAttackPreview")) // 클릭좌표에 플레이어공격미리보기가 있다면
                {
                    RaycastHit2D enemyHit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Token"));
                    if (enemyHit)
                    {
                        if (enemyHit.transform.CompareTag("Enemy"))
                        {
                            Enemy enemy = enemyHit.transform.GetComponent<Enemy>();

                            Debug.Log($"{enemyHit.transform.name} 적 체력 감소 이벤트!");
                            canAttack = false;
                            return;
                        }
                    }
                }
            }
        }
    }
    // 미리보기 벽 설치
    void SetPreviewWall()
    {
        RaycastHit2D hit = Physics2D.Raycast(touchPosition, transform.forward, 15f, LayerMask.GetMask("Ground"));
        // Debug.DrawRay(Camera.main.ScreenToWorldPoint(touchPosition), transform.forward * 15f, Color.red, 0.1f);
        int[] wallInfo = new int[3]; // 벽 위치 정보, 회전 정보 저장

        if (hit) // 마우스 위치가 땅 위라면
        {
            // Debug.Log(Mathf.Floor((touchPosition / GameManager.gridSize).x));
            float[] touchPosFloor = { Mathf.Floor((touchPosition / GameManager.gridSize).x), Mathf.Floor((touchPosition / GameManager.gridSize).y) }; // 벽 좌표
            if (touchPosFloor[0] < -4 || touchPosFloor[0] > 3 || touchPosFloor[1] < -4 || touchPosFloor[1] > 3) // 벽 좌표가 땅 밖이라면
            {
                playerWallPreview.SetActive(false); // 비활성화
                return;
            }
            if (Mathf.Abs(Mathf.Round((touchPosition / GameManager.gridSize).x) - (touchPosition / GameManager.gridSize).x) > 0.3f) // 마우스 x 위치가 일정 범위 안이면
            {
                playerWallPreview.transform.position = GameManager.gridSize * new Vector3(Mathf.Floor((touchPosition / GameManager.gridSize).x) + 0.5f, Mathf.Floor((touchPosition / GameManager.gridSize).y) + 0.5f, 0);
                playerWallPreview.transform.rotation = Quaternion.Euler(0, 0, 0); // 위치 이동 및 회전
                // 벽 위치 정보, 회전 정보 저장
                wallInfo[0] = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).x);
                wallInfo[1] = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).y);
                wallInfo[2] = 0;
                playerWallPreview.SetActive(true); // 활성화
            }
            else if (Mathf.Abs(Mathf.Round((touchPosition / GameManager.gridSize).y) - (touchPosition / GameManager.gridSize).y) > 0.3f) // 마우스 y 위치가 일정 범위 안이면
            {
                playerWallPreview.transform.position = GameManager.gridSize * new Vector3(Mathf.Floor((touchPosition / GameManager.gridSize).x) + 0.5f, Mathf.Floor((touchPosition / GameManager.gridSize).y) + 0.5f, 0);
                playerWallPreview.transform.rotation = Quaternion.Euler(0, 0, 90);// 위치 이동 및 회전
                // 벽 위치 정보, 회전 정보 저장
                wallInfo[0] = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).x);
                wallInfo[1] = Mathf.FloorToInt((playerWallPreview.transform.position / GameManager.gridSize).y);
                wallInfo[2] = 1;
                playerWallPreview.SetActive(true); // 활성화
            }
            else // 그 외일땐
            {
                playerWallPreview.SetActive(false); //비활성화
            }
            // Debug.Log($"{wallInfo[0]}, {wallInfo[1]}");
            if (!wallInfo.SequenceEqual(previousWallInfo)) // 벽 위치가 바뀐다면
            {
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
        for (int i = 0; i < playerMovablePositions.Count; i++)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, ((Vector2)playerMovablePositions[i]).normalized, GameManager.gridSize * playerMovablePositions[i].magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
            RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(transform.position, ((Vector2)playerMovablePositions[i]).normalized, GameManager.gridSize * playerMovablePositions[i].magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘
            RaycastHit2D tokenHit = Physics2D.RaycastAll(transform.position, ((Vector2)playerMovablePositions[i]).normalized, GameManager.gridSize * playerMovablePositions[i].magnitude, LayerMask.GetMask("Token")).OrderBy(h => h.distance).Where(h => h.transform.tag == "Enemy").FirstOrDefault(); // 적에 의해 완전히 막힘
            bool fullBlock = false;
            // Debug.Log($"{(bool)tokenHit} - {(tokenHit ? tokenHit.collider.gameObject.name : i)}");
            if (!wallHit)
            { // 벽에 의해 완전히 막히지 않았고
                for (int j = 0; j < semiWallHit.Length; j++)
                { // 반벽이 2개가 겹쳐있을 경우에
                    for (int k = j + 1; k < semiWallHit.Length; k++)
                    {
                        if (Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance) < 0.000001f)
                        {
                            fullBlock = true; // 완전 막힘으로 처리
                            break;
                        }
                    }
                    if (fullBlock) break;
                }
                if (!fullBlock)
                { // 완전 막히지 않았다면 플레이어 미리보기 활성화
                    if (!tokenHit)
                    {
                        Debug.DrawRay(transform.position, (Vector2)playerMovablePositions[i] * GameManager.gridSize, Color.green, 0.1f);
                        playerPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)playerMovablePositions[i];
                        playerPreviews[i].SetActive(true);
                    }
                    else
                    {
                        Debug.DrawRay(transform.position, (Vector2)playerMovablePositions[i] * GameManager.gridSize, Color.yellow, 0.1f);
                    }
                }
            }
            else Debug.DrawRay(transform.position, (Vector2)playerMovablePositions[i] * GameManager.gridSize, Color.red, 0.1f);
        }
    }
    // 공격 미리보기
    void SetPreviewAttack()
    {
        for (int i = 0; i < playerAttackablePositions.Count; i++)
        {
            RaycastHit2D wallHit = Physics2D.Raycast(transform.position, ((Vector2)playerAttackablePositions[i]).normalized, GameManager.gridSize * playerAttackablePositions[i].magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
            RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(transform.position, ((Vector2)playerAttackablePositions[i]).normalized, GameManager.gridSize * playerAttackablePositions[i].magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘
            RaycastHit2D tokenHit = Physics2D.RaycastAll(transform.position, ((Vector2)playerAttackablePositions[i]).normalized, GameManager.gridSize * playerAttackablePositions[i].magnitude, LayerMask.GetMask("Token")).OrderBy(h => h.distance).Where(h => h.transform.tag == "Enemy").FirstOrDefault(); // 적에 의해 완전히 막힘
            bool fullBlock = false;
            // Debug.Log($"{(bool)tokenHit} - {(tokenHit ? tokenHit.collider.gameObject.name : i)}");
            if (!wallHit)
            { // 벽에 의해 완전히 막히지 않았고
                for (int j = 0; j < semiWallHit.Length; j++)
                { // 반벽이 2개가 겹쳐있을 경우에
                    for (int k = j + 1; k < semiWallHit.Length; k++)
                    {
                        if (Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance) < 0.000001f)
                        {
                            fullBlock = true; // 완전 막힘으로 처리
                            break;
                        }
                    }
                    if (fullBlock) break;
                }
                if (!fullBlock)
                { // 완전 막히지 않았고 적이 공격 범주에 있다면 공격한다.
                    // if (!tokenHit) // 적이 있어야만 하이라이트가 되게?
                    {
                        playerAttackPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)playerAttackablePositions[i];
                        playerAttackPreviews[i].GetComponent<SpriteRenderer>().color = Color.red;
                        playerAttackPreviews[i].SetActive(true);
                        continue;
                    }
                }
            }
            playerAttackPreviews[i].transform.position = transform.position + GameManager.gridSize * (Vector3)(Vector2)playerAttackablePositions[i];
            playerAttackPreviews[i].GetComponent<SpriteRenderer>().color = Color.grey;
            playerAttackPreviews[i].SetActive(true);
        }
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
    }
}
