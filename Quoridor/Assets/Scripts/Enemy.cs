using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour, IMove, IAttack, IDead
{
    public static List<Vector3> enemyPositions = new List<Vector3>(); // ��� ���� ��ġ ���� ����
    public static List<GameObject> enemyObjects = new List<GameObject>(); // ��� �� �⹰ ������Ʈ ����

    //-------------- Enemy Values --------------//
    public int cost;
    public int hp;
    public int[] moveCtrl = new int[2];
    public int characteristic;
    public enum EState { Idle, Move, Attack, Dead };
    enum ECharacteristic { Forward, BackWard, Hold }; // 0 - ������ player�� ����, 1 - �� �������� �����鼭 �÷��̾� ����, 2 - �ڱ� ������ ����ϸ鼭 �÷��̾ ����
    //------------------------------------------//

    public EState state = EState.Idle;

    //--------------- Move ���� ---------------//
    // ��� enemy ��ü ���ÿ� ������ �ǽ�
    public void EnemyMove(List<Path> path)
    {
        int currentCharacter = transform.GetComponent<Enemy>().characteristic;
        // enemy�� Move ������ �� ������ Ư¡�� ���� �����̴� ���� ����
        if (state == EState.Move)
        {
            if(currentCharacter == (int)ECharacteristic.Forward)
            {
                GetShortRoad(path);
            }
            else if (currentCharacter == (int)ECharacteristic.BackWard)
            {
                GetBackRoad();
            }
            else if (currentCharacter == (int)ECharacteristic.Hold)
            {
                GetHoldRoad();
            }
        }
    }

    // A* �˰���
    public void GetShortRoad(List<Path> path)
    {
        
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
