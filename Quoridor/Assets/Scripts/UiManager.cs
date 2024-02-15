using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static UnityEngine.UI.Image;
using Unity.VisualScripting;

public class UiManager : MonoBehaviour
{
    // panel 관리용 변수
    public static GameObject[] panelBox = new GameObject[2]; // 0 - Turn, 1 - history
    public static GameObject[] historyBox = new GameObject[2]; // 0 - player, 2 - enemy
    public static int turnAnchor = 0; // GameManager에 있는 Turn과 비교하는 비교군

    // 이규빈 생성 변수들
    public GameObject uiCanvas;
    private GameManager gameManager;
    private List<RectTransform> enemyStates = new List<RectTransform>(); //적 상태창들의 RectTransform
    public float uiMoveTime = 0.2f; //적 상태창 움직이는 시간 
    public bool popLock = false; //임시 변수. 플레이어 및 적턴 알려주는 팝업 통제용.
    public List<int> sortingList = new List<int>(); //행동력 순서로 EnemyState를 정렬할 리스트. 각 배열의 숫자는 몇 번째 적인지를 나타냄.
    private GameObject explosionEffect;
    private List<RectTransform> particlesRT = new List<RectTransform>();
    public GameObject turnEndButton;
    public Text WallCountText;


    private void Awake()
    {
        GameObject uiC = Instantiate(uiCanvas);
        turnEndButton = uiC.transform.GetChild(4).gameObject;
        turnEndButton.GetComponent<Button>().onClick.AddListener(() => PlayerTurnEnd());
        WallCountText = uiC.transform.GetChild(5).GetChild(2).GetComponent<Text>();
    }
    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        //playerUI = Instantiate(playerUI); //플레이어 UI 캔버스 소환
        panelBox[0] = GameObject.Find("TurnPanel");
        panelBox[1] = GameObject.Find("HistoryPanel");
        historyBox[0] = panelBox[1].transform.GetChild(0).transform.GetChild(0).gameObject; // History -> playerBox 접근
        historyBox[1] = panelBox[1].transform.GetChild(0).transform.GetChild(1).gameObject; // History -> enemyBox 접근
        explosionEffect = panelBox[0].transform.parent.GetChild(3).GetChild(2).gameObject;
        for(int i = 0; i < explosionEffect.transform.childCount; i++)
        {
            particlesRT.Add(explosionEffect.transform.GetChild(i).GetComponent<RectTransform>());
        }
        explosionEffect.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Turn % 2 == 0) // 패널 관리 파트
        {
            StartCoroutine(EnemyPanelPop());
        }

        if (GameManager.Turn % 2 == 1)
        {
            StartCoroutine(PlayerPanelPop());
        }
    }

    public void HistoryPanelPop() // history panel 열고 닫기 함수
    {
        if (!panelBox[1].transform.GetChild(0).gameObject.activeSelf) // History Panel Active == false
        {
            panelBox[1].transform.GetChild(0).gameObject.SetActive(true);
        }
        else // History Panel Active == true
        {
            panelBox[1].transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    public static void InputPlayerMoveHistory(Vector3 beforePos, Vector3 currentPos, GameObject historyIndex)
    {
        GameObject playerHistoryContent = historyBox[0];
        while (playerHistoryContent.name != "Content") // player Content 찾아서 넣어주기
        {
            playerHistoryContent = playerHistoryContent.transform.GetChild(0).gameObject;
        }

        Debug.Log("player move history");
        GameObject indexObj = Instantiate(historyIndex, new Vector3(0, 0, 0), Quaternion.identity, playerHistoryContent.transform);
        indexObj.transform.GetChild(0).GetComponent<Text>().text = "턴 " + (GameManager.Turn / 2 + 1); // 턴 표시
        indexObj.transform.GetChild(1).GetComponent<Text>().text = "이동"; // 이동 or 벽설치 표시
        indexObj.transform.GetChild(2).GetComponent<Text>().text = "" + (char)(beforePos.x + 69) + ((beforePos.y - 4) * -1) + "→" + (char)(currentPos.x + 69) + ((currentPos.y - 4) * -1); // 65 - A 에서 아스키코드값 + 좌표값으로 문자 출력
    }

    public static void InputPlayerWallHistory(Vector3 wallPos, Quaternion wallRot, GameObject historyIndex)
    {
        GameObject playerHistoryContent = historyBox[0];
        while (playerHistoryContent.name != "Content") // player Content 찾아서 넣어주기
        {
            playerHistoryContent = playerHistoryContent.transform.GetChild(0).gameObject;
        }

        Debug.Log("player wall history");
        GameObject indexObj = Instantiate(historyIndex, new Vector3(0, 0, 0), Quaternion.identity, playerHistoryContent.transform);
        indexObj.transform.GetChild(0).GetComponent<Text>().text = "턴 " + (GameManager.Turn / 2); // 턴 표시
        indexObj.transform.GetChild(1).GetComponent<Text>().text = "벽설치"; // 이동 or 벽설치 표시
        indexObj.transform.GetChild(2).GetComponent<Text>().text = "아";
    }


    public static IEnumerator EnemyPanelPop() // TurnPanel child → 0 = player, 1 = enemy
    {
        if (turnAnchor != GameManager.Turn)
        {
            turnAnchor = GameManager.Turn;
            panelBox[0].transform.GetChild(1).gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            panelBox[0].transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    public static IEnumerator PlayerPanelPop() // TurnPanel child → 0 = player, 1 = enemy
    {
        if (turnAnchor != GameManager.Turn)
        {
            turnAnchor = GameManager.Turn;
            panelBox[0].transform.GetChild(0).gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            panelBox[0].transform.GetChild(0).gameObject.SetActive(false);
        }
    }




    //적 상태창이 전부 사라질 때 꼭 호출
    public void ResetEnemyStates()
    {
        for(int i = 0; i < enemyStates.Count; i++)
        {
            Destroy(enemyStates[0]);
        }
        enemyStates.Clear();
        sortingList.Clear();

    }


    //매개변수로 받아온 적의 상태창 생성 (받아온 적의 상태창 오브젝트, 받아온 적의 오브젝트, 받아온 적의 Enemy 스크립트, 받아온 적이 몇번째로 소환된 적인지)
    public void CreateEnemyState(GameObject currentEnemyState, GameObject currentEnemyObj, Enemy currentEnemey, int enemyNum)
    {
        //상태창 이미지, 수치들을 바꿈
        currentEnemyState.transform.GetChild(3).GetComponent<Image>().DOFade(0, 0); //맞았을 때 빨간색으로 깜빡이는 Panel을 투명하게
        currentEnemyState.transform.GetChild(4).GetComponent<Image>().DOFade(0, 0); //상태창 하이라이팅 Panel을 투명하게
        currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentEnemyObj.GetComponent<SpriteRenderer>().sprite;
        currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().color = currentEnemyObj.GetComponent<SpriteRenderer>().color;
        currentEnemyState.transform.GetChild(1).GetComponent<Text>().text = "행동력 " + currentEnemey.moveCtrl[1] + " / 10";
        currentEnemey.maxHp = currentEnemey.hp;
        currentEnemyState.transform.GetChild(2).GetComponent<Text>().text = "체력 " + currentEnemey.hp + " / " + currentEnemey.maxHp;
        //상태창에 애니메이션 적용이 편리하도록 리스트에 Rect Transform을 넣어둠.
        enemyStates.Add(currentEnemyState.GetComponent<RectTransform>());
        //currentEnemyState.GetComponent<Button>().onClick.AddListener(() => HighlightEnemy(enemyNum));
        
        //적 상태창 버튼에 적 기물 하이라이팅 함수를 연결
        for(int i = 0; i < GameManager.enemyObjects.Count; i++)
        {
            if (GameManager.enemyObjects[i] == currentEnemyObj)
            {
                currentEnemyState.GetComponent<Button>().onClick.AddListener(() => HighlightEnemy(i));
                break;
            }
        }
    }

    //정렬되지 않은 sortingList를 생성 (매개변수는 크기)
    public void CreateSortingList(int listSize)
    {
        for(int i = 0; i < listSize; i++)
        {
            sortingList.Add(i);
        }
    }

    //적 상태창을 행동력 순서로 정렬
    public void SortEnemyStates()
    {
        for (int i = 1; i < GameManager.enemyObjects.Count; i++)
        {
            int key = sortingList[i];
            int j = i - 1;

            while (j >= 0 && GameManager.enemyObjects[sortingList[j]].GetComponent<Enemy>().moveCtrl[1] < GameManager.enemyObjects[key].GetComponent<Enemy>().moveCtrl[1])
            {
                sortingList[j + 1] = sortingList[j];
                j--;
            }
            sortingList[j + 1] = key;
        }
    }

    //적 상태창들 배치
    public void DeploymentEnemyStates()
    {
        float firstPosition = enemyStates[0].rect.height * -0.7f;
        //enemyStates[enemyManager.sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);
        enemyStates[sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);

        for (int i = 1; i < enemyStates.Count; i++)
        {
            //enemyStates[enemyManager.sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
            enemyStates[sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
        }
    }

    //행동력 올라가는 애니메이션  (몇번째 enemy인지, 시작 행동력, 목표 행동력, 마지막 적의 움직임인지)
    public IEnumerator CountMovectrlAnim(int enemyNum, int start, int goal, bool finalMove)
    {
        Enemy enemy = GameManager.enemyObjects[enemyNum].GetComponent<Enemy>();
        if (goal > 10) goal = 10;
        enemyStates[enemyNum].GetComponent<Image>().DOFade(1, 0); //밝아지는 연출
        DOVirtual.Int(start, goal, uiMoveTime, ((x) => { enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + x + " / " + enemy.moveCtrl[0]; })).SetEase(Ease.InCubic);
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(SwapStatesAnim(enemyNum, finalMove));
    }

    public void StartCountEnemyHpAnim(int i, int originHP, int hp) //코루틴을 실행한 스크립트가 사라지면 코루틴이 멈춰버리므로 uiManager에서 코루틴을 호출
    {
        StartCoroutine(CountEnemyHpAnim(i, originHP, hp));
    }
    //적 체력 내려가는 애니메이션 (몇번째 enemy인지, 처음 체력, 맞은 후 체력)
    public IEnumerator CountEnemyHpAnim(int enemyNum, int start ,int goal)
    {
        Enemy enemy = GameManager.enemyObjects[enemyNum].GetComponent<Enemy>();
        if (goal < 0) goal = 0;
        enemyStates[enemyNum].GetChild(3).GetComponent<Image>().DOFade(1, 0);
        enemyStates[enemyNum].GetChild(3).GetComponent<Image>().DOFade(0, uiMoveTime * 5);
        DOVirtual.Int(start, goal, uiMoveTime * 5, ((x) => { enemyStates[enemyNum].GetChild(2).GetComponent<Text>().text = "체력 : " + x + " / " + enemy.maxHp; })).SetEase(Ease.OutCubic);
        yield return new WaitForSeconds(uiMoveTime * 5);
        if(goal == 0) //적 사망
        {
            StartCoroutine(DyingEnemyAnim(enemyNum));
        }
        yield return null;
    }
    
    //적 사망 시 상태창 애니메이션
    private IEnumerator DyingEnemyAnim(int enemyNum)
    {
        enemyStates[enemyNum].GetChild(3).GetComponent<Image>().DOFade(1, uiMoveTime * 4);
        yield return StartCoroutine(QuakeAnim(enemyStates[enemyNum], uiMoveTime * 4));
        GameObject destroyState = enemyStates[enemyNum].gameObject;
        
        //UI 터지는 부분
        explosionEffect.SetActive(true);
        RectTransform statesPanelRT = enemyStates[0].parent.parent.GetComponent<RectTransform>();
        explosionEffect.GetComponent<RectTransform>().anchoredPosition = statesPanelRT.anchoredPosition + new Vector2(0, statesPanelRT.rect.height/2 + enemyStates[enemyNum].anchoredPosition.y);
        foreach(RectTransform particle in particlesRT)
        {
            particle.anchoredPosition = Vector2.zero;
            particle.GetComponent<Image>().DOFade(1, 0);
            float angle = Random.Range(0, Mathf.PI * 2);
            particle.DOAnchorPos(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 300, uiMoveTime*2);
            particle.GetComponent<Image>().DOFade(0, uiMoveTime*2);
        }

        enemyStates.RemoveAt(enemyNum);
        Destroy(destroyState);

        //적 상태창 버튼에 적 기물 하이라이팅 함수를 연결
        for(int i = 0; i < enemyStates.Count; i++)
        {
            enemyStates[i].GetComponent<Button>().onClick.RemoveAllListeners();
            int iii = i;
            enemyStates[i].GetComponent<Button>().onClick.AddListener(() => HighlightEnemy(iii));
            //for(int j = 0; j < GameManager.enemyObjects.Count; j++)
            //{

            //}
        }


        for (int i = sortingList.Count - 1; i >= 0; i--)
        {
            if (sortingList[i] > enemyNum)
            {
                sortingList[i]--;
            } 
            else if (sortingList[i] == enemyNum)
            {
                sortingList.RemoveAt(i);
            }
        }
        if (GameManager.enemyObjects.Count != 0)
        {
            StartCoroutine(SwapStatesAnim(0, false));
        }
        yield return new WaitForSeconds(uiMoveTime*2);
        explosionEffect.SetActive(false);
    }

    //UI 흔들리는 애니메이션 (흔들 UI의 RectTransform, 흔들 시간)
    private IEnumerator QuakeAnim(RectTransform rt, float quakeTime)
    {
        Vector2 originPos = rt.anchoredPosition;
        rt.DOShakeAnchorPos(quakeTime, 10);
        yield return new WaitForSeconds(quakeTime);
    }
    

    //행동력에 따라 적들 상태창 스왑 (몇번째 enemy인지, 마지막 적의 움직임인지)
    public IEnumerator SwapStatesAnim(int enemyNum, bool isFinalMove)
    {
        float firstPosition = enemyStates[0].rect.height * -0.7f;
        enemyStates[sortingList[0]].DOAnchorPosY(firstPosition, uiMoveTime);
        //enemyStates[enemyManager.sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);
        for (int i = 1; i < enemyStates.Count; i++)
        {
            enemyStates[sortingList[i]].DOAnchorPosY(firstPosition - (enemyStates[i].rect.height + 10) * i, uiMoveTime);
            //enemyStates[enemyManager.sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
        }
        yield return new WaitForSeconds(uiMoveTime);
        enemyStates[enemyNum].GetComponent<Image>().DOFade(0.392f, 0);
        if (isFinalMove) GetComponent<EnemyManager>().EnemyTurnAnchorTrue();
    }

    // 행동력을 사용한 적의 상태창을 맨 아래로 내리고 나머지는 위로 올림. (몇번째 적인지, 얼마로 바꿀건지, 몇번째 이동 실행인지)
    public IEnumerator ReloadState(int enemyNum, int goal, int count)
    {
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x + 400, uiMoveTime);
        //CanvasGroup cg;
        //cg = enemyStates[enemyNum]
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(SwapStatesAnim(enemyNum, false));
        enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + 0 + " / " + GameManager.enemyObjects[enemyNum].GetComponent<Enemy>().moveCtrl[0];
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x - 400, uiMoveTime);
        yield return new WaitForSeconds(uiMoveTime);

        if (count == GameManager.enemyObjects.Count - 1)
            yield return StartCoroutine(CountMovectrlAnim(enemyNum, 0, goal, true));
        else
            yield return StartCoroutine(CountMovectrlAnim(enemyNum, 0, goal, false));
    }

    //적 하이라이팅 (몇번째로 소환된 적인지)
    public void HighlightEnemy(int enemyNum)
    {
        StartCoroutine(FadeInOutLoop(enemyNum));
        StartCoroutine(GameManager.enemyObjects[enemyNum].GetComponent<Enemy>().FadeInOutLoop(uiMoveTime*2));
    }

    //적 하이라이팅 페이드 인/아웃 루프 (몇번째로 소환된 적인지)
    private IEnumerator FadeInOutLoop(int enemyNum)
    {
        //for (int i = 0; i < 3; i++)
        {
            enemyStates[enemyNum].GetChild(4).GetComponent<Image>().DOFade(1, uiMoveTime*2);
            yield return new WaitForSeconds(uiMoveTime*2);
            enemyStates[enemyNum].GetChild(4).GetComponent<Image>().DOFade(0, uiMoveTime*2);
            yield return new WaitForSeconds(uiMoveTime * 2);
        }
    }

    //턴 종료 시 호출
    public void PlayerTurnEnd()
    {
        EnemyManager.turnCheck = false;
        GameManager.Turn++;
        gameManager.playerActionUI.PassiveUI();
        turnEndButton.SetActive(false);
    }
}
