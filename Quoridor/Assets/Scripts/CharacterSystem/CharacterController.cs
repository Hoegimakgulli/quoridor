using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterState;
using HM.Containers;
using HM.Utils;

public class CharacterController : MonoBehaviour
{
    public List<Dictionary<string, object>> stateDatas                          = new List<Dictionary<string, object>>();
    public List<BaseCharacter> characterFields                                  = new List<BaseCharacter>();
    public Dictionary<CharacterDefinition.ECharacter, List<BaseCharacter>> controlCharacter  = new Dictionary<CharacterDefinition.ECharacter, List<BaseCharacter>>();
    public CharacterDefinition.EPlayerControlStatus playerControlStatus                      = CharacterDefinition.EPlayerControlStatus.None;
    
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
        for (int i = 0; i < 81; i++)
        {
            allPositions.Add(new Vector2Int(i % 9 - 4, i / 9 - 4));
        }

        InitCharacter();
    }

    private void Update()
    {
        GetCilckObject();
    }

    private void GetCilckObject()
    {
        TouchUtil.TouchSetUp(ref touchState, ref touchPos);

        if (playerControlStatus == CharacterDefinition.EPlayerControlStatus.None)
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
        for (int dataCount = 0; dataCount < stateDatas.Count; ++dataCount)
        {
            switch ((int)stateDatas[dataCount]["position"]) // 현재 포지션 기준으로 클래스를 나눴지만 추후 구분해 어떤걸 넣을지 생각
            {
                case 0:
                    characterFields.Add(new TankerCharacter(this));
                    break;
                case 1:
                    characterFields.Add(new AttackerCharacter(this));
                    break;
                case 2:
                    characterFields.Add(new SupporterCharacter(this));
                    break;
                default:
                    Debug.LogError("해당 포지션은 올바르지않은 데이터입니다.");
                    break;
            }
            characterFields[dataCount].SetData(stateDatas[dataCount]);
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
            BaseCharacter baseCharacter = controlCharacter[CharacterDefinition.ECharacter.Player][Random.Range(0, controlCharacter[CharacterDefinition.ECharacter.Player].Count)];
            GameObject spawnObject = Instantiate(playerPrefab, playerPos * GameManager.gridSize, Quaternion.identity);

            PlayerActionUI playerActionUI = Instantiate(playerPrefabs.actionUI, spawnObject.transform).transform.GetChild(0).GetComponent<PlayerActionUI>();
            playerActionUI.GetComponentInParent<Canvas>().sortingLayerName = "Text"; // 임시 적용

            switch (baseCharacter.characterPosition)
            {
                case BaseCharacter.EPositionType.Attacker:
                    controlCharacter[CharacterDefinition.ECharacter.Player].Add(new AttackerCharacter(this));
                    break;
                case BaseCharacter.EPositionType.Tanker:
                    controlCharacter[CharacterDefinition.ECharacter.Player].Add(new TankerCharacter(this));
                    break;
                case BaseCharacter.EPositionType.Supporter:
                    controlCharacter[CharacterDefinition.ECharacter.Player].Add(new SupporterCharacter(this));
                    break;
                default:
                    break;
            }
            controlCharacter[CharacterDefinition.ECharacter.Player][playerCount].SetData(baseCharacter.SendData());
            controlCharacter[CharacterDefinition.ECharacter.Player][playerCount].position = playerPos;
            controlCharacter[CharacterDefinition.ECharacter.Player][playerCount].id = playerCount;

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
