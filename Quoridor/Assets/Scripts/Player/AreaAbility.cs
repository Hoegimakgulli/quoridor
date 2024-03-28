using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public delegate void EnterEvent(Enemy enemy);
public delegate void StayEvent(Enemy enemy);
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
    public StayEvent stayEvent;
    public ExitEvent exitEvent;

    public GameObject sprite;

    int tempTurn;
    GameManager gameManager;
    EnemyManager enemyManager;
    Player player;
    List<BoxCollider2D> boxColliderList = new List<BoxCollider2D>();
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

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < areaPositionList.Count; i++)
        {
            bool[] result = player.CheckRay(transform.position, areaPositionList[i]);
            if (result[0])
            {
                // boxColliderList[i].enabled = false;
                areaObjectList[i].SetActive(false);
            }
            if (result[1] || canPenetrate)
            {
                // boxColliderList[i].enabled = true;
                areaObjectList[i].SetActive(true);
                GameObject targetObject = EnemyManager.GetEnemyObject(transform.position + GameManager.ChangeCoord(areaPositionList[i]), false);
                if (targetObject != null) Event(targetObject);
            }
            else
            {
                // boxColliderList[i].enabled = false;
                areaObjectList[i].SetActive(false);
            }
        }
        EventExit();
        canDone = true;
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
            else
            {
                targetStayList.Clear();
                canDone = false;
                // Debug.Log("Cleared");
                // foreach (var go in targetList)
                // {
                //     Debug.Log(go.name);
                // }
            }
        }
    }
    public void SetUp()
    {
        for (int i = 0; i < areaPositionList.Count; i++)
        {
            BoxCollider2D boxCollider = transform.AddComponent<BoxCollider2D>();
            boxCollider.isTrigger = true;
            boxCollider.offset = areaPositionList[i];
            boxColliderList.Add(boxCollider);

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
        }
        if (!targetStayList.Contains(targetObject))
        {
            targetStayList.Add(targetObject);
            stayEvent(target);
        }
    }
    void EventExit()
    {
        List<GameObject> exitObject = targetList.Except(targetStayList).ToList();
        for (int i = 0; i < exitObject.Count; i++)
        {
            targetList.Remove(exitObject[i]);
            if (exitObject[i] != null) exitEvent(exitObject[i].GetComponent<Enemy>());
        }
    }
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (other.tag == "Enemy")
    //     {
    //         enterEvent(other.GetComponent<Enemy>());
    //         if (lifeType == ELifeType.Count)
    //         {
    //             if (--life == 0)
    //             {
    //                 OnAbilityDisable();
    //             }
    //         }
    //     }
    // }
    // private void OnTriggerStay2D(Collider2D other)
    // {
    //     if (other.tag == "Enemy")
    //     {
    //         Debug.Log(other.name);
    //         if (!targetList.Contains(other.gameObject))
    //         {
    //             Debug.Log(other.gameObject.name);
    //             targetList.Add(other.gameObject);
    //             stayEvent(other.GetComponent<Enemy>());
    //         }
    //     }
    // }
    // private void OnTriggerExit2D(Collider2D other)
    // {
    //     if (other.tag == "Enemy")
    //     {
    //         exitEvent(other.GetComponent<Enemy>());
    //     }
    // }
}
