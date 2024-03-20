using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public delegate void EnterEvent(Enemy enemy);
public delegate void StayEvent(Enemy enemy);
public delegate void ExitEvent(Enemy enemy);

public class AreaAbility : MonoBehaviour
{
    public enum ELifeType { Turn, Count }
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
    EnemyManager enemyManager;
    Player player;
    List<BoxCollider2D> boxColliderList = new List<BoxCollider2D>();
    List<GameObject> areaObjectList = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        tempTurn = GameManager.Turn;
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Player>();
        enemyManager = GameObject.Find("GameManager").GetComponent<EnemyManager>();
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
        if (GameManager.Turn % 2 == Player.playerOrder && tempTurn != GameManager.Turn)
        {
            tempTurn = GameManager.Turn;
            if (lifeType == ELifeType.Turn)
            {
                if (--life == 0)
                {
                    OnAbilityDisable();
                }
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
            enterEvent(other.GetComponent<Enemy>());
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
        if (other.tag == "Enemy") stayEvent(other.GetComponent<Enemy>());
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag == "Enemy") exitEvent(other.GetComponent<Enemy>());
    }
}
