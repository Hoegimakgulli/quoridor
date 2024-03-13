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
            StageEnemySelect();
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
        StageEnemySelect();
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
    }

    public GameObject enemyStatePrefab; // 적 기물 상태 판넬안에 들어가는 기본 빵틀 이라고 생각;
    private UiManager uiManager;

    public void StageEnemySelect()
    {
        uiManager = GetComponent<UiManager>();
        //적 상태창 리셋
        uiManager.ResetEnemyStates();

        SpawnList currentSpawn = stageEnemySettig[gameManager.currentStage];
        List<GameObject> currentValues;

        for (int spawnCount = 0; spawnCount < currentSpawn.values.Length; spawnCount++) // 현재 어떤 등급의 적들이 소환되어야하는지 정하기
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
                GameObject tmpCurrentObj = currentValues[Random.Range(0, currentValues.Count)]; // 랜덤으로 적 오브젝트 생성 현재 소환되야하는 기물 등급에 따라서 결정됨
                do
                {
                    if (tmpCurrentObj.name.Contains("EnemyCavalry")) // 기마병 한정 소환 위치 조정
                    {
                        enemyPosition = new Vector3(Random.Range(-4, 5), 4, 0);
                    }
                    else
                    {
                        enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0);
                        //enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0);
                    }
                }
                while (GameManager.enemyPositions.Contains(enemyPosition) && GameManager.enemyPositions.Count != 0); // 이미 소환된 적의 위치랑 안 겹칠때

                // enemyValues 클래스에 값 넣어주기
                Enemy tmpCurrentObjEnemy = tmpCurrentObj.GetComponent<Enemy>();
                tmpCurrentObjEnemy.moveCtrl[1] = Random.Range(0, tmpCurrentObjEnemy.moveCtrl[2] + 1);
                EnemyValues enemyItem = new EnemyValues(tmpCurrentObjEnemy.maxHp, tmpCurrentObjEnemy.moveCtrl[1], StageEnemySelectNum(tmpCurrentObj.name), count, enemyPosition * GameManager.gridSize); // 기본 적 구조체 생성
                GameManager.enemyValueList.Add(enemyItem); // 구조체 리스트에 넣어주기

                GameManager.enemyPositions.Add(enemyPosition);
            }
        }

        StageEnemySpawn();
    }

    public int StageEnemySelectNum(string name)
    {
        int selectNum = 0;
        foreach (GameObject child in EM.enemyPrefabs)
        {
            if (child.name.Contains(name))
            {
                break;
            }
            selectNum++;
        }
        return selectNum;
    }

    public void StageEnemySpawn()
    {
        foreach (EnemyValues child in GameManager.enemyValueList)
        {
            GameObject currentEnemyObj = Instantiate(EM.enemyPrefabs[child.uniqueNum], child.position, Quaternion.identity, GameObject.FindWithTag("EnemyBox").transform);
            if (EM.enemyPrefabs[child.uniqueNum].name.Contains("EnemyShieldSoldier")) // 소환되는 오브젝트가 방패병일 경우 맵그래프 변경
            {
                Vector3 enemyPosition = child.position / GameManager.gridSize;
                int currentShieldPos = (int)(enemyPosition.x + 4 + ((enemyPosition.y + 4) * 9)); // mapgraph 좌표로 변환
                if (currentShieldPos + 9 < 81 && gameManager.mapGraph[currentShieldPos, currentShieldPos + 9] == 1) // 만약 쉴드가 보드만 외벽에 붙어있는지 확인 조건식에 부합하면 벽취급으로 변환
                {
                    currentEnemyObj.GetComponent<Enemy>().ShieldTrue = true;
                    gameManager.mapGraph[currentShieldPos, currentShieldPos + 9] = 0;
                    gameManager.mapGraph[currentShieldPos + 9, currentShieldPos] = 0;
                }
            }

            GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("Canvas(Clone)").transform.GetChild(3).GetChild(1).GetChild(0));
            uiManager.CreateEnemyState(currentEnemyState, currentEnemyObj, currentEnemyObj.GetComponent<Enemy>(), child.spawnNum); //적 각각의 상태창을 만들어내는 함수
        }

        uiManager.CreateSortingList(GameManager.enemyValueList.Count);
        uiManager.SortEnemyStates(); //상태창 순서 정렬 (행동력 기준으로)
        uiManager.DeploymentEnemyStates(); //상태창 각각 배치
        gameManager.PlayerTurnSet(); //플레이어 턴 시작 시 실행되어야 할것잉 모여있는 함수 (규빈 작성)
    }
}
