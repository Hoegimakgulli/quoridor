using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HM.Containers;
using CharacterDefinition;
using HM.Utils;

public class CharacterController : MonoBehaviour
{
    public List<Dictionary<string, object>> stateDatas                   = new List<Dictionary<string, object>>();
    public List<BaseCharacter> playerCharacterFields                     = new List<BaseCharacter>();
    public List<BaseCharacter> enemyCharacterFields                      = new List<BaseCharacter>();
    public Dictionary<ECharacter, List<BaseCharacter>> controlCharacter  = new Dictionary<ECharacter, List<BaseCharacter>>() 
    {
        {ECharacter.Player, new List<BaseCharacter>()},
        {ECharacter.Enemy, new List<BaseCharacter>()}
    };
    public EPlayerControlStatus playerControlStatus                      = EPlayerControlStatus.None;
    
    [Header ("Prefabs Section")]
    public GameObject playerPrefab;
    public PlayerPrefabs playerPrefabs;

    protected List<Vector2Int> allPositions = new List<Vector2Int>();

    private TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None;
    private Vector2 touchPos = new Vector2(0, 0);

    private List<GameObject> characters = new List<GameObject>();
    private List<PlayerActionUI> playerActionUis;
    private GameObject currentCtrlCharacter;

    private void Start()
    {
        stateDatas = CSVReader.Read("CharacterDatas");
        for (int i = 0; i < 81; i++)
        {
            allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        }

        InitCharacter();
        Debug.Log("Character Debug Start");
        foreach(BaseCharacter ch in playerCharacterFields)
        {
            Debug.Log(ch.characterName);
        }
        Debug.Log("Enemy Character Debug Start");
        foreach (BaseCharacter ch in enemyCharacterFields)
        {
            Debug.Log(ch.characterName);
        }
        PlayerSpawn();

        foreach (BaseCharacter playerCharacter in controlCharacter[ECharacter.Player])
        {
            playerCharacter.Start();
        }
    }

    private void Update()
    {
        GetCilckObject();
    }

    private void GetCilckObject()
    {
        TouchUtil.TouchSetUp(ref touchState, ref touchPos);

        if (playerControlStatus == EPlayerControlStatus.None)
        {
            if (touchState == TouchUtil.ETouchState.Began)
            {
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(touchPos), Vector3.forward, 15f, LayerMask.GetMask("Token"));

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

                else if (hit.collider != null && hit.collider.gameObject.tag == "Enemy")
                {
                    hit.collider.gameObject.GetComponent<Enemy>().EnemyActionInfo();
                }

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
            controlCharacter[ECharacter.Player][playerCount].position = playerPos;
            controlCharacter[ECharacter.Player][playerCount].id = playerCount;

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
