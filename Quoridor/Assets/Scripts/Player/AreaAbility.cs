using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public delegate void EnterEvent(Enemy enemy);
public delegate void ExitEvent(Enemy enemy);

public class AreaAbility : MonoBehaviour
{
    public enum ELifeType { Turn, Count, Dummy }
    public ELifeType lifeType;
    public int life;  // 지속 기간
    public List<GameObject> targetList = new List<GameObject>(); // 타깃 오브젝트
    public List<Vector2Int> areaPositionList = new List<Vector2Int>();
    public bool canPenetrate;

    public EnterEvent enterEvent;

    public ExitEvent exitEvent;

    public GameObject sprite;

    int tempTurn;
    GameManager gameManager;
    EnemyManager enemyManager;
    Player player;
    List<GameObject> areaObjectList = new List<GameObject>();

    public List<GameObject> targetStayList = new List<GameObject>();
    public bool canDone;
    // Start is called before the first frame update
    void Awake()
    {
        tempTurn = GameManager.Turn;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        enemyManager = GameObject.Find("GameManager").GetComponent<EnemyManager>();
        gameManager = enemyManager.gameObject.GetComponent<GameManager>();
    }
    void FixedUpdate()
    {

    }
    // Update is called once per frame
    void Update()
    {
        if (tempTurn != GameManager.Turn)
        {
            tempTurn = GameManager.Turn;
            if (GameManager.Turn % 2 == Player.playerOrder)
            {
                if (lifeType == ELifeType.Turn)
                {
                    if (--life == 0)
                    {
                        OnAbilityDisable();
                    }
                }
            }
            canDone = false;
        }
        targetStayList.Clear();
        for (int i = 0; i < areaPositionList.Count; i++)
        {
            bool[] result = player.CheckRay(transform.position, areaPositionList[i]);
            if (result[0])
            {
                areaObjectList[i].SetActive(false);
            }
            if (result[1] || canPenetrate)
            {
                areaObjectList[i].SetActive(true);
                GameObject targetObject = EnemyManager.GetEnemyObject(transform.position + GameManager.ChangeCoord(areaPositionList[i]), false);
                if (targetObject != null) Event(targetObject);
            }
            else
            {
                areaObjectList[i].SetActive(false);
            }
        }
        EventExit();
        canDone = true;
    }
    public void SetUp()
    {
        for (int i = 0; i < areaPositionList.Count; i++)
        {
            // 임시 //
            areaObjectList.Add(Instantiate(sprite, this.transform));
            areaObjectList[i].transform.localPosition = (Vector2)areaPositionList[i];
        }
        if (lifeType == ELifeType.Dummy)
        {
            transform.tag = "PlayerDummy";
            gameObject.layer = LayerMask.NameToLayer("Token");
        }
        gameManager.areaAbilityList.Add(this);
    }
    void OnAbilityDisable()
    {
        for (int i = 0; i < areaPositionList.Count; i++)
        {
            if (areaObjectList[i].activeInHierarchy)
            {
                Enemy enemy = EnemyManager.GetEnemy(transform.position + GameManager.ChangeCoord(areaPositionList[i]), false);
                if (enemy != null) exitEvent(enemy);
            }
        }
        Destroy(this.gameObject);
    }
    void Event(GameObject targetObject)
    {
        if (targetObject.tag != "Enemy") return;
        Enemy target = targetObject.GetComponent<Enemy>();
        if (!targetList.Contains(targetObject))
        {
            targetList.Add(targetObject);
            enterEvent(target);
            if (lifeType == ELifeType.Count)
            {
                if (--life == 0)
                {
                    OnAbilityDisable();
                }
            }
        }
        if (!targetStayList.Contains(targetObject))
        {
            targetStayList.Add(targetObject);
        }
    }
    void EventExit()
    {
        List<GameObject> exitObject = targetList.Except(targetStayList).ToList();
        for (int i = 0; i < exitObject.Count; i++)
        {
            Debug.Log("Exit Event!!");
            targetList.Remove(exitObject[i]);
            if (exitObject[i] != null) exitEvent(exitObject[i].GetComponent<Enemy>());
        }
    }
}
