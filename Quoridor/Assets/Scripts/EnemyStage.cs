using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ������������ ��޿� ���� ������ ���� ���صα�
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

// �������� �� �����ؼ� ���� ���� ��ũ��Ʈ
public class EnemyStage : MonoBehaviour
{
    // key == ���� ��������, value == ���� �����ؾ��ϴ� ���� Ŭ����
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

    // ���������� ��ȯ�Ǵ� ������ ����ɶ� �ǵ帮�� �Ǵ� �Լ�
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

    // �븻 ����, ���� ���ֵ� enemy��ũ��Ʈ���� ��޺��� ����Ʈ�� �־�δ� �Լ�
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
