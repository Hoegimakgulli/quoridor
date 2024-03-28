using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisposableButton : MonoBehaviour
{
    public enum ActiveCondition { None, Action, Attack } //능력 중 선제조건이 있는 능력이 있음. 그 선제 조건을 의미
    Player player;
    GameManager gameManager;
    Button button;
    Text text;
    public IActiveAbility activeAbility;

    private void Start()
    {
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        button = GetComponent<Button>();
        text = transform.GetChild(0).GetComponent<Text>();
    }
    void Update()
    {
        if ((activeAbility as IAbility).canEvent)
        {
            switch (activeAbility.activeCondition) //액티브 컨디션에 따라 버튼 활성화
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
        if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.Ability && text.text == player.usingAbilityID.ToString())
        {
            button.interactable = false;
        }
    }
    public void OnClick() //버튼을 1회용으로 만드는 함수
    {
        if ((activeAbility as IAbility).abilityType == PlayerAbility.EAbilityType.TargetActive)
        {
            gameManager.playerActionUI.PassiveUI();
            player.usingAbilityID = int.Parse(text.text); //플레이어한테 사용중인 능력을 알려줌.
            gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Ability; //플레이어 상태 변경
        }
        else (activeAbility as IAbility).Event();
    }
}
