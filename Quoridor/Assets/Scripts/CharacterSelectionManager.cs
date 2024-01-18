using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;



public class CharacterSelectionManager : MonoBehaviour
{

    public int charactersNum; //ĳ������ ����
    public int currentSelectedNum = 0;
    public List<Sprite> slotSprite = new List<Sprite>(); //ĳ���� ���� �̹��� ����Ʈ
    public List<GameObject> slotObject = new List<GameObject>(); //ĳ���� ���� ������Ʈ ����Ʈ
    public GameObject selectedCharacter; //���� ���õǾ� �ִ� ĳ����

    public List<TextMeshProUGUI> tempoTexts = new List<TextMeshProUGUI>(); //�ӽ�

    private List<CharacterSlot> slotCS = new List<CharacterSlot>();

    private void Start()
    {
        for(int i = 0; i < slotObject.Count; i++)
        {
            slotCS.Add(slotObject[i].GetComponent<CharacterSlot>());
        }
        SelectCharacter(currentSelectedNum);
    }

    //���õ� ĳ���Ͱ� ���°������ ���ڷ� �޾ƿ´�.
    public void SelectCharacter(int selectedNum)
    {
        if (!slotCS[selectedNum].isSelectable) //������ �Ұ����� ĳ���͸� ���������� �ҹ�
        {
            return;
        }
            
        currentSelectedNum = selectedNum;
        selectedCharacter = slotObject[currentSelectedNum];
        for(int i = 0; i < slotObject.Count; i++)
        {
            if (!slotCS[i].isSelectable) //���� �Ұ����� ��� ������ �˰�
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

        /* �ӽ� */
        tempoTexts[0].text = "Character\nImage " + (currentSelectedNum + 1);
        tempoTexts[1].text = "Moving\nImage " + (currentSelectedNum + 1);
        tempoTexts[2].text = "Attack\nImage " + (currentSelectedNum + 1);
        tempoTexts[3].text = "Skill Information  " + (currentSelectedNum + 1);
        /* �ӽ� */
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
