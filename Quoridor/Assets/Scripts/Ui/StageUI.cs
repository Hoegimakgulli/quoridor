using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageUI : MonoBehaviour
{
    GameManager gameManager;
    EnemyStage enemyStage;
    // AbilitySelect abilitySelect;
    Player player;
    // Start is called before the first frame update
    void Start()
    {
        GameObject gameManagerObject = GameManager.Instance.gameObject;
        gameManager = gameManagerObject.GetComponent<GameManager>();
        enemyStage = gameManagerObject.GetComponent<EnemyStage>();
        // abilitySelect = gameManagerObject.GetComponent<AbilitySelect>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }

    public void ClearStage()
    {
        if (!gameManager.gameObject.GetComponent<UiManager>().freezeButton)
        {
            StageManager.currentStage++;
            SceneManager.LoadScene(1);
            return;
            // Lecacy code
            /*
            gameManager.currentStage++;
            if (gameManager.currentStage > 10)
            {
                Application.Quit();
                return;
            }
            gameManager.Initialize();

            enemyStage.EnemyInitialize();
            player.Initialize();

            abilitySelect.AbilitySelectStart();

            Destroy(this.gameObject);
            */
        }
    }
}
