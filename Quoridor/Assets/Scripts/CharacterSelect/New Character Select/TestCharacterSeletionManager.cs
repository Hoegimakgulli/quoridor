using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TestCharacterSeletionManager : MonoBehaviour
{
    public GameObject characterSlotPrefab;
    public GameObject selectSlotPrefab;
    private Transform infoPanel;
    private GameObject selectSlotLayoutGroup;
    private GameObject characterSlotLayoutGroup;
    private bool[] isUnlockCharacter;
    private string[] characterName;
    private string[] characterStory;
    private string[] characterSkill;
    private int selectedCharacter; //캐릭터 몇명 선택했는지
    private int currentCharacterNum; //현재 선택한 캐릭터의 번호

    private List<int> partyList = new List<int>();

    private void Awake()
    {
        //데이터 불러오기
        DataManager.DM.LoadGameData();
        infoPanel = transform.GetChild(3); //Hierarchy 순서에 따라 바꿔줄 것
        infoPanel.gameObject.SetActive(false);
        characterSlotLayoutGroup = transform.GetChild(1).gameObject; //Hierarchy 순서에 따라 바꿔줄 것
        selectSlotLayoutGroup = transform.GetChild(2).GetChild(0).gameObject; //Hierarchy 순서에 따라 바꿔줄 것

        //캐릭터 언락 정보, 캐릭터 이름, 캐릭터 스토리, 캐릭터 스킬 텍스트 받아오기
        isUnlockCharacter = DataManager.DM.GetUnlockCharacter();
        characterName = DataManager.DM.GetCharacterName();
        characterStory = DataManager.DM.GetCharacterStory();
        characterSkill = DataManager.DM.GetCharacterSkill();
        partyList = DataManager.DM.GetPartyList();
        Invoke("ChangeSelectSlot", 0.1f);
        Invoke("ChangeCharacterSlot", 0.1f);
        selectedCharacter = partyList.Count;

        //캐릭터 슬롯 생성
        for(int i = 0; i < isUnlockCharacter.Length; i++)
        {
            GameObject slot = Instantiate(characterSlotPrefab);
            slot.transform.SetParent(characterSlotLayoutGroup.transform);
            slot.GetComponent<RectTransform>().localScale = Vector3.one;
            slot.transform.GetChild(1).GetChild(0).GetComponent<Text>().text = characterName[i];
            int j = i;
            slot.GetComponent<Button>().onClick.AddListener(() => ActiveCharacterInfo(j));
        }

        //선택 슬롯 생성
        RectTransform groupRT = selectSlotLayoutGroup.GetComponent<RectTransform>();
        groupRT.sizeDelta = new Vector2(80*DataManager.DM.GetSelectCharacterNum(), 80);
        for(int i = 0; i < DataManager.DM.GetSelectCharacterNum(); i++)
        {
            GameObject slot = Instantiate(selectSlotPrefab);
            slot.transform.SetParent(selectSlotLayoutGroup.transform);
            slot.GetComponent<RectTransform>().localScale = Vector3.one;
        }
    }

    //캐릭터 정보창 활성화 (선택창)
    void ActiveCharacterInfo(int characterNum)
    {
        infoPanel.gameObject.SetActive(true);
        infoPanel.GetChild(0).GetChild(0).GetComponent<Text>().text = characterName[characterNum];
        infoPanel.GetChild(1).GetChild(1).GetChild(0).GetComponent<Text>().text = characterStory[characterNum];
        infoPanel.GetChild(4).GetChild(0).GetComponent<Text>().text = characterSkill[characterNum];
        infoPanel.GetChild(6).GetChild(0).gameObject.SetActive(true); //걍 대충
        infoPanel.GetChild(6).GetChild(1).gameObject.SetActive(false); //걍 대충
        foreach(int num in partyList)
        {
            if(num == characterNum)
            {
                infoPanel.GetChild(6).GetChild(1).gameObject.SetActive(true); //걍 대충
                infoPanel.GetChild(6).GetChild(0).gameObject.SetActive(false); //걍 대충
                break;
            }
        }
        currentCharacterNum = characterNum;
    }

    //캐릭터 정보창 닫음
    public void CloseCharacterInfo()
    {
        infoPanel.gameObject.SetActive(false);
    }

    //캐릭터 파티 편성/추방 시, 선택 슬롯에 선택한 캐릭터 표시 및 캐릭터 슬롯 색 변화
    public void InputSelectSlot()
    {
        foreach (int num in partyList)
        {
            if (num == currentCharacterNum)
            {
                partyList.Remove(num);
                CloseCharacterInfo();
                ChangeColorCharacterSlot(num, new Color(1f, 1f, 1f));
                selectedCharacter--;
                ChangeSelectSlot();
                return;
            }
        }
        if (selectedCharacter < DataManager.DM.GetSelectCharacterNum())
        {
            partyList.Add(currentCharacterNum);
            CloseCharacterInfo();
            ChangeColorCharacterSlot(currentCharacterNum, new Color(.3f, .3f, .3f));
            ChangeSelectSlot();
            selectedCharacter++;
        }
    }

    //캐릭터 슬롯 활성화 비활성화 상태 표시
    private void ChangeCharacterSlot()
    {
        foreach(int num in partyList)
        {
            ChangeColorCharacterSlot(num, new Color(.3f, .3f, .3f));
        }
    }

    //partyList에 맞춰 선택 슬롯 표기를 변경
    private void ChangeSelectSlot()
    {
        for(int i = 0; i < DataManager.DM.GetSelectCharacterNum(); i++)
        {
            if (i < partyList.Count)
            {
                selectSlotLayoutGroup.transform.GetChild(i).GetChild(0).GetComponent<Text>().text = characterName[partyList[i]];
            }
            else
            {
                selectSlotLayoutGroup.transform.GetChild(i).GetChild(0).GetComponent<Text>().text = "";
            }
        }
    }

    //(임시)캐릭터 슬롯의 색을 바꾸는 함수
    private void ChangeColorCharacterSlot(int characterNum, Color inputColor)
    {
        characterSlotLayoutGroup.transform.GetChild(characterNum).GetChild(0).GetComponent<Image>().color = inputColor;
        characterSlotLayoutGroup.transform.GetChild(characterNum).GetChild(1).GetComponent<Image>().color = inputColor;
    }

    //(임시) 게임 스타트
    public void StartGame()
    {
        DataManager.DM.SetPartyList(partyList);
        SceneManager.LoadScene("StageSelect");
    }
}
