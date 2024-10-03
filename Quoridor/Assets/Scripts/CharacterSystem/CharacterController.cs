using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HM.Containers;
using CharacterDefinition;
using HM.Utils;

public class CharacterController : MonoBehaviour
{
    /// <summary>
    /// 기물 DT 기반으로한 기물 프레임이라고 생각.
    /// 실제 문서 DT == stateDatas
    /// ~Fields는 stateDatas에서 playable 변수 기준으로 player가 다루는 기물의 데이터인지 enemy 기물의 데이터인지 분류
    /// </summary>
    public List<Dictionary<string, object>> stateDatas                   = new List<Dictionary<string, object>>();
    public List<BaseCharacter> playerCharacterFields                     = new List<BaseCharacter>();
    public List<BaseCharacter> enemyCharacterFields                      = new List<BaseCharacter>();

    /// <summary>
    /// 실제로 해당 타일에 생성된 기물들의 "BaseCharacter" 정보라고 생각.
    /// 해당 DIC에서 불러와 스크립트 상에서 유동적으로 사용할 예정 -> 데이터 사용은 해당 변수로만 사용한다고 생각하면됨
    /// </summary>
    public Dictionary<ECharacter, List<BaseCharacter>> controlCharacter  = new Dictionary<ECharacter, List<BaseCharacter>>() 
    {
        {ECharacter.Player, new List<BaseCharacter>()},
        {ECharacter.Enemy, new List<BaseCharacter>()}
    };
    
    [Header ("Prefabs Section")]
    public GameObject playerPrefab;
    public PlayerPrefabs playerPrefabs;

    protected List<Vector2Int> allPositions = new List<Vector2Int>();

    /// <summary>
    /// 모바일 && 에디터상에서 플레이어가 "현재 고른 자신의 PC"가 어떤 상태인지 판단하기 위한 변수
    /// 실제로 사용할 데이터는 playerControlStatus이고 나머지는 playerControlStatus <- 어떤 상태인지 결정하는 변수라고 생각하면 됨 
    /// </summary>
    public EPlayerControlStatus playerControlStatus = EPlayerControlStatus.None;
    private TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None;
    private Vector2 touchPos = new Vector2(0, 0);

    /// <summary>
    /// characters 변수는 타일에 돌입했을때 화면에 보이는 "모든"기물을 오브젝트 형태로 저장했다고 생각
    /// playerActionUis는 player 기물들의 행동을 결정하는 UI 모음이라고 생각 (currentCtrlCharacter && playerControlStatus에 따라 선택)
    /// </summary>
    private List<GameObject> characters = new List<GameObject>();
    private List<PlayerActionUI> playerActionUis;
    private GameObject currentCtrlCharacter;

    private void Start()
    {
        stateDatas = CSVReader.Read("CharacterDatas");

        // 화면 중간 기준으로 2차원 좌표 전부 저장
        for (int i = 0; i < 81; i++)
        {
            allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        }

        // stateDatas 기준으로 기물 분류 ~Field안에 나누어 생성
        InitCharacter();

        // player 기물 생성
        PlayerSpawn();

        // 기물 생성 후 각각 기물들이 실행되어야하는 Start 함수 호출
        foreach (BaseCharacter playerCharacter in controlCharacter[ECharacter.Player])
        {
            playerCharacter.Start();
        }
    }

    private void Update()
    {
        // 마우스 상태에 따라 화면에 "모든 기물" 선택 확인 (아직 Player만 설정)
        GetCilckObject();
    }

