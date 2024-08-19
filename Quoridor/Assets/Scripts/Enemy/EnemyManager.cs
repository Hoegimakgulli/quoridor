using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HM.Containers;
using HM.Utils;
using DG.Tweening;

[System.Serializable]
public class Path
{
    // 중앙 좌표상 (0, 0) 시작으로 x, y 좌표
    // G = 시작으로부터 이동한 거리, H = 가로, 세로로 벽을 무시하고 Player까지 이동한 거리
    public int x, y, G, H;

    public Path(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public Path ParentNode;

    // F = G, H 총 합산값
    public int F
    {
        get
        {
            return G + H;
        }
    }
}

public class EnemyManager : MonoBehaviour
{
    public List<GameObject> enemyPrefabs; // 모든 유닛들 통합으로 관리

    public GameObject enemyBoxPrefab; // 경고 표기 담아두는 박스
    static GameObject EnemyBox;
    public GameObject enemyUiCanvas;

    private bool enemyTurnAnchor = true;
    public GameManager gameManager;

    private void Awake()
    {
        SetEnemyBox();
        Debug.Log("ui Spawned");
        gameManager = GameManager.Instance;
        GameManager.enemyValueList.Clear();
    }

    private void Start()
    {
    }

    void Update()
    {
        // 적 턴일때 (이동 및 공격확인)
        if (GameManager.Turn % 2 == 0 && enemyTurnAnchor && gameManager.canEnemyTurn)
        {
            enemyTurnAnchor = false;

            //EnemyMoveStart();
            StartCoroutine(StartEnemyTurn());
        }
    }

    static public bool turnCheck = false;

    IEnumerator StartEnemyTurn()
    {
        //yield return new WaitForSeconds(0.01f);
        //MoveCtrlUpdate();

        yield return StartCoroutine(MoveCtrlUpdateCoroutine());
        UiManager uiManager = GetComponent<UiManager>();
        //while (uiManager.)
        //enemyTurnAnchor = true;
    }

    //적 턴이 전부 끝났을 때 호출됨.
    public void EnemyTurnAnchorTrue()
    {
        enemyTurnAnchor = true;
        GameManager.Turn++;
        gameManager.PlayerTurnSet(); //플레이어 턴이 시작됨을 알림
    }

