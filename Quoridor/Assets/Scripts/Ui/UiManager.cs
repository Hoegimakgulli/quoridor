using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static UnityEngine.UI.Image;
using Unity.VisualScripting;
using JetBrains.Annotations;

public class UiManager : MonoBehaviour
{
    // panel 관리용 변수
    public static GameObject[] panelBox = new GameObject[2]; // 0 - Turn, 1 - history
    public static GameObject[] historyBox = new GameObject[2]; // 0 - player, 2 - enemy
    public static int turnAnchor = 0; // GameManager에 있는 Turn과 비교하는 비교군

    // 이규빈 생성 변수들
    public GameObject uiCanvas;
    private GameManager gameManager;
    private EnemyManager enemyManager;
    private List<RectTransform> enemyStates = new List<RectTransform>(); //적 상태창들의 RectTransform
    public float uiMoveTime = 0.2f; //적 상태창 움직이는 시간 
    public bool popLock = false; //임시 변수. 플레이어 및 적턴 알려주는 팝업 통제용.
    public List<int> sortingList = new List<int>(); //행동력 순서로 EnemyState를 정렬할 리스트. 각 배열의 숫자는 몇 번째 적인지를 나타냄.
    //private GameObject explosionEffect;
    //private List<RectTransform> particlesRT = new List<RectTransform>();
    public GameObject turnEndButton;
    public Text WallCountText;
    public bool freezeButton = false; //true면 행동 및 턴 종료 버튼 작동 안함. (적 상태창 애니메이션 중 행동 제약)

    Color[] enemyMoveableColor = new Color[2]; //0은 비활성화 컬러, 1은 활성화 컬러
    Color[] enemyAttackableColor = new Color[2]; //0은 비활성화 컬러, 1은 활성화 컬러

    GameObject enemyActionInfoPanel; //적 이동, 공격 범위를 표시할 UI를 담을 Panel
    GameObject enemyAttackInfoPanel; //적의 공격 범위를 표시할 UI를 담을 Panel
    GameObject enemyMoveInfoPanel; //적의 이동 범위를 표시할 UI를 담을 Panel
    List<RectTransform> enemyAttackablePoints = new List<RectTransform>(); //적 공격 가능 위치를 표시할 오브젝트들의 RectTransform
    List<RectTransform> enemyMoveablePoints = new List<RectTransform>(); //적 이동 가능 위치를 표시할 오브젝트들의 RectTransform



    public class attackedEnemyValues
    {
        public int enemyNum;
        public int originalHP;
        public int goalHP;
        public int maxHP;

        public attackedEnemyValues(int enemyNum, int originalHP, int goalHP, int maxHP)
        {
            this.enemyNum = enemyNum;
            this.originalHP = originalHP;
            this.goalHP = goalHP;
            this.maxHP = maxHP;
        }
    }

    public List<attackedEnemyValues> attackedEnemyList = new List<attackedEnemyValues>();


    private void Awake()
    {
        enemyMoveableColor[0] = new Color(1, 1, 1);
        enemyMoveableColor[1] = new Color(0.4f, 0.8f, 1);
        enemyAttackableColor[0] = new Color(1, 1, 1);
        enemyAttackableColor[1] = new Color(1f, 0.8f, 0.4f);
        enemyManager = GetComponent<EnemyManager>();
        uiCanvas = Instantiate(uiCanvas);
        turnEndButton = uiCanvas.transform.GetChild(4).gameObject;
        turnEndButton.GetComponent<Button>().onClick.AddListener(() => PlayerTurnEnd());
        WallCountText = uiCanvas.transform.GetChild(5).GetChild(2).GetComponent<Text>();

    }
    private void Start()
    {
        gameManager = GetComponent<GameManager>();
        //playerUI = Instantiate(playerUI); //플레이어 UI 캔버스 소환
        panelBox[0] = GameObject.Find("TurnPanel");
        panelBox[1] = GameObject.Find("HistoryPanel");
        historyBox[0] = panelBox[1].transform.GetChild(0).transform.GetChild(0).gameObject; // History -> playerBox 접근
        historyBox[1] = panelBox[1].transform.GetChild(0).transform.GetChild(1).gameObject; // History -> enemyBox 접근
        /*explosionEffect = panelBox[0].transform.parent.GetChild(3).GetChild(2).gameObject;
        for (int i = 0; i < explosionEffect.transform.childCount; i++)
        {
            particlesRT.Add(explosionEffect.transform.GetChild(i).GetComponent<RectTransform>());
        }
        explosionEffect.SetActive(false);
    */
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
        for (int i = enemyStates.Count - 1; i >= 0; i--)
        {
            Destroy(enemyStates[i].gameObject);
        }
        enemyStates.Clear();
        sortingList.Clear();

    }


