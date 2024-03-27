using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class CharacterData
{
    public string name;
    public string story;
    public string move;
    public string attack;
    public string skill;

    public CharacterData(string name, string story, string move, string attack, string skill)
    {
        this.name = name;
        this.story = story;
        this.move = move;
        this.attack = attack;
        this.skill = skill;
    }
}

public class CharacterSelect : MonoBehaviour
{
    private Dictionary<int, CharacterData> charactors = new Dictionary<int, CharacterData>();

    private GameObject characterPanel;
    private RectTransform[] charactersUI = new RectTransform[5];
    public float uiMoveSpeed = 0.2f;
    public float uiWaitMoveTime = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        int count = 0;
        characterPanel = GameObject.Find("CharacterSelectPanel");
        foreach (Transform child in characterPanel.transform.GetChild(2).transform)
        {
            charactersUI[count] = child.GetComponent<RectTransform>();
            count++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            CharacterPanelContrl();
        }    
    }

    void CharacterDataSetting()
    {
        charactors.Add(0, new CharacterData("병사", "", "", "", ""));
        charactors.Add(1, new CharacterData("안젤리카", "", "", "", ""));
        charactors.Add(2, new CharacterData("아이작", "", "", "", ""));
        charactors.Add(3, new CharacterData("호아킨", "", "", "", ""));
    }

    void CharacterPanelContrl()
    {
        if (characterPanel.transform.GetChild(0).gameObject.activeSelf)
        {
            characterPanel.transform.GetChild(0).gameObject.SetActive(false);
            characterPanel.transform.GetChild(1).gameObject.SetActive(false);
        }
        else
        {
            characterPanel.transform.GetChild(0).gameObject.SetActive(true);
            characterPanel.transform.GetChild(1).gameObject.SetActive(true);
        }

        StartCoroutine(CharacterSelectStart(0));
    }

    IEnumerator CharacterSelectStart(int count)
    {
        charactersUI[count].DOAnchorPosY(-charactersUI[count].anchoredPosition.y, uiMoveSpeed);
        yield return new WaitForSeconds(uiWaitMoveTime);
        if(count < 4)
        {
            StartCoroutine(CharacterSelectStart(++count));
        }
    }
}
