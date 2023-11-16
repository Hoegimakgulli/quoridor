using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions  = new List<Vector3>();    // ��� ���� ��ġ ���� ����
    public static List<GameObject> enemyObjects = new List<GameObject>(); // ��� �� �⹰ ������Ʈ ����

    //-------------- Enemy Values --------------//
    public int cost;                                  // ��ȯ �� �ʿ��� ���
    public int hp;                                    // �޾ƾ��ϴ� �� ü��
    public int[] moveCtrl = new int[2];               // 0 = �䱸 �ൿ��, 1 = ���� ä���� �ִ� �ൿ��
    public int[] unitMoveVector = new int[8];         // 0 = ��, 1 = ������, 2 = �Ʒ�, 3 = ����, 4 = ������, 5 = ��������, 6 = �����ʾƷ�, 7 = ���ʾƷ�
    //public int[] unitMoveVector = new int[2];         // 0 = + , 1 = x
    public enum EState { Idle, Move, Attack, Dead };
    public enum ECharacteristic { Forward, BackWard, Hold }; // 0 - ������ player�� ����, 1 - �� �������� �����鼭 �÷��̾� ����, 2 - �ڱ� ������ ����ϸ鼭 �÷��̾ ����
    //------------------------------------------//

    public EState state = EState.Idle;
    public ECharacteristic characteristic = ECharacteristic.Forward;

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
        int anchor = 0;
        Vector2 unitPos = transform.position / GameManager.gridSize;
        Vector2 fixPos = new Vector2(0, 0);
        unitPos = new Vector2Int(Mathf.FloorToInt(unitPos.x) + 4, Mathf.FloorToInt(unitPos.y) + 4);

        for(int count = 1; count < path.Count; count++)
        {
            Vector2Int movePath = new Vector2Int(Mathf.FloorToInt(path[count].x - unitPos.x), Mathf.FloorToInt(path[count].y - unitPos.y));
            if(movePath.x == 0 && (anchor == 0 || anchor == 1)) // �� �Ʒ�
            {
                if(movePath.y < 0) // �Ʒ�
                {
                    if(Mathf.Abs(movePath.y) <= unitMoveVector[2])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 1;
                    }
                    else
                    {
                        break;
                    }
                }
                else // ��
                {
                    if (Mathf.Abs(movePath.y) <= unitMoveVector[0])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 1;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            else if(movePath.y == 0 && (anchor == 0 || anchor == 2)) // �� ��
            {
                if(movePath.x < 0) // ����
                {
                    if (Mathf.Abs(movePath.x) <= unitMoveVector[3])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 2;
                    }
                    else
                    {
                        break;
                    }
                }
                
                else // ������
                {
                    if (Mathf.Abs(movePath.y) <= unitMoveVector[1])
                    {
                        fixPos = new Vector2(path[count].x, path[count].y);
                        anchor = 2;
                    }
                    else
                    {
                        break;
                    }
                }
            }

            else if(anchor == 0 || anchor == 3) // �밢
            {
                Debug.Log("�밢 �̵�");
            }

            else // ������ �ٲ�ų� ���� ���ϰ��
            {
                break;
            }
        }

        transform.position = new Vector3((fixPos.x - 4) * GameManager.gridSize, (fixPos.y - 4) * GameManager.gridSize, 0);
        if (canAttack == true)
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
    private bool canAttack = false;
    // playerAttack �Լ� Raycast���
    public void AttackPlayer()
    {
        if (canAttack && state == EState.Attack)
        {
            Vector2 playerPos = GameObject.Find("Player").transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, playerPos - (Vector2)transform.position, 15f, LayerMask.GetMask("Player")); // enemy ��ġ���� player���� ray���
            if(hit != false) // ���� �ε�ġġ �ʾҴٸ�
            {
                Debug.Log("Player Dead");
                Destroy(hit.transform.gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.name == "Player")
        {
            canAttack = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.transform.name == "Player")
        {
            canAttack = false;
        }
    }
    //--------------- Attack ���� ---------------//
}