    //매개변수로 받아온 적의 상태창 생성 (받아온 적의 상태창 오브젝트, 받아온 적의 오브젝트, 받아온 적의 Enemy 스크립트, 받아온 적이 몇번째로 소환된 적인지)
    public void CreateEnemyState(GameObject currentEnemyState, GameObject currentEnemyObj, Enemy currentEnemey, int enemyNum)
    {
        //상태창 이미지, 수치들을 바꿈
        currentEnemyState.transform.GetChild(5).gameObject.SetActive(false); //터지는 이펙트 비활성화
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
        for (int i = 0; i < GameManager.enemyValueList.Count; i++)
        {
            Debug.Log(EnemyManager.GetEnemyObject(i));
            if (EnemyManager.GetEnemyObject(i) == currentEnemyObj)
            {
                currentEnemyState.GetComponent<Button>().onClick.AddListener(() => HighlightEnemy(i));
                break;
            }
        }
    }

    //정렬되지 않은 sortingList를 생성 (매개변수는 크기)
    public void CreateSortingList(int listSize)
    {
        for (int i = 0; i < listSize; i++)
        {
            sortingList.Add(i);
        }
    }

    //Sorting List를 적 행동력 순서대로 변경
    public void SortEnemyStates()
    {
        for (int i = 1; i < GameManager.enemyValueList.Count; i++)
        {
            int key = sortingList[i];
            int j = i - 1;

            while (j >= 0 && GameManager.enemyValueList[sortingList[j]].moveCtrl < GameManager.enemyValueList[key].moveCtrl)
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
        if (goal > 10) goal = 10;
        enemyStates[enemyNum].GetComponent<Image>().DOFade(1, 0); //밝아지는 연출
        DOVirtual.Int(start, goal, uiMoveTime, ((x) => { enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + x + " / 10"; })).SetEase(Ease.InCubic);
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(SwapStatesAnim(enemyNum, finalMove));
    }

    public void StartCountEnemyHpAnim(int i, int originHP, int hp) //코루틴을 실행한 스크립트가 사라지면 코루틴이 멈춰버리므로 uiManager에서 코루틴을 호출
    {
        freezeButton = true;
        if (hp < 0) hp = 0; //피격 후 hp가 0 아래로 내려가면 0으로 고정.
        attackedEnemyList.Add(new attackedEnemyValues(i, originHP, hp, EnemyManager.GetEnemyObject(i).GetComponent<Enemy>().maxHp)); //적 번호, 원래 HP, 바뀐 HP, max HP를 리스트에 저장
        if (attackedEnemyList.Count == 1) //한 번의 공격에 한번씩만 실행되도록.
        {
            StartCoroutine(CountEnemyHpAnim());
        }
    }
    //적 체력 내려가는 애니메이션 (몇번째 enemy인지, 처음 체력, 맞은 후 체력)
    public IEnumerator CountEnemyHpAnim()
    {
        yield return new WaitForSeconds(0.05f); //공격받은 모든 적이 attackedEnemyList에 들어올때까지 잠깐 기다림

        for (int i = 0; i < attackedEnemyList.Count; i++)
        {
            Debug.Log(i + " : " + attackedEnemyList[i].enemyNum);
        }
        foreach (attackedEnemyValues value in attackedEnemyList)
        {
            enemyStates[value.enemyNum].GetChild(3).GetComponent<Image>().DOFade(1, 0); //피격시 ui 빨개지는 애니메이션 준비
            enemyStates[value.enemyNum].GetChild(3).GetComponent<Image>().DOFade(0, uiMoveTime * 3); //피격시 ui 빨개지는 애니메이션
            //enemyStates[enemyNum].GetChild(2).GetComponent<Text>().text = "체력 : " + value.goalHP + " / " + value.maxHP;
            DOVirtual.Int(value.originalHP, value.goalHP, uiMoveTime * 3, ((x) => { enemyStates[value.enemyNum].GetChild(2).GetComponent<Text>().text = "체력 : " + x + " / " + value.maxHP; })).SetEase(Ease.OutCubic); //체력 줄어드는 애니메이션
        }
        yield return new WaitForSeconds(uiMoveTime * 3);
        for (int i = attackedEnemyList.Count - 1; i >= 0; i--)
        {
            if (attackedEnemyList[i].goalHP > 0) //피격된 것 중 죽지 않은 것들은 리스트에서 제거 
            {
                attackedEnemyList.Remove(attackedEnemyList[i]);
            }
        }
        if (attackedEnemyList.Count > 0) //사망한 적이 하나 이상 있다면
        {
            StartCoroutine(DyingEnemyAnim());
        }
        else
        {
            freezeButton = false; //사망한 적이 없으면 바로 버튼 클릭 가능한 상태가 됨
        }
        yield return null;
    }

    //적 사망 시 상태창 애니메이션
    private IEnumerator DyingEnemyAnim()
    {

        List<GameObject> destroyObjects = new List<GameObject>(); //처리 후 삭제될 오브젝트를 담아두는 리스트
        List<int> destroyEnemyNum = new List<int>(); //처리 후 삭제될 적의 번호를 담아두는 리스트
        foreach (attackedEnemyValues value in attackedEnemyList) //죽기 전 예열 foreach문
        {
            enemyStates[value.enemyNum].GetChild(3).GetComponent<Image>().DOFade(1, uiMoveTime * 3); //죽은애들 상태창 빨개짐
            StartCoroutine(QuakeAnim(enemyStates[value.enemyNum], uiMoveTime * 3)); //죽은애들 상태창 흔들림
        }
        yield return new WaitForSeconds(uiMoveTime * 3);
        foreach (attackedEnemyValues value in attackedEnemyList) //터질때 실행되는 foreach문
        {
            enemyStates[value.enemyNum].GetComponent<RectTransform>().sizeDelta = Vector2.zero; //상태창 본체 크기 0으로
            for (int i = 0; i < 5; i++)
            {
                enemyStates[value.enemyNum].GetChild(i).gameObject.SetActive(false); //터지는 이펙트를 제외한 모든 자식을 비활성화
            }
            enemyStates[value.enemyNum].GetChild(5).gameObject.SetActive(true); //터지는 이펙트 활성화
            List<RectTransform> explosionParticleRT = new List<RectTransform>(); //이펙트 입자들 리스트
            for (int i = 0; i < enemyStates[value.enemyNum].GetChild(5).childCount; i++)
            {
                explosionParticleRT.Add(enemyStates[value.enemyNum].GetChild(5).GetChild(i).GetComponent<RectTransform>());
            }
            foreach (RectTransform particle in explosionParticleRT) //터지는 이펙트 애니메이션
            {
                particle.anchoredPosition = Vector2.zero;
                particle.GetComponent<Image>().DOFade(1, 0);
                float angle = Random.Range(0, Mathf.PI * 2);
                particle.DOAnchorPos(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 300, uiMoveTime * 1.5f);
                particle.GetComponent<Image>().DOFade(0, uiMoveTime * 1.5f);
            }
            destroyObjects.Add(enemyStates[value.enemyNum].gameObject);
            destroyEnemyNum.Add(value.enemyNum);
            //enemyStates.RemoveAt(value.enemyNum); //상태창 리스트에서 죽은애 상태창을 제거
        }
        for (int i = attackedEnemyList.Count - 1; i >= 0; i--)
        {
            for (int j = sortingList.Count - 1; j >= 0; j--) // SortingList에서 죽은애는 제거하고 죽은애보다 뒤에있는애는 앞으로 당겨줌.
            {
                if (sortingList[j] > attackedEnemyList[i].enemyNum)
                {
                    sortingList[j]--;
                }
                else if (sortingList[j] == attackedEnemyList[i].enemyNum)
                {
                    sortingList.RemoveAt(j);
                }
            }
            enemyStates.RemoveAt(attackedEnemyList[i].enemyNum); //상태창 리스트에서 죽은애 상태창을 제거
            foreach (attackedEnemyValues value in attackedEnemyList)
            {
                if (value.enemyNum > attackedEnemyList[i].enemyNum)
                {
                    value.enemyNum--;
                }
            }
            attackedEnemyList.Remove(attackedEnemyList[i]);
        }
        for (int i = 0; i < enemyStates.Count; i++) //적 상태창 버튼들에 적 기물 하이라이팅 함수를 연결
        {
            enemyStates[i].GetComponent<Button>().onClick.RemoveAllListeners();
            int iii = i;
            enemyStates[i].GetComponent<Button>().onClick.AddListener(() => HighlightEnemy(iii));
        }

        if (GameManager.enemyValueList.Count != 0) //안 죽은 적이 남아있다면
        {
            StartCoroutine(SwapStatesAnim(0, false)); //순서대로 상태창 위치 바꾸는 애니메이션 실행
        }
        yield return new WaitForSeconds(uiMoveTime * 1.5f); //터지는 이펙트 입자가 사라지는 시간동안 대기
        for (int i = destroyObjects.Count - 1; i >= 0; i--) //죽은애들 상태창 오브젝트 전부 삭제
        {
            Destroy(destroyObjects[i]);
        }
        freezeButton = false; //버튼 잠금 해제
    }

    //UI 흔들리는 애니메이션 (흔들 UI의 RectTransform, 흔들 시간)
    private IEnumerator QuakeAnim(RectTransform rt, float quakeTime)
    {
        Vector2 originPos = rt.anchoredPosition;
        rt.DOShakeAnchorPos(quakeTime, 10);
        yield return new WaitForSeconds(quakeTime);
    }


    //SortingList에 따라 적들 상태창 스왑 (몇번째 enemy인지, 마지막 적의 움직임인지)
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
        enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "행동력 : " + 0 + " / 10";  //GameManager.enemyObjects[enemyNum].GetComponent<Enemy>().moveCtrl[0];
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x - 400, uiMoveTime);
        yield return new WaitForSeconds(uiMoveTime);