    private void GetCilckObject()
    {
        // touchState, touchPos 값 변경 자세한 내용은 TouchSetUp 정의피킹
        TouchUtil.TouchSetUp(ref touchState, ref touchPos);

        // 아직 어떤 기물도 선택하지 않았을 때
        if (playerControlStatus == EPlayerControlStatus.None)
        {
            if (touchState == TouchUtil.ETouchState.Began)
            {
                // LayerMask에 따라 "기물"인지 아닌지 판별
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(touchPos), Vector3.forward, 15f, LayerMask.GetMask("Token"));

                // 플레이어 기물일때
                if (hit.collider != null && hit.collider.gameObject.tag == "Player")
                {
                    currentCtrlCharacter = hit.transform.gameObject;
                    //playerActionUI = currentCtrlCharacter.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>();
                    foreach (GameObject child in characters)
                    {
                        if (child == currentCtrlCharacter)
                        {
                            child.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>().ActiveUI();
                        }
                        else
                        {
                            child.transform.GetChild(0).GetChild(0).GetComponent<PlayerActionUI>().PassiveUI();
                        }
                    }
                }

                // 적 기물일때
                else if (hit.collider != null && hit.collider.gameObject.tag == "Enemy")
                {
                    hit.collider.gameObject.GetComponent<Enemy>().EnemyActionInfo();
                }

                // 어떤 기물도 선택되지 않았을 때 -> 비어있는 화면을 눌렀을때 화면에 보이는 모든 UI 제거 (기본 UI 제외)
                else
                {
                    currentCtrlCharacter = null;
                    foreach (PlayerActionUI playerUi in playerActionUis)
                    {
                        playerUi.PassiveUI();
                    }
                    //uiManager.PassiveEnemyInfoUI();
                }
            }
        }
    }

    private void InitCharacter()
    {
        BaseCharacter _baseCharacter;
        for (int dataCount = 0; dataCount < stateDatas.Count; ++dataCount)
        {
            switch ((int)stateDatas[dataCount]["position"]) // 현재 포지션 기준으로 클래스를 나눴지만 추후 구분해 어떤걸 넣을지 생각
            {
                case 0:
                    _baseCharacter = new TankerCharacter(this);           
                    break;
                case 1:
                    _baseCharacter = new AttackerCharacter(this);
                    break;
                case 2:
                    _baseCharacter = new SupporterCharacter(this);
                    break;
                default:
                    _baseCharacter = new BaseCharacter(this);
                    Debug.LogError("해당 포지션은 올바르지않은 데이터입니다.");
                    break;
            }
            _baseCharacter.SetData(stateDatas[dataCount]);
            if (bool.Parse(stateDatas[dataCount]["playable"].ToString())) playerCharacterFields.Add(_baseCharacter.DeepCopy());
            else enemyCharacterFields.Add(_baseCharacter.DeepCopy());
        }
    }

    private void PlayerSpawn()
    {
        List<int> alreadySpawned = new List<int>();
        Vector2 playerPos;
        for (int playerCount = 0; playerCount < GameManager.Instance.playerCount; ++playerCount)
        {
            int x;
            // 좌표 뽑아오기
            do
            {
                x = Random.Range(-4, 5);
            } while (alreadySpawned.Contains(x));
            alreadySpawned.Add(x);
            playerPos = new Vector2(x, -4);
            // 도대체 이 병신은 이 줄을 왜 추가 했을까
            BaseCharacter baseCharacter = playerCharacterFields[Random.Range(0, playerCharacterFields.Count)];
            GameObject spawnObject = Instantiate(playerPrefab, playerPos * GameManager.gridSize, Quaternion.identity);

            PlayerActionUI playerActionUI = Instantiate(playerPrefabs.actionUI, spawnObject.transform).transform.GetChild(0).GetComponent<PlayerActionUI>();
            playerActionUI.GetComponentInParent<Canvas>().sortingLayerName = "Text"; // 임시 적용

            switch (baseCharacter.characterPosition)
            {
                case BaseCharacter.EPositionType.Attacker:
                    controlCharacter[ECharacter.Player].Add(new AttackerCharacter(this));
                    break;
                case BaseCharacter.EPositionType.Tanker:
                    controlCharacter[ECharacter.Player].Add(new TankerCharacter(this));
                    break;
                case BaseCharacter.EPositionType.Supporter:
                    controlCharacter[ECharacter.Player].Add(new SupporterCharacter(this));
                    break;
                default:
                    break;
            }
            controlCharacter[ECharacter.Player][playerCount] = baseCharacter.DeepCopy();
            controlCharacter[ECharacter.Player][playerCount].mPosition = playerPos;
            controlCharacter[ECharacter.Player][playerCount].id = playerCount;

            playerActionUI.baseCharacter = controlCharacter[ECharacter.Player][playerCount];

            spawnObject.GetComponent<SpriteRenderer>().sprite = baseCharacter.characterSprite;
            spawnObject.name = baseCharacter.characterName + playerCount;
            characters.Add(spawnObject);
            playerActionUis.Add(playerActionUI);
        }
    }

    #region BaseCharacterFuc
    public GameObject GetObjectToPosition(Vector3 pos)
    {
        foreach(GameObject character in characters)
        {
            if(character.transform.position == pos)
            {
                return character;
            }
        }
        return null;
    }

    public GameObject SetObjectToParent(GameObject child, GameObject parent = null, Vector3? pos = null, Quaternion? rot = null)
    {
        if (parent)
        {
            return Instantiate(child, pos.HasValue ? pos.Value : Vector3.zero, rot.HasValue ? rot.Value : Quaternion.identity, parent.transform);
        }
        else
        {
            return Instantiate(child, pos.HasValue ? pos.Value : Vector3.zero, rot.HasValue ? rot.Value : Quaternion.identity);
        }
    }
    #endregion
}
