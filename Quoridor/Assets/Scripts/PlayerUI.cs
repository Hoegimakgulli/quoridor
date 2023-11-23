using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    GameManager gameManager;
    Player player;
    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }
    public void OnMoveClick()
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Move;
        player.ResetPreview();
    }
    public void OnBuildClick()
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Build;
        player.ResetPreview();
    }
    public void OnAttackClick()
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Attack;
        player.ResetPreview();
    }
    public void OnTurnClick()
    {
        GameManager.Turn++;
    }
}
