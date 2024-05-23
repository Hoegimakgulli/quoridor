using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;
using Unity.VisualScripting;
using System.Runtime.InteropServices.WindowsRuntime;

public class AbilityDescription
{
    public string skillName; // 능력 이름
    public string skillDescription; // 능력 설명
    public int rank; // 능력 등급
    public bool isGet; // 획득 여부
    public bool canShow; // 선지 과정때 나와도 되는 능력인지 판단
    public List<int> needAbilityPart = new List<int>(); // 출현 필요 조건 능력
    public List<int> needAbilityMain = new List<int>(); // 무조건 나와야하는 능력

    // 스킬 이름, 스킬 설명, 등급 순으로 나열
    public AbilityDescription(string skillName, string skillDescription, int rank)
    {
        this.skillName = skillName;
        this.skillDescription = skillDescription;
        this.rank = rank;
        isGet = false;
        canShow = true;
    }
}

public class AbilitySlot
{
    public float[] firstSlot = new float[4];
    public float[] secondSlot = new float[4];
    public float[] thirdSlot = new float[4];

    // 생성자 (Low, Middle, High, Special) 4개 순서대로 진행되는데 first, second, third로 나누어 저장
    public AbilitySlot(float FF, float FS, float FT, float FP, float SF, float SS, float ST, float SP, float TF, float TS, float TT, float TP)
    {
        firstSlot[0] = FF;
        firstSlot[1] = FS;
        firstSlot[2] = FT;
        firstSlot[3] = FP;

        secondSlot[0] = SF;
        secondSlot[1] = SS;
        secondSlot[2] = ST;
        secondSlot[3] = SP;

        thirdSlot[0] = TF;
        thirdSlot[1] = TS;
        thirdSlot[2] = TT;
        thirdSlot[3] = TP;
    }
}

public class AbilitySelect : MonoBehaviour
{
    // key = 능력 번호, value = 능력 선택창에 필요한 인자
    public Dictionary<int, AbilityDescription> skills = new Dictionary<int, AbilityDescription>();
    public List<Dictionary<string, object>> Skills = new List<Dictionary<string, object>>();

    // key = 스테이지 번호, value = 각 스테이지에 해당하는 슬롯의 확률
    public Dictionary<int, AbilitySlot> slots = new Dictionary<int, AbilitySlot>();

    // 대사 딕셔너리
    public List<Dictionary<string, object>> talks = new List<Dictionary<string, object>>();

    // 등급별로 능력 분류
    private List<int> lowSkills = new List<int>();
    private List<int> middleSkills = new List<int>();
    private List<int> highSkills = new List<int>();
    private List<int> specialSkills = new List<int>();

    // AbilitySelectPanel안에 들어 있는 오브젝트들 현 비활성화 되어있음
    public GameObject[] slotUi = new GameObject[3];
    public GameObject selectButton;
    public Button SelectUiButton;
    public float popDuring = 1f;

    public PlayerAbility playerAbility;
    public GameManager gameManager;

    public void Start()
    {
        playerAbility = GameObject.FindWithTag("Player").GetComponent<PlayerAbility>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        Skills = CSVReader.Read("AbilityCSV");
        int count = 0;
        GameObject AS = GameObject.Find("AbilitySelectPanel");
        SelectUiButton = AS.transform.GetChild(2).GetChild(0).GetComponent<Button>();
        foreach (Transform slotItem in AS.transform.GetChild(0).transform)
        {
            slotUi[count] = slotItem.gameObject;
            count++;
        }

        Initialize(); // 게임을 시작하고 초기에만 실행
    }

