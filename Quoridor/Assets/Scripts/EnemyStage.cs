using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 스테이지마다 등급에 따라 유닛의 수를 정해두기
public class SpawnList
{
    public int[] values = new int[4]; // 0 = normal, 1 = champion, 2 = named, 3 = boss;

    public SpawnList(int Normal, int Champion, int Named, int Boss)
    {
        values[0] = Normal;
        values[1] = Champion;
        values[2] = Named;
        values[3] = Boss;
    }

    public int TotalReturn()
    {
        return (values[0] + values[1] + values[2] + values[3]);
    }
}

// 스테이지 적 관련해서 변수 관리 스크립트
public class EnemyStage : MonoBehaviour
{
    public static int totalEnemyCount = 0;

    // key == 현재 스테이지, value == 지금 생성해야하는 유닛 클래스
    public Dictionary<int, SpawnList> stageEnemySettig = new Dictionary<int, SpawnList>();
    public List<GameObject> normalEnemys;
    public List<GameObject> championEnemys;
    public List<GameObject> namedEnemys;
    public List<GameObject> bossEnemys;

    public EnemyManager EM;
    public GameManager gameManager;

    public GameObject stageUI;

    public void Start()
    {
        gameManager = transform.GetComponent<GameManager>();
        stageEnemySettig.Clear();
        SpawnSettingStart(); // 스테이지마다 등급별 소환 유닛 수 설정
        ShareEnemys(); // enemy 스크립트 안에 있는 value에 따라 등급 리스트 저장

        if (stageEnemySettig.ContainsKey(gameManager.currentStage))
        {
            totalEnemyCount = stageEnemySettig[gameManager.currentStage].TotalReturn();
            //if (!EM.LoadEnemyData())
            StageEnemySpawn();
        }
        else
        {
            // 딕셔너리에 값이 존재하지 않을때 ( key == 현재 스테이지 )
            Debug.LogError("지정된 스테이지가 아닙니다 다시 확인해주세요");
        }
    }

    public void Update()
    {
        // 모든 적 유닛이 사망했을때
        if (totalEnemyCount == 0)
        {
            Instantiate(stageUI);
            totalEnemyCount = -1;
        }
    }

    public void EnemyInitialied()
    {
        totalEnemyCount = stageEnemySettig[gameManager.currentStage].TotalReturn();
        StageEnemySpawn();
    }

    // 스테이지에 소환되는 유닛이 변경될때 건드리면 되는 함수
    public void SpawnSettingStart()
    {
        stageEnemySettig.Add(1, new SpawnList(2, 0, 0, 0));
        stageEnemySettig.Add(2, new SpawnList(2, 1, 0, 0));
        stageEnemySettig.Add(3, new SpawnList(1, 2, 0, 0));
        stageEnemySettig.Add(4, new SpawnList(0, 2, 1, 0));
        stageEnemySettig.Add(5, new SpawnList(0, 1, 2, 0));
        stageEnemySettig.Add(6, new SpawnList(1, 1, 2, 0));
        stageEnemySettig.Add(7, new SpawnList(0, 2, 2, 0));
        stageEnemySettig.Add(8, new SpawnList(0, 3, 2, 0));
        stageEnemySettig.Add(9, new SpawnList(0, 2, 3, 0));
        stageEnemySettig.Add(10, new SpawnList(1, 2, 3, 0));
    }

    // 노말 유닛, 정예 유닛들 enemy스크립트에서 등급별로 리스트에 넣어두는 함수
    public void ShareEnemys()
    {
        foreach (GameObject child in EM.enemyPrefabs)
        {
            int enemyValue = (int)child.GetComponent<Enemy>().value;
            switch (enemyValue)
            {
                case 0:
                    normalEnemys.Add(child);
                    break;
                case 1:
                    championEnemys.Add(child);
                    break;
                case 2:
                    namedEnemys.Add(child);
                    break;
                case 3:
                    bossEnemys.Add(child);
                    break;
            }
        }

        foreach (GameObject child in EM.loyalEnemyPrefabs)
        {
            int enemyValue = (int)child.GetComponent<Enemy>().value;
            switch (enemyValue)
            {
                case 0:
                    normalEnemys.Add(child);
                    break;
                case 1:
                    championEnemys.Add(child);
                    break;
                case 2:
                    namedEnemys.Add(child);
                    break;
                case 3:
                    bossEnemys.Add(child);
                    break;
            }
        }
    }

    public GameObject enemyStatePrefab; // 적 기물 상태 판넬안에 들어가는 기본 빵틀 이라고 생각;
    private UiManager uiManager;
    public void StageEnemySpawn()
    {
        uiManager = GetComponent<UiManager>();
        //적 상태창 리셋
        uiManager.ResetEnemyStates();

        SpawnList currentSpawn = stageEnemySettig[gameManager.currentStage];
        List<GameObject> currentValues;

        for (int spawnCount = 0; spawnCount < currentSpawn.values.Length; spawnCount++)
        {
            int enemyValue = spawnCount;
            switch (enemyValue)
            {
                case 0:
                    currentValues = normalEnemys;
                    break;
                case 1:
                    currentValues = championEnemys;
                    break;
                case 2:
                    currentValues = namedEnemys;
                    break;
                case 3:
                    currentValues = bossEnemys;
                    break;
                default:
                    Debug.LogError("적들 소환시 아무것도 할당 받지 못했습니다. / error : EnemyStage");
                    currentValues = normalEnemys;
                    break;
            }

            for (int count = 0; count < currentSpawn.values[spawnCount]; count++)
            {
                Vector3 enemyPosition;
                do
                {
                    enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0);
                }
                while (GameManager.enemyPositions.Contains(enemyPosition) && GameManager.enemyPositions.Count != 0); // 이미 소환된 적의 위치랑 안 겹칠때
                GameManager.enemyPositions.Add(enemyPosition);
                int value = Random.Range(0, currentValues.Count);
                GameObject currentEnemyObj = Instantiate(currentValues[value], GameManager.gridSize * GameManager.enemyPositions[GameManager.enemyPositions.Count - 1], Quaternion.identity);
                GameManager.enemyObjects.Add(currentEnemyObj);

                // 유닛 판넬안에 보드위에 있는 적들 데이터 정보를 넣는 부분
                Enemy currentEnemey = currentEnemyObj.GetComponent<Enemy>();
                currentEnemey.moveCtrl[1] = Random.Range(0, 3);
                currentEnemey.type = value;
                currentEnemey.index = count;


                // 적 정보 UI 판넬에 표시하는 부분
                //GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("EnemyStateContent").transform);
                GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("Canvas(Clone)").transform.GetChild(3).GetChild(1).GetChild(0));
                /*
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentEnemyObj.GetComponent<SpriteRenderer>().sprite;
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().color = currentEnemyObj.GetComponent<SpriteRenderer>().color;
                currentEnemyState.transform.GetChild(1).GetComponent<Text>().text = "행동력 " + currentEnemey.moveCtrl[1] + " / 10";
                currentEnemey.maxHp = currentEnemey.hp;
                currentEnemyState.transform.GetChild(2).GetComponent<Text>().text = "체력 " + currentEnemey.hp + " / " + currentEnemey.maxHp;
                */
                uiManager.CreateEnemyState(currentEnemyState, currentEnemyObj, currentEnemey); //적 각각의 상태창을 만들어내는 함수
            }
        }
        uiManager.CreateSortingList(GameManager.enemyObjects.Count);
        uiManager.SortEnemyStates(); //상태창 순서 정렬 (행동력 기준으로)
        uiManager.DeploymentEnemyStates(); //상태창 각각 배치
    }
}
