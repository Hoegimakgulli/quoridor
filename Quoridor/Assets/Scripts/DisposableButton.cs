using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisposableButton : MonoBehaviour
{
    public enum ActiveCondition { None, Action, Attack }
    Player player;
    GameManager gameManager;
    Button button;
    Text text;
    public ActiveCondition activeCondition = ActiveCondition.None;
    public bool isAlreadyUsed = false;
    bool isTargetAbility = false;

    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        button = GetComponent<Button>();
        text = transform.GetChild(0).GetComponent<Text>();
    }
    void Update()
    {
        if (!isAlreadyUsed)
        {
            switch (activeCondition)
            {
                case ActiveCondition.Action:
                    button.interactable = player.canAction;
                    break;
                case ActiveCondition.Attack:
                    button.interactable = player.canAttack;
                    break;
                default:
                    button.interactable = true;
                    break;
            }
        }
        if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.Ability)
        {
            isTargetAbility = true;
        }
        else if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.None && isTargetAbility && text.text == player.usingAbilityID.ToString())
        {
            isAlreadyUsed = false;
            isTargetAbility = false;
        }
    }
    private void OnDisable()
    {
        isAlreadyUsed = false; ;
    }
    public void OnClick()
    {
        button.interactable = false;
        isAlreadyUsed = true;
        // Debug.Log("비활성화됨");
        gameManager.playerActionUI.PassiveUI();
    }
}
