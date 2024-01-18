using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    // panel 관리용 변수
    public static GameObject[] panelBox = new GameObject[2]; // 0 - Turn, 1 - history
    public static GameObject[] historyBox = new GameObject[2]; // 0 - player, 2 - enemy
    public static int turnAnchor = 0; // GameManager에 있는 Turn과 비교하는 비교군

    // 이규빈 생성 변수들
    public GameObject enemyStatePre;
    public EnemyManager enemyManager;
    private List<RectTransform> enemyStates = new List<RectTransform>();
    private GameObject canvas;
    private List<GameObject> enemies = new List<GameObject>();
    private List<Enemy> enemiesScript = new List<Enemy>();
    public float uiMoveTime = 0.2f; //적 상태창 움직이는 시간 
    public bool popLock = false; //임시 변수. 플레이어 및 적턴 알려주는 팝업 통제용.
    //private List<int> sortingList = new List<int>(); //행동력 순서로 EnemyState를 정렬한 리스트. 각 배열의 숫자는 enemyState 리스트의 몇번째 배열인지를 나타냄

    public GameObject EnemyStatePanel;

    private void Awake()
    {
        canvas = GameObject.Find("Canvas");
        enemyManager = GetComponent<EnemyManager>();
        Instantiate(EnemyStatePanel, GameObject.Find("Canvas").transform);
    }

    private void Start()
    {
        panelBox[0] = GameObject.Find("TurnPanel");
        panelBox[1] = GameObject.Find("HistoryPanel");
        historyBox[0] = panelBox[1].transform.GetChild(0).transform.GetChild(0).gameObject; // History -> playerBox 접근
        historyBox[1] = panelBox[1].transform.GetChild(0).transform.GetChild(1).gameObject; // History -> enemyBox 접근
    }

    private void Update()
    {
        if (GameManager.Turn % 2 == 0) // 패널 관리 파트
        {
            StartCoroutine(EnemyPanelPop());
        }

        if (GameManager.Turn % 2 == 1 && !popLock)
        {
            StartCoroutine(PlayerPanelPop());
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            StartCoroutine(A());
        }
    }
    IEnumerator A()
    {
        for(int i = 0; i < 10; i++)
        {
            Debug.Log(i);
            yield return StartCoroutine(BBC());
        }
    }
    IEnumerator BBC() {
        Debug.Log("fkfkfk");
        yield return new WaitForSeconds(1);
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
    
    //현재 존재하는 적들을 적 상태창에 표기 및 정렬한다.
    public void EnemyStateSetting()
    {
        enemies = Enemy.enemyObjects;
        enemyStates.Clear();
        enemiesScript.Clear();
        for(int i = 0; i < Enemy.enemyObjects.Count; i++)
        {
            enemiesScript.Add(enemies[i].GetComponent<Enemy>());
            enemyStates.Add(Instantiate(enemyStatePre, canvas.transform.GetChild(3).GetChild(1).GetChild(0)).GetComponent<RectTransform>());
            enemyStates[i].GetChild(0).GetChild(0).GetComponent<Image>().sprite = enemies[i].GetComponent<SpriteRenderer>().sprite;
            enemyStates[i].GetChild(0).GetChild(0).GetComponent<Image>().color = enemies[i].GetComponent<SpriteRenderer>().color;
            enemyStates[i].GetChild(1).GetComponent<Text>().text = "행동력 : " + enemiesScript[i].moveCtrl[1] + " / 10";
            enemyStates[i].GetChild(2).GetComponent<Text>().text = "체력 : " + enemiesScript[i].hp + " / " + enemies[i].GetComponent<Enemy>().maxHp;
            //enemyManager.sortingList.Add(i);
        }
        //EnemyStateSort();
        EnemyStatesArr();
    }
/*
    //적 상태창 순서 정렬
    private void EnemyStateSort()
    {
        for(int i = 1; i < enemies.Count; i++)
        {
            int key = sortingList[i];
            int j = i - 1;

            while(j >= 0 && enemiesScript[sortingList[j]].cost < enemiesScript[key].cost)
            {
                sortingList[j + 1] = sortingList[j];
                j--;
            }
            sortingList[j + 1] = key;
        }
    }
*/
    //적 상태창들 배치
    private void EnemyStatesArr()
    {
        float firstPosition = enemyStates[0].rect.height * -0.7f;
        enemyStates[enemyManager.sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);
        for(int i = 1; i < enemyStates.Count; i++)
        {
            enemyStates[enemyManager.sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
        }
    }

    //행동력 올라가는 애니메이션  (몇번째 에네미인지, 시작 행동력, 목표 행동력)
    public IEnumerator MovectrlCountAnim(int enemyNum, int startCost, int goalCost)
    {
        if (goalCost > 10) goalCost = 10;
        enemyStates[enemyNum].GetComponent<Image>().DOFade(1, 0);
        DOVirtual.Int(startCost, goalCost, uiMoveTime, ((x) => { enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + x + " / " + enemiesScript[enemyNum].moveCtrl[0]; })).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(StateSwapAnim(enemyNum));
    }

    //행동력에 따라 적들 상태창 스왑
    public IEnumerator StateSwapAnim(int enemyNum)
    {
        float firstPosition = enemyStates[0].rect.height * -0.7f;
        enemyStates[enemyManager.sortingList[0]].DOAnchorPosY(firstPosition, uiMoveTime);
        //enemyStates[enemyManager.sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);
        for (int i = 1; i < enemyStates.Count; i++)
        {
            enemyStates[enemyManager.sortingList[i]].DOAnchorPosY(firstPosition - (enemyStates[i].rect.height + 10) * i, uiMoveTime);
            //enemyStates[enemyManager.sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
        }
        yield return new WaitForSeconds(uiMoveTime);
        enemyStates[enemyNum].GetComponent<Image>().DOFade(0.392f, 0);
    }

    // 행동력 사용 후 맨 아래로 내리고 나머지 위로 올림.
    public IEnumerator ReloadState(int enemyNum, int goalCost)
    {
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x + 400, uiMoveTime);
        //CanvasGroup cg;
        //cg = enemyStates[enemyNum]
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(StateSwapAnim(enemyNum));
        enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + 0 + " / " + enemiesScript[enemyNum].moveCtrl[0];
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x - 400, uiMoveTime);
        yield return new WaitForSeconds(uiMoveTime);

        yield return StartCoroutine(MovectrlCountAnim(enemyNum, 0, goalCost));
    }
}
