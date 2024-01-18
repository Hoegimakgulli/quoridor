using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UiManager : MonoBehaviour
{
    // panel ������ ����
    public static GameObject[] panelBox = new GameObject[2]; // 0 - Turn, 1 - history
    public static GameObject[] historyBox = new GameObject[2]; // 0 - player, 2 - enemy
    public static int turnAnchor = 0; // GameManager�� �ִ� Turn�� ���ϴ� �񱳱�

    // �̱Ժ� ���� ������
    public GameObject enemyStatePre;
    public EnemyManager enemyManager;
    private List<RectTransform> enemyStates = new List<RectTransform>();
    private GameObject canvas;
    private List<GameObject> enemies = new List<GameObject>();
    private List<Enemy> enemiesScript = new List<Enemy>();
    public float uiMoveTime = 0.2f; //�� ����â �����̴� �ð� 
    public bool popLock = false; //�ӽ� ����. �÷��̾� �� ���� �˷��ִ� �˾� ������.
    //private List<int> sortingList = new List<int>(); //�ൿ�� ������ EnemyState�� ������ ����Ʈ. �� �迭�� ���ڴ� enemyState ����Ʈ�� ���° �迭������ ��Ÿ��

    public GameObject EnemyStatePanel;

    private void Awake()
    {
        canvas = GameObject.Find("Canvas");
        enemyManager = GetComponent<EnemyManager>();
        Instantiate(EnemyStatePanel, GameObject.Find("Canvas(Clone)").transform);
    }

    private void Start()
    {
        panelBox[0] = GameObject.Find("TurnPanel");
        panelBox[1] = GameObject.Find("HistoryPanel");
        historyBox[0] = panelBox[1].transform.GetChild(0).transform.GetChild(0).gameObject; // History -> playerBox ����
        historyBox[1] = panelBox[1].transform.GetChild(0).transform.GetChild(1).gameObject; // History -> enemyBox ����
    }

    private void Update()
    {
        if (GameManager.Turn % 2 == 0) // �г� ���� ��Ʈ
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

    public void HistoryPanelPop() // history panel ���� �ݱ� �Լ�
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
        while (playerHistoryContent.name != "Content") // player Content ã�Ƽ� �־��ֱ�
        {
            playerHistoryContent = playerHistoryContent.transform.GetChild(0).gameObject;
        }

        Debug.Log("player move history");
        GameObject indexObj = Instantiate(historyIndex, new Vector3(0, 0, 0), Quaternion.identity, playerHistoryContent.transform);
        indexObj.transform.GetChild(0).GetComponent<Text>().text = "�� " + (GameManager.Turn / 2 + 1); // �� ǥ��
        indexObj.transform.GetChild(1).GetComponent<Text>().text = "�̵�"; // �̵� or ����ġ ǥ��
        indexObj.transform.GetChild(2).GetComponent<Text>().text = "" + (char)(beforePos.x + 69) + ((beforePos.y - 4) * -1) + "��" + (char)(currentPos.x + 69) + ((currentPos.y - 4) * -1); // 65 - A ���� �ƽ�Ű�ڵ尪 + ��ǥ������ ���� ���
    }

    public static void InputPlayerWallHistory(Vector3 wallPos, Quaternion wallRot, GameObject historyIndex)
    {
        GameObject playerHistoryContent = historyBox[0];
        while (playerHistoryContent.name != "Content") // player Content ã�Ƽ� �־��ֱ�
        {
            playerHistoryContent = playerHistoryContent.transform.GetChild(0).gameObject;
        }

        Debug.Log("player wall history");
        GameObject indexObj = Instantiate(historyIndex, new Vector3(0, 0, 0), Quaternion.identity, playerHistoryContent.transform);
        indexObj.transform.GetChild(0).GetComponent<Text>().text = "�� " + (GameManager.Turn / 2); // �� ǥ��
        indexObj.transform.GetChild(1).GetComponent<Text>().text = "����ġ"; // �̵� or ����ġ ǥ��
        indexObj.transform.GetChild(2).GetComponent<Text>().text = "��";
    }


    public static IEnumerator EnemyPanelPop() // TurnPanel child �� 0 = player, 1 = enemy
    {
        if (turnAnchor != GameManager.Turn)
        {
            turnAnchor = GameManager.Turn;
            panelBox[0].transform.GetChild(1).gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            panelBox[0].transform.GetChild(1).gameObject.SetActive(false);
        }
    }

    public static IEnumerator PlayerPanelPop() // TurnPanel child �� 0 = player, 1 = enemy
    {
        if (turnAnchor != GameManager.Turn)
        {
            turnAnchor = GameManager.Turn;
            panelBox[0].transform.GetChild(0).gameObject.SetActive(true);
            yield return new WaitForSeconds(1);
            panelBox[0].transform.GetChild(0).gameObject.SetActive(false);
        }
    }
    
    //���� �����ϴ� ������ �� ����â�� ǥ�� �� �����Ѵ�.
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
            enemyStates[i].GetChild(1).GetComponent<Text>().text = "�ൿ�� : " + enemiesScript[i].moveCtrl[1] + " / 10";
            enemyStates[i].GetChild(2).GetComponent<Text>().text = "ü�� : " + enemiesScript[i].hp + " / " + enemies[i].GetComponent<Enemy>().maxHp;
            //enemyManager.sortingList.Add(i);
        }
        //EnemyStateSort();
        EnemyStatesArr();
    }
/*
    //�� ����â ���� ����
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
    //�� ����â�� ��ġ
    private void EnemyStatesArr()
    {
        float firstPosition = enemyStates[0].rect.height * -0.7f;
        enemyStates[enemyManager.sortingList[0]].anchoredPosition = new Vector2(0, firstPosition);
        for(int i = 1; i < enemyStates.Count; i++)
        {
            enemyStates[enemyManager.sortingList[i]].anchoredPosition = new Vector2(0, firstPosition - (enemyStates[i].rect.height + 10) * i);
        }
    }

    //�ൿ�� �ö󰡴� �ִϸ��̼�  (���° ���׹�����, ���� �ൿ��, ��ǥ �ൿ��)
    public IEnumerator MovectrlCountAnim(int enemyNum, int startCost, int goalCost)
    {
        if (goalCost > 10) goalCost = 10;
        enemyStates[enemyNum].GetComponent<Image>().DOFade(1, 0);
        DOVirtual.Int(startCost, goalCost, uiMoveTime, ((x) => { enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "�ൿ�� : " + x + " / " + enemiesScript[enemyNum].moveCtrl[0]; })).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(StateSwapAnim(enemyNum));
    }

    //�ൿ�¿� ���� ���� ����â ����
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

    // �ൿ�� ��� �� �� �Ʒ��� ������ ������ ���� �ø�.
    public IEnumerator ReloadState(int enemyNum, int goalCost)
    {
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x + 400, uiMoveTime);
        //CanvasGroup cg;
        //cg = enemyStates[enemyNum]
        yield return new WaitForSeconds(uiMoveTime);
        yield return StartCoroutine(StateSwapAnim(enemyNum));
        enemyStates[enemyNum].GetChild(1).GetComponent<Text>().text = "�ൿ�� : " + 0 + " / " + enemiesScript[enemyNum].moveCtrl[0];
        enemyStates[enemyNum].DOAnchorPosX(enemyStates[enemyNum].anchoredPosition.x - 400, uiMoveTime);
        yield return new WaitForSeconds(uiMoveTime);

        yield return StartCoroutine(MovectrlCountAnim(enemyNum, 0, goalCost));
    }
}
