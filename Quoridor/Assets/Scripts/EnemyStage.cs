using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    // key == 현재 스테이지, value == 지금 생성해야하는 유닛 클래스
    public static Dictionary<int, SpawnList> stageEnemySettig = new Dictionary<int, SpawnList>();
    public static List<GameObject> normalEnemys;
    public static List<GameObject> championEnemys;
    public static List<GameObject> namedEnemys;
    public static List<GameObject> bossEnemys;

    public EnemyManager EM;

    GameManager gameManager;

    public void Start()
    {
        gameManager = transform.gameObject.GetComponent<GameManager>();
        stageEnemySettig.Clear();
        SpawnSettingStart();
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

    public void SpawnEnemyUnit()
    {
        int[] tmpValues = stageEnemySettig[gameManager.currentStage].values;
        for (int count = 0; count < tmpValues.Length; count++)
        {
            List<GameObject> tmpValueEnemys;
            switch (count)
            {
                case 0:
                    tmpValueEnemys = normalEnemys;
                    break;
                case 1:
                    tmpValueEnemys = championEnemys;
                    break;
                case 2:
                    tmpValueEnemys = namedEnemys;
                    break;
                case 3:
                    tmpValueEnemys = bossEnemys;
                    break;
            }

            for (int unitCount = 0; unitCount < tmpValues[count]; unitCount++)
            {

            }
        }
    }
}
