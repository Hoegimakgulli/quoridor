using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class StageUI : MonoBehaviour
{
    GameManager gameManager;
    EnemyStage enemyStage;
    Player player;
    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        enemyStage = GameObject.Find("GameManager").GetComponent<EnemyStage>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    public void ClearStage()
    {
        if (!gameManager.gameObject.GetComponent<UiManager>().freezeButton)
        {
            gameManager.currentStage++;

            gameManager.Initialize();
            player.Initialize();
            enemyStage.EnemyInitialied();

            Destroy(this.gameObject);
        }
    }
}
