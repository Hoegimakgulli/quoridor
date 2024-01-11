using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUI : MonoBehaviour
{
    GameManager gameManager;
    Player player;

    //건설 완료버튼과 팝업 창들은 인스펙터에서 연결해주세요.
    [SerializeField]
    GameObject buttonComplete;
    [SerializeField]
    GameObject warningPopUp;
    [SerializeField]
    GameObject abilityInfoPopUp;

    bool isAbilityClick = false;
    float timer = 0f;

    // 되도록이면 UI Canvas 안에 자식 순서가 Attack/Build/Move/Ability/EndTurn/WallCount/나머지 순이 되게 해주세요.
    // 그렇지 않으면 오류가 날것입니다. 순서를 바꾸시다면 player.cs -> Update()에 있는 [디버그용] 파트를 수정해주시면 됩니다.
    // 예시는 playerUI prefab을 확인하시면 될것 같습니다. UI 제작후 확인하려면 player prefab 인스펙터에서 playerUI 연결해주시면 됩니다!
    // 추가적으로 궁금하시거나 오류가 있다면 카톡으로 문의해주세요!

    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        player = GameObject.FindWithTag("Player").GetComponent<Player>();
    }
    void Update()
    {
        if (isAbilityClick)
        {
            timer += Time.deltaTime;
        }
        else timer = 0;
    }
    public void OnMoveClick() // 이동 버튼 클릭하였을 때
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Move;
        player.ResetPreview();
        buttonComplete.SetActive(false);
    }
    public void OnBuildClick() // 건설 버튼 클릭하였을 때
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Build;
        player.ResetPreview();
        buttonComplete.GetComponent<Button>().interactable = false;
        buttonComplete.SetActive(true);
    }
    public void OnAttackClick() // 공격 버튼 클릭하였을 때
    {
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Attack;
        player.ResetPreview();
        buttonComplete.SetActive(false);
    }
    // 능력 버튼 제작시 Event Trigger 라는 컴포넌트 추가해 PointerDown과 PointerUp에서 이벤트 실행되게 해주세요
    public void OnAbilityPointerDown() // 능력 버튼 누르면
    {
        isAbilityClick = true;
    }
    public void OnAbilityPointerUp() // 능력 버튼 때면
    {
        if (timer >= 0.5f)
        {
            abilityInfoPopUp.SetActive(true);
        }
        else
        {
            gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Ability;
            player.ResetPreview();
            buttonComplete.SetActive(false);
        }
        isAbilityClick = false;
    }
    public void OnTurnClick() // 턴 종료 버튼 클릭하였을 때
    {
        if (player.canAction) warningPopUp.SetActive(true);
        else
        {
            EnemyManager.turnCheck = false;
            GameManager.Turn++;
        }
    }
    public void OnTurnClickInPopUp() // 턴 진행 버튼 클릭하였을 때 (팝업내에서)
    {
        EnemyManager.turnCheck = false;
        GameManager.Turn++;
        warningPopUp.SetActive(false);
    }
    public void OnCancelClickInPopUp() // 팝업창 닫기
    {
        if (warningPopUp.activeInHierarchy) warningPopUp.SetActive(false);
        else if (abilityInfoPopUp.activeInHierarchy) abilityInfoPopUp.SetActive(false);
    }
    public void OnCompleteBuildClick() // 건설 완료 버튼
    {
        if (player.BuildComplete()) buttonComplete.SetActive(false);
    }
}