        if (count == GameManager.enemyValueList.Count - 1)
            yield return StartCoroutine(CountMovectrlAnim(enemyNum, 0, goal, true));
        else
            yield return StartCoroutine(CountMovectrlAnim(enemyNum, 0, goal, false));
    }

    //적 하이라이팅 (몇번째로 소환된 적인지)
    public void HighlightEnemy(int enemyNum)
    {
        StartCoroutine(FadeInOutLoop(enemyNum));
        StartCoroutine(EnemyManager.GetEnemyObject(enemyNum).GetComponent<Enemy>().FadeInOutLoop(uiMoveTime * 2));
    }

    //적 하이라이팅 페이드 인/아웃 루프 (몇번째로 소환된 적인지)
    private IEnumerator FadeInOutLoop(int enemyNum)
    {
        //for (int i = 0; i < 3; i++)
        {
            enemyStates[enemyNum].GetChild(4).GetComponent<Image>().DOFade(1, uiMoveTime * 2);
            yield return new WaitForSeconds(uiMoveTime * 2);
            enemyStates[enemyNum].GetChild(4).GetComponent<Image>().DOFade(0, uiMoveTime * 2);
            yield return new WaitForSeconds(uiMoveTime * 2);
        }
    }

    //적 정보 표시 UI를 생성하는 함수 (공격범위, 이동범위 등)
    public void CreateEnemyInfoUI()
    {
        Vector2 panelSize = new Vector2(800, 600); //적 정보 표시 Panel의 사이즈. 이 값만으로 UI 사이즈 조절 가능
        float childPanelSize = panelSize.x / 2.5f; //이동 정보 표시 Panel과 공격 정보 표시 Panel의 사이즈
        float childInterver = (panelSize.x - (childPanelSize * 2)) / 3; //자식 Panel들 사이의 간격

        //Destroy(enemyActionInfoPanel); //기존의 것을 삭제
        for (int i = enemyAttackablePoints.Count - 1; i >= 0; i--)
        {
            Destroy(enemyAttackablePoints[i].gameObject);
        }
        for (int i = enemyMoveablePoints.Count - 1; i >= 0; i--)
        {
            Destroy(enemyMoveablePoints[i].gameObject);
        }
        enemyAttackablePoints.Clear();
        enemyMoveablePoints.Clear();

        if (enemyActionInfoPanel == null)
        {
            enemyActionInfoPanel = new GameObject("Enemy Action Info Panel");
            Image ActionInfoImage = enemyActionInfoPanel.AddComponent<Image>(); //이미지 컴포넌트를 추가.
            enemyActionInfoPanel.transform.SetParent(uiCanvas.transform); //uiCanvas의 자식으로 넣어줌
            enemyActionInfoPanel.GetComponent<RectTransform>().sizeDelta = panelSize;
            ActionInfoImage.color = new Color(1, 1, 1, 0.8f); //반투명하게 설정
        }


        GameObject moveablePointsParent;
        GameObject attackablePointsParent;
        //같은 방식으로 이동범위 Panel과 공격범위 Panel을 생성 및 배치
        if (enemyMoveInfoPanel == null)
        {
            enemyMoveInfoPanel = new GameObject("Enemy Move Info Panel");
            Image MoveInfoImage = enemyMoveInfoPanel.AddComponent<Image>();
            RectTransform moveInfoRT = enemyMoveInfoPanel.GetComponent<RectTransform>();
            enemyMoveInfoPanel.transform.SetParent(enemyActionInfoPanel.transform);
            moveInfoRT.sizeDelta = new Vector2(childPanelSize, childPanelSize);
            moveInfoRT.anchoredPosition = new Vector2(-panelSize.x / 2 + childPanelSize / 2 + childInterver, 0);
            MoveInfoImage.color = new Color(1, 1, 1, 0.8f);
            moveablePointsParent = new GameObject("Moveable Points");
            moveablePointsParent.transform.SetParent(enemyMoveInfoPanel.transform);
            moveablePointsParent.AddComponent<RectTransform>().anchoredPosition = Vector3.zero;
        }
        else
        {
            moveablePointsParent = enemyMoveInfoPanel.transform.GetChild(0).gameObject;
        }
        if (enemyAttackInfoPanel == null)
        {
            enemyAttackInfoPanel = new GameObject("Enemy Attack Info Panel");
            Image AttackInfoImage = enemyAttackInfoPanel.AddComponent<Image>();
            RectTransform attackInfoRT = enemyAttackInfoPanel.GetComponent<RectTransform>();
            enemyAttackInfoPanel.transform.SetParent(enemyActionInfoPanel.transform);
            attackInfoRT.sizeDelta = new Vector2(childPanelSize, childPanelSize);
            attackInfoRT.anchoredPosition = new Vector2(panelSize.x / 2 - childPanelSize / 2 - childInterver, 0);
            AttackInfoImage.color = new Color(1, 1, 1, 0.8f);
            attackablePointsParent = new GameObject("Attackable Points");
            attackablePointsParent.transform.SetParent(enemyAttackInfoPanel.transform);
            attackablePointsParent.AddComponent<RectTransform>().anchoredPosition = Vector3.zero;
        }
        else
        {
            attackablePointsParent = enemyAttackInfoPanel.transform.GetChild(0).gameObject;
        }

        int moveablePointsSize = 0; //이동가능 포인트의 최대 개수
        int attackablePointsSize = 0; //공격 가능 포인트의 최대 개수

        for (int i = 0; i < GameManager.enemyValueList.Count; i++)
        {
            Enemy enemy = EnemyManager.GetEnemy(i);

            moveablePointsSize = enemy.moveablePoints.Length > moveablePointsSize ? enemy.moveablePoints.Length : moveablePointsSize;
            attackablePointsSize = enemy.attackablePoints.Length > attackablePointsSize ? enemy.attackablePoints.Length : attackablePointsSize;
        }
        for (int i = 0; i <= moveablePointsSize; i++) //이동가능 포인트의 최대 개수 만큼만 moveablePoint를 생성
        {
            GameObject moveablePoint = new GameObject("Moveable Point " + i);
            Image moveablePointImage = moveablePoint.AddComponent<Image>();
            moveablePoint.transform.SetParent(moveablePointsParent.transform);
            moveablePointImage.color = i == 0 ? Color.gray : enemyMoveableColor[1];
            enemyMoveablePoints.Add(moveablePoint.GetComponent<RectTransform>());
            moveablePoint.GetComponent<RectTransform>().transform.localScale = Vector3.one;
        }
        for (int i = 0; i <= attackablePointsSize; i++) //공격가능 포인트의 최대 개수 만큼만 attackablePoint를 생성
        {
            GameObject attackablePoint = new GameObject("Attackable Point " + i);
            Image attackablePointImage = attackablePoint.AddComponent<Image>();
            attackablePoint.transform.SetParent(attackablePointsParent.transform);
            attackablePointImage.color = i == 0 ? Color.gray : enemyAttackableColor[1];
            enemyAttackablePoints.Add(attackablePoint.GetComponent<RectTransform>());
            attackablePoint.GetComponent<RectTransform>().transform.localScale = Vector3.one;
        }
        enemyActionInfoPanel.gameObject.SetActive(false);

    }

    //적 이동범위, 공격범위 표시하는 UI를 활성화 (위로 펼칠건지 아래로 펼칠건지 결정하기 위한 적의 위치, 적의 이동가능 포인트들, 적의 공격 가능 포인트들, 적의 색깔)
    public void ActiveEnemyInfoUI(Vector2 enemyPosition, Vector2Int[] moveablePoints, Vector2Int[] attackablePoints, Color enemyColor)
    {
        enemyMoveablePoints[0].GetComponent<Image>().color = enemyColor;
        enemyAttackablePoints[0].GetComponent<Image>().color = enemyColor;
        enemyActionInfoPanel.gameObject.SetActive(true);
        if (enemyPosition.y > 0)
        {
            enemyActionInfoPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 1);
        }
        else
        {
            enemyActionInfoPanel.GetComponent<RectTransform>().pivot = new Vector2(0.5f, 0);
        }
        enemyActionInfoPanel.transform.position = Camera.main.WorldToScreenPoint(enemyPosition);
        enemyActionInfoPanel.transform.localScale = Vector2.zero;
        enemyActionInfoPanel.GetComponent<RectTransform>().DOScale(new Vector2(0.7f, 0.7f), uiMoveTime);
        int minX = 0, minY = 0, maxX = 0, maxY = 0; //왼쪽, 아래쪽, 오른쪽, 위쪽으로 각각 최대 몇칸 필요한지 파악하기 위한 변수

        enemyMoveablePoints[0].anchoredPosition = Vector2.zero;
        foreach (var enemyMoveablePoint in enemyMoveablePoints)
        {
            enemyMoveablePoint.gameObject.SetActive(false);
        }
        enemyMoveablePoints[0].gameObject.SetActive(true);
        foreach (var moveablePoint in moveablePoints)
        {
            minX = moveablePoint.x < minX ? moveablePoint.x : minX;
            maxX = moveablePoint.x > maxX ? moveablePoint.x : maxX;
            minY = moveablePoint.y < minY ? moveablePoint.y : minY;
            maxY = moveablePoint.y > maxY ? moveablePoint.y : maxY;
        }

        int maxLength = maxX - minX > maxY - minY ? maxX - minX + 1 : maxY - minY + 1; //가로길이 세로길이 중 긴 것을 포인트들의 크기를 정하는 데 사용
        float horizontalMean = (maxX + minX) / 2.0f; //가로의 중간 위치
        float verticalMean = (maxY + minY) / 2.0f; //세로의 중간 위치
        float pointsMove = enemyMoveInfoPanel.GetComponent<RectTransform>().sizeDelta.x / maxLength; //point의 한칸 이동 거리
        Vector2 pointSize = new Vector2(pointsMove / 1.2f, pointsMove / 1.2f);

        enemyMoveInfoPanel.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-horizontalMean * pointsMove, -verticalMean * pointsMove);

        enemyMoveablePoints[0].anchoredPosition = Vector2.zero;
        enemyMoveablePoints[0].sizeDelta = pointSize;

        for (int i = 0; i < moveablePoints.Length; i++)
        {
            enemyMoveablePoints[i + 1].gameObject.SetActive(true);
            float x = 0; float y = 0;
            if (moveablePoints[i].x > 0) x = (moveablePoints[i].x * 2 - 1) * pointsMove;
            else if (moveablePoints[i].x < 0) x = (moveablePoints[i].x * 2 + 1) * pointsMove;
            if (moveablePoints[i].y > 0) y = (moveablePoints[i].y * 2 - 1) * pointsMove;
            else if (moveablePoints[i].y < 0) y = (moveablePoints[i].y * 2 + 1) * pointsMove;
            enemyMoveablePoints[i + 1].anchoredPosition = new Vector2(moveablePoints[i].x * pointsMove, moveablePoints[i].y * pointsMove);
            enemyMoveablePoints[i + 1].sizeDelta = pointSize;
        }



        minX = 0; minY = 0; maxX = 0; maxY = 0;
        enemyAttackablePoints[0].anchoredPosition = Vector2.zero;
        foreach (var enemyattackablePoint in enemyAttackablePoints)
        {
            enemyattackablePoint.gameObject.SetActive(false);
        }
        enemyAttackablePoints[0].gameObject.SetActive(true);
        foreach (var attackablePoint in attackablePoints)
        {
            minX = attackablePoint.x < minX ? attackablePoint.x : minX;
            maxX = attackablePoint.x > maxX ? attackablePoint.x : maxX;
            minY = attackablePoint.y < minY ? attackablePoint.y : minY;
            maxY = attackablePoint.y > maxY ? attackablePoint.y : maxY;
        }
        Debug.Log("minX " + minX + "   max X " + maxX + "   min Y " + minY + "   maxY " + maxY);
        maxLength = maxX - minX > maxY - minY ? maxX - minX + 1 : maxY - minY + 1; //가로길이 세로길이 중 긴 것을 포인트들의 크기를 정하는 데 사용함
        horizontalMean = (maxX + minX) / 2.0f; //가로의 중간 위치
        verticalMean = (maxY + minY) / 2.0f; //세로의 중간 위치
        pointsMove = enemyAttackInfoPanel.GetComponent<RectTransform>().sizeDelta.x / maxLength; //point의 한칸 이동 거리
        Debug.Log(verticalMean);
        pointSize = new Vector2(pointsMove / 1.2f, pointsMove / 1.2f);
        enemyAttackInfoPanel.transform.GetChild(0).GetComponent<RectTransform>().anchoredPosition = new Vector2(-horizontalMean * pointsMove, -verticalMean * pointsMove);
        enemyAttackablePoints[0].anchoredPosition = Vector2.zero;
        enemyAttackablePoints[0].sizeDelta = pointSize;
        for (int i = 0; i < attackablePoints.Length; i++)
        {
            enemyAttackablePoints[i + 1].gameObject.SetActive(true);
            float x = 0; float y = 0;
            if (attackablePoints[i].x > 0) x = (attackablePoints[i].x * 2 - 1) * pointsMove;
            else if (attackablePoints[i].x < 0) x = (attackablePoints[i].x * 2 + 1) * pointsMove;
            if (attackablePoints[i].y > 0) y = (attackablePoints[i].y * 2 - 1) * pointsMove;
            else if (attackablePoints[i].y < 0) y = (attackablePoints[i].y * 2 + 1) * pointsMove;
            enemyAttackablePoints[i + 1].anchoredPosition = new Vector2(attackablePoints[i].x * pointsMove, attackablePoints[i].y * pointsMove);
            enemyAttackablePoints[i + 1].sizeDelta = pointSize;
        }
    }
    public void PassiveEnemyInfoUI()
    {
        enemyActionInfoPanel.gameObject.SetActive(false);
    }

    private bool canPlayerTurnEnd = true;
    private GameObject wariningPanel;
    //턴 종료 시 호출
    public void PlayerTurnEnd()
    {
        canPlayerTurnEnd = true;
        if (!freezeButton)
        {
            foreach(GameObject child in gameManager.players)
            {
                Player childPlayer = child.GetComponent<Player>();
                if (childPlayer.canAction || childPlayer.canAttack)
                {
                    canPlayerTurnEnd = false;
                    if (wariningPanel == null)
                    {
                        wariningPanel = GameObject.Find("WarningPanel");
                        GameObject.Find("YesButton").GetComponent<Button>().onClick.AddListener(() => YesButtonClick());
                        GameObject.Find("NoButton").GetComponent<Button>().onClick.AddListener(() => NoButtonClick());
                    }
                    wariningPanel.GetComponent<RectTransform>().DOScale(new Vector3(1, 1, 1), 0.2f).SetEase(Ease.Linear);
                    break;
                }
            }

            if (canPlayerTurnEnd)
            {
                EnemyManager.turnCheck = false;
                GameManager.Turn++;
                for (int count = 0; count < gameManager.playerActionUis.Count; count++)
                {
                    gameManager.playerActionUis[count].PassiveUI();
                }
                turnEndButton.SetActive(false);
            }
        }
    }

    public void YesButtonClick()
    {
        wariningPanel.GetComponent<RectTransform>().DOScale(new Vector3(0, 0, 0), 0.01f).SetEase(Ease.Linear);
        EnemyManager.turnCheck = false;
        GameManager.Turn++;
        for (int count = 0; count < gameManager.playerActionUis.Count; count++)
        {
            gameManager.playerActionUis[count].PassiveUI();
        }
        turnEndButton.SetActive(false);
    }

    public void NoButtonClick()
    {
        wariningPanel.GetComponent<RectTransform>().DOScale(new Vector3(0, 0, 0), 0.01f).SetEase(Ease.Linear);
    }
}