using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public static int totalEnemyCount = 0;

    // key == ���� ��������, value == ���� �����ؾ��ϴ� ���� Ŭ����
    public Dictionary<int, SpawnList> stageEnemySettig = new Dictionary<int, SpawnList>();
    public List<GameObject> normalEnemys;
    public List<GameObject> championEnemys;
    public List<GameObject> namedEnemys;
    public List<GameObject> bossEnemys;

    public EnemyManager EM;
    public GameManager gameManager;

    public void Start()
    {
        gameManager = transform.GetComponent<GameManager>();
        stageEnemySettig.Clear();
        SpawnSettingStart(); // ������������ ��޺� ��ȯ ���� �� ����
        ShareEnemys(); // enemy ��ũ��Ʈ �ȿ� �ִ� value�� ���� ��� ����Ʈ ����

        if (stageEnemySettig.ContainsKey(gameManager.currentStage))
        {
            totalEnemyCount = stageEnemySettig[gameManager.currentStage].TotalReturn();
            StageEnemySpawn();
        }
        else
        {
            // ��ųʸ��� ���� �������� ������ ( key == ���� �������� )
            Debug.LogError("������ ���������� �ƴմϴ� �ٽ� Ȯ�����ּ���");
        }
    }

    public void Update()
    {
        // ��� �� ������ ���������
        if (totalEnemyCount == 0)
        {

        }
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

    public GameObject enemyStatePrefab; // �� �⹰ ���� �ǳھȿ� ���� �⺻ ��Ʋ �̶�� ����.
    public void StageEnemySpawn()
    {
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
                    Debug.LogError("���� ��ȯ�� �ƹ��͵� �Ҵ� ���� ���߽��ϴ�. / error : EnemyStage");
                    currentValues = normalEnemys;
                    break;
            }

            for(int count = 0; count < currentSpawn.values[spawnCount]; count++)
            {
                Vector3 enemyPosition;
                do
                {
                    enemyPosition = new Vector3(Random.Range(-4, 5), Random.Range(3, 5), 0);
                }
                while (GameManager.enemyPositions.Contains(enemyPosition) && GameManager.enemyPositions.Count != 0); // �̹� ��ȯ�� ���� ��ġ�� �� ��ĥ��
                GameManager.enemyPositions.Add(enemyPosition);
                GameObject currentEnemyObj = Instantiate(currentValues[Random.Range(0, currentValues.Count)], GameManager.gridSize * GameManager.enemyPositions[GameManager.enemyPositions.Count - 1], Quaternion.identity);
                GameManager.enemyObjects.Add(currentEnemyObj);

                // ���� �ǳھȿ� �������� �ִ� ���� ������ ������ �ִ� �κ�
                Enemy currentEnemey = currentEnemyObj.GetComponent<Enemy>();
                currentEnemey.moveCtrl[1] = Random.Range(0, 3);

                // �� ���� UI �ǳڿ� ǥ���ϴ� �κ�
                GameObject currentEnemyState = Instantiate(enemyStatePrefab, GameObject.Find("EnemyStateContent").transform);
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = currentEnemyObj.GetComponent<SpriteRenderer>().sprite;
                currentEnemyState.transform.GetChild(0).GetChild(0).GetComponent<Image>().color = currentEnemyObj.GetComponent<SpriteRenderer>().color;
                currentEnemyState.transform.GetChild(1).GetComponent<Text>().text = "�ൿ�� " + currentEnemey.moveCtrl[1] + " / 10";
                currentEnemey.maxHp = currentEnemey.hp;
                currentEnemyState.transform.GetChild(2).GetComponent<Text>().text = "ü�� " + currentEnemey.hp + " / " + currentEnemey.maxHp;
            }
        }
    }
}