    //적 움직임 상태창 애니메이션에 맞춰 순차적으로 움직이도록 수정 (이규빈)
    IEnumerator MoveCtrlUpdateCoroutine()
    {
        UiManager uiManager = GetComponent<UiManager>();
        List<int> originSortingList = new List<int>(); //적들이 움직이기 전에 행동력 순서로 분류되어있던 리스트
        for (int i = 0; i < uiManager.sortingList.Count; i++)
        {
            originSortingList.Add(uiManager.sortingList[i]);
        }

        Enemy currentEnemyState;
        int count;
        int originMoveCtrl;  //원래 행동력.
        for (count = 0; count < GameManager.enemyValueList.Count; count++)
        {
            currentEnemyState = GetEnemy(GameManager.enemyValueList[originSortingList[count]].position);
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl;

            if (currentEnemyState.debuffs[Enemy.EDebuff.CantMove] == 0)
            {
                GameManager.enemyValueList[originSortingList[count]].moveCtrl += currentEnemyState.moveCtrl[2]; // 랜덤으로 들어오는 무작위 행동력 0 ~ 적 행동력 회복 최대치
            }

            uiManager.SortEnemyStates(); //행동력에 따라 적 상태창 순서 정렬
            yield return StartCoroutine(uiManager.CountMovectrlAnim(originSortingList[count], originMoveCtrl, GameManager.enemyValueList[originSortingList[count]].moveCtrl, false)); //원래 행동력에서 바뀐 행동력까지 숫자가 바뀌는 애니메이션
            originMoveCtrl = GameManager.enemyValueList[originSortingList[count]].moveCtrl; //여기서부터 originMoveCtrl은 바뀐 후의 행동력
            if (count == GameManager.enemyValueList.Count - 1)
            {
                if (currentEnemyState.moveCtrl[0] > GameManager.enemyValueList[originSortingList[count]].moveCtrl)
                {
                    EnemyTurnAnchorTrue();
                }
            }
            if (currentEnemyState.moveCtrl[0] <= GameManager.enemyValueList[originSortingList[count]].moveCtrl)
            {
                bool isPlayer = true; // true == player, false == dump
                GameObject currenEnemy = GetEnemyObject(GameManager.enemyValueList[originSortingList[count]].position);

                GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
                List<Vector2> shortPath = new List<Vector2>();
                List<Vector2> FinalVectorList = new List<Vector2>();

                isPlayer = GameObject.FindWithTag("PlayerDummy") ? false : true;
                Debug.LogFormat("player : {0}, enemy : {1}", players[0], currenEnemy);
                shortPath = PathFinding.GetAStarGameObject(currenEnemy, GameObject.FindWithTag("PlayerDummy") ? GameObject.FindWithTag("PlayerDummy") : players[0]);
                for (int sortCount = 0; sortCount < players.Length; sortCount++)
                {
                    FinalVectorList = PathFinding.GetAStarGameObject(currenEnemy, players[sortCount]);

                    // 모든 player랑 비교해서 가장 가까운 player 위치로 이동함
                    if (shortPath.Count > FinalVectorList.Count)
                    {
                        isPlayer = true;
                        shortPath = FinalVectorList;
                    }
                }

                currentEnemyState.state = Enemy.EState.Move;
                currentEnemyState.EnemyMove(shortPath, isPlayer);

                GameManager.enemyValueList[originSortingList[count]].moveCtrl = -1; //상태창 순서를 행동력 순으로 정렬했을 때, 방금 이동한 적의 순서가 가장 아래로 내려오도록 행동력을 마이너스로 수정.
                uiManager.SortEnemyStates(); //방금 이동한 적의 상태창이 아래로 내려오도록 리스트를 재정렬
                GameManager.enemyValueList[originSortingList[count]].moveCtrl = originMoveCtrl - currentEnemyState.moveCtrl[0]; //적의 행동력 감소
                currentEnemyState.moveCtrl[1] = GameManager.enemyValueList[originSortingList[count]].moveCtrl; // 현재 이동한 유닛 행동력 변경
                yield return StartCoroutine(uiManager.ReloadState(originSortingList[count], GameManager.enemyValueList[originSortingList[count]].moveCtrl, count));
            }
            currentEnemyState.UpdateTurn(); // 매턴마다 시행 - 동현
        }
    }

    public void SetEnemyBox()
    {
        if (EnemyBox != null) Destroy(EnemyBox);
        EnemyBox = Instantiate(enemyBoxPrefab);
        // Debug.Log(EnemyBox.name);
    }
    public static GameObject GetEnemyObject(Vector3 position, bool shouldLog = true)
    {
        foreach (Transform child in EnemyBox.transform)
        {
            // Debug.Log(child.position);
            if (child.position == position)
            {
                return child.gameObject;
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 Enemy 오브젝트를 찾지 못했습니다.");
        return null;
    }
    public static EnemyValues GetEnemyValues(Vector3 position, bool shouldLog = true)
    {
        foreach (EnemyValues child in GameManager.enemyValueList)
        {
            if (child.position == position)
            {
                return child;
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 EnemyValues도 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }
    public static Enemy GetEnemy(Vector3 position, bool shouldLog = true)
    {
        foreach (Transform child in EnemyBox.transform)
        {
            // Debug.Log(child.position);
            if (child.position == position)
            {
                return child.GetComponent<Enemy>();
            }
        }
        if (shouldLog) Debug.LogError("EnemyManager error : 어떤 Enemy 스크립트를 찾지 못했습니다.");
        return null;
    }
    public static GameObject GetEnemyObject(out GameObject enemyObject, Vector3 position, bool shouldLog = true)
    {
        return enemyObject = GetEnemyObject(position, shouldLog);
    }
    public static EnemyValues GetEnemyValues(out EnemyValues enemyValues, Vector3 position, bool shouldLog = true)
    {
        return enemyValues = GetEnemyValues(position, shouldLog);
    }
    public static Enemy GetEnemy(out Enemy enemy, Vector3 position, bool shouldLog = true)
    {
        return enemy = GetEnemy(position, shouldLog);
    }
    public static GameObject GetEnemyObject(int index)
    {
        return EnemyBox.transform.GetChild(index).gameObject;
    }
    public static EnemyValues GetEnemyValues(int index)
    {
        return GameManager.enemyValueList[index];
    }
    public static Enemy GetEnemy(int index)
    {
        return EnemyBox.transform.GetChild(index).GetComponent<Enemy>();
    }
}
