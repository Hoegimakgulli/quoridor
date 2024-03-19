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
            switch (activeCondition) //액티브 컨디션에 따라 버튼 활성화
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
        if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.Ability) //조준이 필요한 능력들. (32번같은 이벤트. 바로 작동하는게 아니라 플레이어가 선택하는 시간을 줌.)
        {
            isTargetAbility = true; //플레이어가 타겟팅하는 능력을 사용함.
        }
        else if (gameManager.playerControlStatus == GameManager.EPlayerControlStatus.None && isTargetAbility && text.text == player.usingAbilityID.ToString()) //능력 사용 끝남 && 타겟팅 어빌리티임 && 플레이어가 사용한 능력이 내 능력이 맞는지(버튼이 여러개니깐)
        {
            isAlreadyUsed = false;
            isTargetAbility = false;
        }
    }
    private void OnDisable()
    {
        isAlreadyUsed = false;
    }
    public void OnClick() //버튼을 1회용으로 만드는 함수
    {
        button.interactable = false;
        isAlreadyUsed = true; //이미 사용했음으로 표시. (다음 턴이 왔을때도 활성화 안되게. playerAbility에서도 관리함)
        // Debug.Log("비활성화됨");
        PlayerAbility playerAbility = player.GetComponent<PlayerAbility>();
        if (isTargetAbility) gameManager.playerActionUI.PassiveUI();
    }
}
