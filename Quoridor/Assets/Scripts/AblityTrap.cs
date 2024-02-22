using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AblityTrap : MonoBehaviour
{
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.transform.tag == "Enemy")
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
        //Debug.LogError("enemyManager error : 어떤 Enemy 스크립트를 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }

    public enemyValues FindValues(Vector3 position)
    {
        foreach (enemyValues child in GameManager.enemyValueList)
        {
            if (child.position == position)
            {
                return child;
            }
        }
        //Debug.LogError("enemyManager error : 어떤 EnemyValues도 찾지 못했습니다.");
        return null; // 위치에 아무런 오브젝트도 못찾았을 경우
    }
}
