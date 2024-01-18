using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StageUI : MonoBehaviour
{
    GameManager gameManager;
    EnemyStage enemyStage;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        enemyStage = GameObject.Find("GameManager").GetComponent<EnemyStage>();
    }

    public void ClearStage()
    {
        GameManager.enemyPositions.Clear();
        GameManager.enemyObjects.Clear();
        Debug.Log(GameManager.enemyPositions.Count);
        Debug.Log(GameManager.enemyObjects.Count);
        gameManager.currentStage++;
        gameManager.playerPosition = new Vector3(0, -4, 0);
        GameObject.FindWithTag("Player").transform.position = GameManager.gridSize * gameManager.playerPosition;
        enemyStage.StageEnemySpawn();
        GameManager.Turn = 1;
        EnemyStage.totalEnemyCount = enemyStage.stageEnemySettig[gameManager.currentStage].TotalReturn();

        Destroy(this.gameObject);
    }
}
