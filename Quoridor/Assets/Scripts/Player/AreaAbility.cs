using System.Collections;
using System.Collections.Generic;
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

    public int enemyCount;
    public int counter;
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
                boxColliderList[i].enabled = false;
                areaObjectList[i].SetActive(false);
            }
            if (result[1] || canPenetrate)
            {
                boxColliderList[i].enabled = true;
                areaObjectList[i].SetActive(true);
            }
            else
            {
                boxColliderList[i].enabled = false;
                areaObjectList[i].SetActive(false);
            }
        }
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
                counter = 0;
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
            if (boxColliderList[i].enabled)
            {
                Enemy enemy = enemyManager.GetEnemy(transform.position + GameManager.ChangeCoord(areaPositionList[i]), false);
                if (enemy != null) exitEvent(enemy);
            }
        }
        Destroy(this.gameObject);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            Debug.Log("Start Ontrriger");
            enterEvent(other.GetComponent<Enemy>());
            enemyCount++;
            if (lifeType == ELifeType.Count)
            {
                if (--life == 0)
                {
                    OnAbilityDisable();
                }
            }
        }
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            if (!targetList.Contains(other.gameObject)) targetList.Add(other.gameObject);
            if (counter < enemyCount)
            {
                stayEvent(other.GetComponent<Enemy>());
                counter++;
            }
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Enemy")
        {
            exitEvent(other.GetComponent<Enemy>());
            enemyCount--;
        }
    }
}
