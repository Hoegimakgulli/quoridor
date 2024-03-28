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
        CharacterDataSetting();
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
        charactors.Add(0, 
            new CharacterData
            ("병사",

            "충성!",
            
            "기본이동",
            
            "기본공격",

            "보호막 : 적 기물의 공격에 피격되었을 때 이를 무효로 하고 후방 2칸으로 이동한다. 해당 능력은 1회만 발동한다."));

        charactors.Add(1, 
            new CharacterData
            ("안젤리카",
            
            "국의 최대 화기 생산사인 AG 주식회사는 안젤리카의 빛나는 선구안으로 세워졌다. " +
            "원하는 것은 손에 쥐고 마는 안젤리카는 숙원 사업을 이룰 기회를 포착하고 제153대에 합류했다. " +
            "어디서든 눈에 띄는 유명 인사지만 사업 밖의 이야기는 언제나 비밀에 부친다.",
            
            "후방이동특화",
            
            "원거리공격특화",

            "재장전 : 공격으로 적 기물을 처치했을 경우 최대 1회 더 공격할 수 있다."));

        charactors.Add(2, 
            new CharacterData
            ("아이작",

            "아이작은 과거 제국의 남쪽 왕국에서 망명 온 장군의 외아들로, " +
            "현재는 양친이 별세하여 홀로 북쪽의 마물을 막고 있었다. " +
            "모국의 암살을 염려한 부친이 엄격하게 길러내어 제국 최강의 전투가 중 한 명으로 꼽힌다. " +
            "마도구의 발전으로 북쪽 경계에 여유가 생겨 새로 동원되었다.", 

            "전방이동특화",
            
            "근거리난투특화",

            "넉백 : 공격을 맞은 적 기물이 처치되지 않을 경우 플레이어 기물에게 멀어지는 방향으로 1칸 후퇴 이동" +
            "벽에 막히거나 맵 끝에서는 발동되지 않는다."));

        charactors.Add(3, 
            new CharacterData
            ("호아킨",

            " 깊은 명문가의 둘째 아들이자 차기 후작인 호아킨은 황립 사관학교를 졸업하자마자 곧장 전장에 합류했다. " +
            "바다 절벽 위 고택 안에서 부드러운 카펫만 밟고 성장했다. " +
            "어린 엘리트 기사로서 태어나서 처음으로 실전에 발을 내딛으려 한다.",

            "상하좌우 밸런스 특화",
            
            "공격 밸런스 특화",

            "회피 기동 : 공격 이후 상하좌우 1칸을 선택하여 이동할 수 있다."));
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