    public void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
        {
            AbilitySelectStart();
        }
    }

    public void Initialize()
    {
        AbilityStart(); // skills 딕셔너리에 값 대입
        SlotStart();    // slots 딕셔너리에 값 대입
        TalkStart();    // talks 딕셔너리에 값 대입
        ButtonSetting();
        AlreadyHaveAbility();
        ShareAbility();
        CanShowAbilityShare();
        CharacterTalkStart();
    }

    private void AlreadyHaveAbility()
    {
        // 기존에 가지고 있는 능력들 확인 후 딕셔너리에 체크
        foreach (int IdNum in playerAbility.startAbilities)
        {
            if (skills.ContainsKey(IdNum))
            {
                skills[IdNum].isGet = true;
            }

            else
            {
                Debug.LogError("AbilitySelect : 등록된 능력이 없습니다. 다시 확인해주세요");
            }
        }
    }

    public void AbilityStart() // 초기 게임 실행 1번만 실행.
    {
        // 각각 스킬 이름, 설명, 등급 순서대로 입력
        for(int skillCount = 1; skillCount <= Skills.Count; ++skillCount)
        {
            string rank = Skills[skillCount - 1]["등장 확률 및 가치"].ToString();
            int rankNum = 0;
            switch (rank)
            {
                case "저":
                    rankNum = 0;
                    break;
                case "중":
                    rankNum = 1;
                    break;
                case "고":
                    rankNum = 2;
                    break;
                case "특수":
                    rankNum = 3;
                    break;
                default:
                    Debug.LogError("AbilitySelect.cs : 존재하지 않는 등급의 능력입니다.");
                    break;
            }
            skills.Add(skillCount, new AbilityDescription(
                Skills[skillCount - 1]["능력 이름"].ToString(),
                Skills[skillCount - 1]["인게임 텍스트"].ToString(),
                rankNum));
        }
    }

    public void SlotStart()
    {
        // 스테이지마다 슬롯에 가지고 있는 확률 대입 4, 4, 4개로 쪼개서 들어감
        slots.Add(1, new AbilitySlot(90, 8, 2, 0, 90, 8, 2, 0, 90, 8, 2, 0));
        slots.Add(2, new AbilitySlot(90, 8, 2, 0, 36, 62, 2, 0, 50, 48, 2, 0));
        slots.Add(3, new AbilitySlot(1, 2, 27, 70, 42, 50, 8, 0, 42, 50, 8, 0));
        slots.Add(4, new AbilitySlot(20, 30, 50, 0, 50, 20, 30, 0, 50, 30, 20, 0));
        slots.Add(5, new AbilitySlot(0.2f, 1.8f, 18, 80, 10, 80, 10, 0, 50, 40, 10, 0));
        slots.Add(6, new AbilitySlot(10, 30, 60, 0, 50, 46, 4, 0, 60, 36, 4, 0));
        slots.Add(7, new AbilitySlot(0.2f, 1.8f, 8, 90, 2, 68, 30, 0, 50, 40, 10, 0));
        slots.Add(8, new AbilitySlot(30, 40, 30, 0, 45, 35, 20, 0, 45, 35, 20, 0));
        slots.Add(9, new AbilitySlot(2, 18, 80, 0, 2, 80, 18, 0, 2, 80, 18, 0));
    }

    public void ButtonSetting()
    {
        for (int i = 0; i < 3; i++)
        {
            int iii = i;
            slotUi[i].GetComponent<Button>().onClick.AddListener(SelectSkillHighlighting);
        }

        SelectUiButton.onClick.AddListener(SelectButtonTrigger);
    }

    public void TalkStart()
    {
        switch (GameObject.FindWithTag("Player").name)
        {
            case "Angelique(Clone)":
                talks = CSVReader.Read("Angelique");
                break;
            case "Chloe(Clone)":
                talks = CSVReader.Read("Chloe");
                break;
            case "Isaac(Clone)":
                talks = CSVReader.Read("Isaac");
                break;
            case "Joaquin(Clone)":
                talks = CSVReader.Read("Joaquin");
                break;
            case "Kaawa(Clone)":
                talks = CSVReader.Read("Kaawa");
                break;
            case "Lucas(Clone)":
                talks = CSVReader.Read("Lucas");
                break;
            case "Soldier(Clone)":
                talks = CSVReader.Read("Soldier");
                break;
            default:
                Debug.LogError("등록되지않은 캐릭터 이름입니다");
                break;
        }

        GameObject.Find("PCTalk").GetComponent<TMP_Text>().text = talks[Random.Range((gameManager.currentStage * 3) - 3, (gameManager.currentStage * 3))]["Talk"].ToString();
    }

    public void ShareAbility() // skills 딕셔너리에 들어있는 아이템들을 등급에 맞게 리스트에 넣어줌 이후 랜덤으로 돌려서 나오도록 조정.
    {
        foreach (KeyValuePair<int, AbilityDescription> item in skills)
        {
            switch (item.Value.rank)
            {
                case 0: // low
                    lowSkills.Add(item.Key);
                    break;
                case 1: // middle
                    middleSkills.Add(item.Key);
                    break;
                case 2: // high
                    highSkills.Add(item.Key);
                    break;
                case 3: // special
                    specialSkills.Add(item.Key);
                    break;
                default: // 분류 불가
                    Debug.LogError("AbilitySelect Scripts : 능력을 분류하는 과정에서 오류가 생겼습니다. (등록되지않은 능력 등급)");
                    break;
            }
        }
    }

    public void CharacterTalkStart()
    {
        // 지금 플레이하고 있는 캐릭터 이미지를 따온 후 적용
        GameObject playerChar = GameObject.FindWithTag("Player");
        Image playerUiImage = GameObject.Find("PCImage").GetComponent<Image>();

        playerUiImage.sprite = playerChar.transform.GetComponent<SpriteRenderer>().sprite;
        playerUiImage.color = playerChar.transform.GetComponent<SpriteRenderer>().color;
    }

    private void CanShowAbilityCheck() // 능력 중 의존 능력들의 필요 조건이 충족됐는지 확인하는 함수
    {
        foreach (KeyValuePair<int, AbilityDescription> item in skills)
        {
            bool isMain = true; // needAbilityMain 리스트관리 변수
            bool isPart = false; // needAbilityPart 리스트관리 변수
            if (item.Value.needAbilityMain.Count == 0 && item.Value.needAbilityPart.Count == 0)
            {
                continue;
            }
            else
            {
                foreach (int skillNum in item.Value.needAbilityMain)
                {
                    if (!playerAbility.abilitiesID.Contains(skillNum))
                    {
                        isMain = false;
                        break;
                    }
                }
                if (isMain)
                {
                    foreach (int skillNum in item.Value.needAbilityPart)
                    {
                        if (playerAbility.abilitiesID.Contains(skillNum))
                        {
                            isPart = true;
                            break;
                        }
                    }
                }

                if (isPart && isPart)
                {
                    item.Value.canShow = true;
                }
                else
                {
                    item.Value.canShow = false;
                }
            }
        }
    }

    private void CanShowAbilityShare() // 각 능력중 필요한 능력들을 
    {
        foreach (KeyValuePair<int, AbilityDescription> item in skills)
        {
            switch (item.Key)
            {
                // Active 능력 패시브들 모음
                case 16:
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);
                    break;
                case 17:
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);

                    item.Value.needAbilityMain.Add(16);
                    break;
                case 18:
                    item.Value.needAbilityPart.Add(13);
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);
                    break;
                case 19:
                    item.Value.needAbilityPart.Add(13);
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);

                    item.Value.needAbilityMain.Add(18);
                    break;
                case 20:
                    item.Value.needAbilityPart.Add(13);
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);
                    break;
                case 21:
                    item.Value.needAbilityPart.Add(13);
                    item.Value.needAbilityPart.Add(14);
                    item.Value.needAbilityPart.Add(15);
                    break;
                // 설치 능력 패시브 모음
                case 25:
                    item.Value.needAbilityPart.Add(22);
                    item.Value.needAbilityPart.Add(23);
                    item.Value.needAbilityPart.Add(24);
                    break;
                case 26:
                    item.Value.needAbilityPart.Add(22);
                    item.Value.needAbilityPart.Add(23);
                    item.Value.needAbilityPart.Add(24);

                    item.Value.needAbilityMain.Add(25);
                    break;
                case 27:
                    item.Value.needAbilityPart.Add(23);
                    item.Value.needAbilityPart.Add(24);
                    item.Value.needAbilityPart.Add(29);
                    break;
                case 28:
                    item.Value.needAbilityPart.Add(23);
                    item.Value.needAbilityPart.Add(24);
                    item.Value.needAbilityPart.Add(29);

                    item.Value.needAbilityMain.Add(27);
                    break;

                default:
                    break;
            }
        }
    }

    private List<int> tmpSkillNumBox = new List<int>(); // 임시로 뽑힌 능력이 어떤 번호인지 담아두는 변수

    public void AbilitySelectStart()
    {
        CanShowAbilityCheck();
        AbilitySlot currentSlot = null;
        if (slots.ContainsKey(gameManager.currentStage))
        {
            currentSlot = slots[gameManager.currentStage];
        }
        else
        {
            Debug.LogError("AbilitySelect Scripts AbilitySelectStart 함수 : 해당 스테이지에 맞는 slots를 찾지 못했습니다.");
        }

        for (int slotCount = 0; slotCount < 3; slotCount++) // 첫번째 슬롯 두번째 슬롯 세번째 슬롯까지 총 3개 뽑는 과정 실시
        {
            float select = Random.Range(0.0f, 100.0f); // 확률계산
            float sum = 0; // 확률 계산에 필요한 sum 변수
            int count = 0; // 저 중 고 특수중 어떤 등급을 뽑아야하는지 결정하는 float함수

            switch (slotCount) // 각 슬롯에 해당하는 저 중 고 특성 순서대로 비교 연산 
            {
                case 0:
                    foreach (float num in currentSlot.firstSlot)
                    {
                        sum += num;
                        if (select <= sum)
                        {
                            break;
                        }
                        count++; // ++ 될때 마다 등급 상승
                    }
                    break;
                case 1:
                    foreach (float num in currentSlot.secondSlot)
                    {
                        sum += num;
                        if (select <= sum)
                        {
                            break;
                        }
                        count++;
                    }
                    break;
                case 2:
                    foreach (float num in currentSlot.thirdSlot)
                    {
                        sum += num;
                        if (select <= sum)
                        {
                            break;
                        }
                        count++;
                    }
                    break;
            }

            int skillSelect = 0;
            switch (count) //각 등급에 맞는 능력중에서 리스트로 뽑아 이전에 나온 능력인지 확인 최종 스킬 번호 추출
            {
                case 0: // 이미 뽑힌 스킬이 아닐때까지 계속 뽑음
                    do
                    {
                        skillSelect = lowSkills[Random.Range(0, lowSkills.Count)];
                    } while (skills[skillSelect].isGet || !skills[skillSelect].canShow || tmpSkillNumBox.Contains(skillSelect));
                    break;
                case 1:
                    do
                    {
                        skillSelect = middleSkills[Random.Range(0, middleSkills.Count)];
                    } while (skills[skillSelect].isGet || !skills[skillSelect].canShow || tmpSkillNumBox.Contains(skillSelect));
                    break;
                case 2:
                    do
                    {
                        skillSelect = highSkills[Random.Range(0, highSkills.Count)];
                    } while (skills[skillSelect].isGet || !skills[skillSelect].canShow || tmpSkillNumBox.Contains(skillSelect));
                    break;
                case 3:
                    do
                    {
                        skillSelect = specialSkills[Random.Range(0, specialSkills.Count)];
                    } while (skills[skillSelect].isGet || !skills[skillSelect].canShow || tmpSkillNumBox.Contains(skillSelect));
                    break;
            }

            tmpSkillNumBox.Add(skillSelect);
            /*
             * 스킬 선택창을 열고 중복되는 슬롯에 중복되는 능려이 안뜨게 작성할것
             */

            slotUi[slotCount].transform.GetChild(1).GetComponent<TMP_Text>().text = skills[skillSelect].skillName; // 스킬 이름 넣어주는 파트
            slotUi[slotCount].transform.GetChild(2).GetComponent<TMP_Text>().text = skills[skillSelect].skillDescription; // 스킬 설명 넣어주는 파트
        }

        // 정보 저장 이후 능력 선택창이 생성되는 부분

        StartCoroutine(SelectSkillUiPop());
    }

    public void SelectButtonTrigger() // Select 버튼을 클릭했을 때
    {
        int itemCount = 0;

        if (!selectButton)
        {
            Debug.LogError("원하는 능력을 골라주세요");
        }

        else
        {
            foreach (GameObject Item in slotUi)
            {
                if (selectButton == Item)
                {
                    break;
                }

                itemCount++;
            }

            if (itemCount == 3)
            {
                Debug.Log("어떤 스킬도 선택하지 않았습니다");
            }
            else
            {
                // 능력 선택한 이후 애니메이션 추가 예정
                SelectSkillUiDePop();
                Debug.Log(tmpSkillNumBox[itemCount]);
                int tmpNum = tmpSkillNumBox[itemCount];
                skills[tmpSkillNumBox[itemCount]].isGet = true; // 얻은 스킬은 얻었다는 표시를 남겨줌
                tmpSkillNumBox.Clear(); // 담아뒀던 임시 능력 번호는 초기화
                playerAbility.AddAbility(tmpNum); // 선택한 능력 추가
            }
        }
    }

    public void SelectSkillHighlighting() // 어떤 능력을 고를지 선택
    {
        GameObject.Find("GameManager").GetComponent<AbilitySelect>().selectButton = EventSystem.current.currentSelectedGameObject;
    }

    IEnumerator SelectSkillUiPop() // 능력 선택 창이 내려오는 함수
    {
        foreach (GameObject uiItem in slotUi)
        {
            uiItem.GetComponent<RectTransform>().DOAnchorPosY(0, popDuring).SetEase(Ease.OutCirc);
            yield return new WaitForSeconds(popDuring);
        }
        GameObject.Find("PlayerChaBox").GetComponent<RectTransform>().DOAnchorPosX(0, popDuring).SetEase(Ease.OutCirc);
        yield return new WaitForSeconds(popDuring);

        GameObject.Find("SelectBox").GetComponent<RectTransform>().DOAnchorPosY(0, popDuring).SetEase(Ease.OutCirc);
    }

    public void SelectSkillUiDePop() // 능력 선택 창이 올라가는 함수
    {
        float popDuring = GameObject.Find("GameManager").GetComponent<AbilitySelect>().popDuring;
        foreach (GameObject uiItem in GameObject.Find("GameManager").GetComponent<AbilitySelect>().slotUi)
        {
            uiItem.GetComponent<RectTransform>().DOAnchorPosY(950, popDuring).SetEase(Ease.OutCirc);
        }
        GameObject.Find("PlayerChaBox").GetComponent<RectTransform>().DOAnchorPosX(-1500, popDuring).SetEase(Ease.OutCirc);

        GameObject.Find("SelectBox").GetComponent<RectTransform>().DOAnchorPosY(-350, popDuring).SetEase(Ease.OutCirc);
    }
}
