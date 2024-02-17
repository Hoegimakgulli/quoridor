using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisposableButton : MonoBehaviour
{
    public enum ActiveCondition { None, Action, Attack }
    Player player;
    Button button;
    public ActiveCondition activeCondition = ActiveCondition.None;
    bool isAlreadyUsed = false;
    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        button = GetComponent<Button>();
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
                    break;
            }
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
    }
}
