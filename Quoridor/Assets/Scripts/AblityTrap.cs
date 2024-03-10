using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AblityTrap : MonoBehaviour
{
    private int trapNum;

    public void Start()
    {
        ShareTrap();
    }

    public void Update()
    {
        DeleteTrapTurn(); // 스테이지가 바뀌었을 때 스테이지에 있는 트랩 제거
    }

    public void ShareTrap() // 지금 트랩의 오브젝트 이름에 따라 변경
    {
        if (transform.name.Contains("AutoTrap"))
        {
            trapNum = 1;
        }
        else
        {
            Debug.LogError("AblityTrap 스크립트에서 trapNum을 분류하지 못했습니다");
        }
    }

    public void DeleteTrapTurn()
    {
        if (GameManager.Turn == 1)
        {
            Destroy(transform.gameObject);
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.transform.tag == "Enemy")
        {
            Enemy enemyPushTrap = collision.transform.GetComponent<Enemy>();
            // 밟은 적 행동력 감소
            enemyPushTrap.moveCtrl[1] -= 3;
            FindValues(transform.position).moveCtrl -= 3;
            collision.transform.GetComponent<Enemy>().AttackedEnemy(1); // 자동 덫에 밟은 적은 데미지 제공
        }
    }

    public GameObject FindValuesObj(Vector3 position)
    {
        GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
        foreach (Transform child in enemyBox.transform)
        {
            Debug.Log(child.position);
            if (child.position == position)
            {
                return child.gameObject;
            }
        }
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }

    public EnemyValues FindValues(Vector3 position)
    {
        foreach (EnemyValues child in GameManager.enemyValueList)
        {
            if (child.position == position)
            {
                return child;
            }
        }
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }
}
