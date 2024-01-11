using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions  = new List<Vector3>();    // ��� ���� ��ġ ���� ����
    public static List<GameObject> enemyObjects = new List<GameObject>(); // ��� �� �⹰ ������Ʈ ����

    //-------------- Enemy Values --------------//
    public int cost;                                  // ��ȯ �� �ʿ��� ���
    public int hp;                                    // �޾ƾ��ϴ� �� ü��
    public int maxHp;
    public int[] moveCtrl = new int[3];               // 0 = �䱸 �ൿ��, 1 = ���� ä���� �ִ� �ൿ��, 2 = ���� �ൿ�� ���� �ִ�ġ
    public Vector2Int[] moveablePoints;
    public Vector2Int[] attackablePoints;
    public enum EState { Idle, Move, Attack, Dead };
    public enum EValue { Normal = 0, Champion = 1, Named = 2, Boss = 3 }; // 0 = Normal, 1 = Champion, 2 = Named, 3 = Boss
    // 0 - ������ player�� ����, 1 - �� �������� �����鼭 �÷��̾� ����, 2 - �ڱ� ������ ����ϸ鼭 �÷��̾ ���� 
    // ���� ���� Ư�� ��ӹ��� �� ��� ���� �̻��.
    public enum ECharacteristic { Forward, BackWard, Hold };
    //------------------------------------------//

    public EState state = EState.Idle;
    public ECharacteristic characteristic = ECharacteristic.Forward;
    public EValue value = EValue.Normal;


    //--------------- Move ���� ---------------//
    // ��� enemy ��ü ���ÿ� ������ �ǽ�
    public void EnemyMove(List<Path> path)
    {
        // enemy�� Move ������ �� ������ Ư¡�� ���� �����̴� ���� ����
        if (state == EState.Move)
        {
            if(characteristic == ECharacteristic.Forward)
            {
                GetShortRoad(path);
            }
            else if (characteristic == ECharacteristic.BackWard)
            {
                GetBackRoad();
            }
            else if (characteristic == ECharacteristic.Hold)
            {
                GetHoldRoad();
            }
        }
    }

    // A* �˰���
    public void GetShortRoad(List<Path> path)
    {
        Vector2 playerPos = GameObject.FindWithTag("Player").transform.position / GameManager.gridSize;
        if (!AttackCanEnemy())
        {
            Vector2 unitPos = transform.position / GameManager.gridSize;
            Vector2 fixPos = new Vector2(0, 0);
            unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

            for (int count = 1; count < path.Count; count++)
            {
                Vector2 pathPoint = new Vector2(path[count].x, path[count].y);
                int moveCount;
                for (moveCount = 0; moveCount < moveablePoints.Length; ++moveCount)
                {
                    Vector2 currentMovePoint = unitPos + moveablePoints[moveCount];
                    if (pathPoint == currentMovePoint && currentMovePoint != playerPos)
                    {
                        fixPos = currentMovePoint;
                        break;
                    }
                }
                if (moveCount == moveablePoints.Length)
                {
                    break;
                }
            }

            transform.position = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
            state = EState.Attack;
            AttackPlayer();
        }
        else
        {
            state = EState.Attack;
            AttackPlayer();
        }
    }
    
    // ���� ����
    public void GetBackRoad()
    {

    }
    // ���� ����
    public void GetHoldRoad()
    {

    }
    //--------------- Move ���� ---------------//

    //--------------- Die ���� ---------------//
    // Attack �޾��� �� �����ϴ� �Լ�
    public void DieEnemy()
    {
        if (state == EState.Dead)
        {
            Debug.Log("Enemy Dead : " + transform.name);
            Destroy(transform.gameObject);
        }
    }
    //--------------- Die ���� ---------------//

    //--------------- Attack ���� ---------------//
    // playerAttack �Լ� Raycast���
    public void AttackPlayer()
    {
        if (AttackCanEnemy() && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Token")); // enemy ��ġ���� player���� ray���
            if(hit.transform.tag == "Player") // ���� ray�� Player �±׸� ������ �ִٸ�
            {
                Debug.Log("Player Dead");
                Destroy(hit.transform.gameObject);
            }
        }
        else
        {
            state = EState.Idle;
        }
    }

    public bool AttackCanEnemy()
    {
        int attackCount;
        Vector2 currentAttackPoint;
        Vector2 playerPos = GameObject.FindGameObjectWithTag("Player").transform.position / GameManager.gridSize;
        playerPos = new Vector2Int(Mathf.FloorToInt(playerPos.x), Mathf.FloorToInt(playerPos.y));
        Vector2 enemyPos = transform.position / GameManager.gridSize;
        enemyPos = new Vector2Int(Mathf.FloorToInt(enemyPos.x), Mathf.FloorToInt(enemyPos.y));

        for (attackCount = 0; attackCount < attackablePoints.Length; ++attackCount)
        {
            currentAttackPoint = enemyPos + attackablePoints[attackCount];
            if(playerPos == currentAttackPoint)
            {
                Debug.Log("���� ����");
                return true;
            }
        }
        return false;
    }
    //--------------- Attack ���� ---------------//

    Sequence shakeSequence;
    public GameObject fadeObj;
    public bool useShake = true;

    public void Start()
    {
        Material tmpObj = Instantiate(fadeObj, transform.position, Quaternion.identity, transform).GetComponent<SpriteRenderer>().material;
        shakeSequence = DOTween.Sequence()
            .SetAutoKill(false)
            .OnStart(() => {
                tmpObj.color = new Color(1, 1, 1, 0);
        });
        //shakeSequence.Append(tmpObj.DOFade(0, 1).SetEase(Ease.Linear));
        shakeSequence.Append(tmpObj.DOFade(1f, 1).SetEase(Ease.Linear));
        shakeSequence.Append(tmpObj.DOFade(0, 1).SetEase(Ease.Linear));
        //shakeSequence.Append(tmpObj.DOFade(1f, 1).SetEase(Ease.Linear));
    }

    public void Update()
    {
        if (useShake && GameManager.Turn % 2 == 1 && moveCtrl[1] + moveCtrl[2] >= 10)
        {
            Debug.Log("Start shake");
            useShake = false;
            StartCoroutine(ShakeTokenAction());
        }
    }

    IEnumerator ShakeTokenAction()
    {
        shakeSequence.Restart();
        yield return new WaitForSeconds(shakeSequence.Duration());
        useShake = true;
    }
}
