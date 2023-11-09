using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class CharacterSelectionManager : MonoBehaviour
{

    public int charactersNum; //캐릭터의 갯수
    public int currentSelectedNum = 0;
    public List<Sprite> slotSprite = new List<Sprite>(); //캐릭터 슬롯 이미지 리스트
    public List<GameObject> slotObject = new List<GameObject>(); //캐릭터 슬롯 오브젝트 리스트
    public GameObject selectedCharacter; //현재 선택되어 있는 캐릭터

    public List<TextMeshProUGUI> tempoTexts = new List<TextMeshProUGUI>(); //임시

    private List<CharacterSlot> slotCS = new List<CharacterSlot>();

    private void Start()
    {
        for(int i = 0; i < slotObject.Count; i++)
        {
            slotCS.Add(slotObject[i].GetComponent<CharacterSlot>());
        }
        SelectCharacter(currentSelectedNum);
    }

    //선택된 캐릭터가 몇번째인지를 인자로 받아온다.
    public void SelectCharacter(int selectedNum)
    {
        if (!slotCS[selectedNum].isSelectable) //선택이 불가능한 캐릭터를 선택했으면 불발
        {
            return;
        }
            
        currentSelectedNum = selectedNum;
        selectedCharacter = slotObject[currentSelectedNum];
        for(int i = 0; i < slotObject.Count; i++)
        {
            if (!slotCS[i].isSelectable) //선택 불가능한 경우 완전히 검게
            {
                slotObject[i].GetComponent<Image>().color = new Color(0, 0, 0);
            }
            else if (slotObject[i] == slotObject[currentSelectedNum])
            {
                slotObject[i].GetComponent<Image>().color = new Color(1, 1, 1);
            }
            else
            {
                slotObject[i].GetComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f);
            }
        }

        /* 임시 */
        tempoTexts[0].text = "Character\nImage " + (currentSelectedNum + 1);
        tempoTexts[1].text = "Moving\nImage " + (currentSelectedNum + 1);
        tempoTexts[2].text = "Attack\nImage " + (currentSelectedNum + 1);
        tempoTexts[3].text = "Skill Information  " + (currentSelectedNum + 1);
        /* 임시 */
    }

    public void LeftArrow()
    {
        do {
            currentSelectedNum--;
            if (currentSelectedNum < 0)
            {
                currentSelectedNum = charactersNum - 1;
            }
        } while (!slotCS[currentSelectedNum].isSelectable);
        SelectCharacter(currentSelectedNum);
    }
    public void RightArrow()
    {
        do
        {
            currentSelectedNum++;
            if (currentSelectedNum > charactersNum - 1)
            {
                currentSelectedNum = 0;
            }
        } while (!slotCS[currentSelectedNum].isSelectable);
        SelectCharacter(currentSelectedNum);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("SampleScene");
    }
}
