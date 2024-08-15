using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class PlayerActionUI : MonoBehaviour
{
    RectTransform[] playerUIs = new RectTransform[4]; //플레이어용 UI. 0 = 이동. 1 = 건설. 2 = 공격, 3 = 파괴
    Image[] uiImages = new Image[4]; //각 버튼의 이미지
    Image[] uiImages2 = new Image[4]; //각 버튼의 이미지 안의 그림
    RectTransform rt;

    float uiMoveTime = 0.4f;
    public float uiMoveDistance = 50f;
    GameManager gameManager;
    Player player;
    List<Tweener> activeTweens = new List<Tweener>();

    // Start is called before the first frame update
    void Awake()
    {
        rt = GetComponent<RectTransform>();
        for (int i = 0; i < 4; i++)
        {
            playerUIs[i] = transform.GetChild(i).GetComponent<RectTransform>();
            uiImages[i] = transform.GetChild(i).GetComponent<Image>();
            uiImages2[i] = transform.GetChild(i).GetChild(0).GetComponent<Image>();
        }
        rt.localScale = Vector2.zero;
    }
    private void Start()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        player = transform.parent.parent.GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.L))
        {
            ActiveUI();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            SelectActionAnim(0);
        }
        if (Input.GetKeyDown(KeyCode.J))
        {
            SelectActionAnim(1);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            SelectActionAnim(2);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            SelectActionAnim(3);
        }
    }

    //플레이어 UI 등장
    public void ActiveUI()
    {
        foreach (var tween in activeTweens) //실행중인 tween들을 전부 킬
        {
            tween.Kill();
        }
        bool[] conditions = new bool[4] { player.canMove, player.canBuild, player.canAttack, player.canDestroy };
        if (player != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (conditions[i])
                {
                    uiImages[i].raycastTarget = true; //버튼이 클릭 가능한 상태로
                    uiImages2[i].raycastTarget = true; //버튼이 클릭 가능한 상태로
                    playerUIs[i].localScale = Vector2.one;
                    playerUIs[i].anchoredPosition = Vector2.zero;
                    uiImages[i].DOFade(0.5f, 0);
                    uiImages2[i].DOFade(0.5f, 0);
                }
                else
                {
                    uiImages[i].raycastTarget = false; //버튼이 클릭 불가능한 상태로
                    uiImages2[i].raycastTarget = false; //버튼이 클릭 불가능한 상태로
                    playerUIs[i].localScale = Vector2.zero; //크기 0으로
                }
            }
        }
        // if (player != null && player.canAction) //플레이어가 이동 및 건설 가능한 상태라면
        // {
        //     for (int i = 0; i < 2; i++)
        //     {
        //         uiImages[i].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         uiImages2[i].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         playerUIs[i].localScale = Vector2.one;
        //         playerUIs[i].anchoredPosition = Vector2.zero;
        //         uiImages[i].DOFade(0.5f, 0);
        //         uiImages2[i].DOFade(0.5f, 0);
        //     }
        // }
        // else if ((player != null && player.buildCount > 0) || (player != null && player.moveCount > 0)) //플레이어가 건설 가능한 상태라면
        // {
        //     if (player.buildCount > 0)
        //     {
        //         uiImages[1].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         uiImages2[1].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         playerUIs[1].localScale = Vector2.one;
        //         playerUIs[1].anchoredPosition = Vector2.zero;
        //         uiImages[1].DOFade(0.5f, 0);
        //         uiImages2[1].DOFade(0.5f, 0);
        //     }
        //     if (player.moveCount > 0)
        //     {
        //         uiImages[0].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         uiImages2[0].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         playerUIs[0].localScale = Vector2.one;
        //         playerUIs[0].anchoredPosition = Vector2.zero;
        //         uiImages[0].DOFade(0.5f, 0);
        //         uiImages2[0].DOFade(0.5f, 0);
        //     }
        // }
        // else //플레이어가 이동 및 건설이 불가능한 상태라면
        // {
        //     for (int i = 0; i < 2; i++)
        //     {
        //         uiImages[i].raycastTarget = false; //버튼이 클릭 불가능한 상태로
        //         uiImages2[i].raycastTarget = false; //버튼이 클릭 불가능한 상태로
        //         playerUIs[i].localScale = Vector2.zero; //크기 0으로
        //     }
        // }

        if (player != null && player.canMove) //플레이어가 움직임 가능한 상태라면
        {
            {
                uiImages[0].raycastTarget = true; //버튼이 클릭 가능한 상태로
                uiImages2[0].raycastTarget = true; //버튼이 클릭 가능한 상태로
                playerUIs[0].localScale = Vector2.one;
                playerUIs[0].anchoredPosition = Vector2.zero;
                uiImages[0].DOFade(0.5f, 0);
                uiImages2[0].DOFade(0.5f, 0);
            }
        }
        else //플레이어가 움직임이 불가능한 상태라면
        {
            uiImages[0].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            uiImages2[0].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            playerUIs[0].localScale = Vector2.zero; //크기 0으로
        }

        if (player != null && player.canAction) //플레이어가 빌딩 가능한 상태라면
        {
            {
                uiImages[1].raycastTarget = true; //버튼이 클릭 가능한 상태로
                uiImages2[1].raycastTarget = true; //버튼이 클릭 가능한 상태로
                playerUIs[1].localScale = Vector2.one;
                playerUIs[1].anchoredPosition = Vector2.zero;
                uiImages[1].DOFade(0.5f, 0);
                uiImages2[1].DOFade(0.5f, 0);
            }
        }
        else //플레이어가 빌딩이 불가능한 상태라면
        {
            uiImages[1].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            uiImages2[1].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            playerUIs[1].localScale = Vector2.zero; //크기 0으로
        }

        if (player != null && player.canAttack) //플레이어가 공격 가능한 상태라면
        {
            {
                uiImages[2].raycastTarget = true; //버튼이 클릭 가능한 상태로
                uiImages2[2].raycastTarget = true; //버튼이 클릭 가능한 상태로
                playerUIs[2].localScale = Vector2.one;
                playerUIs[2].anchoredPosition = Vector2.zero;
                uiImages[2].DOFade(0.5f, 0);
                uiImages2[2].DOFade(0.5f, 0);
            }
        }
        else //플레이어가 공격이 불가능한 상태라면
        {
            uiImages[2].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            uiImages2[2].raycastTarget = false; //버튼이 클릭 불가능한 상태로
            playerUIs[2].localScale = Vector2.zero; //크기 0으로
        }
        // if (player != null && player.canAttack) //플레이어가 공격 가능한 상태라면
        // {
        //     {
        //         uiImages[2].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         uiImages2[2].raycastTarget = true; //버튼이 클릭 가능한 상태로
        //         playerUIs[2].localScale = Vector2.one;
        //         playerUIs[2].anchoredPosition = Vector2.zero;
        //         uiImages[2].DOFade(0.5f, 0);
        //         uiImages2[2].DOFade(0.5f, 0);
        //     }
        // }
        // else //플레이어가 공격이 불가능한 상태라면
        // {
        //     uiImages[2].raycastTarget = false; //버튼이 클릭 불가능한 상태로
        //     uiImages2[2].raycastTarget = false; //버튼이 클릭 불가능한 상태로
        //     playerUIs[2].localScale = Vector2.zero; //크기 0으로
        // }


        rt.localScale = Vector2.zero;
        activeTweens.Add(rt.DOScale(Vector2.one, 0.4f).SetEase(Ease.OutBack));
    }

    //이동 버튼을 눌렀을 때
    public void MoveClick()
    {
        SelectActionAnim(1);
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Move;
        player.ResetPreview();
    }

    //건설 버튼을 눌렀을 때
    public void BuildClick()
    {
        SelectActionAnim(2);
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Build;
        player.ResetPreview();
    }

    //공격 버튼을 눌렀을 때
    public void AttackClick()
    {
        SelectActionAnim(3);
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Attack;
        player.ResetPreview();
    }
    public void DestroyClick()
    {
        SelectActionAnim(4);
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.Destroy;
        player.ResetPreview();
    }

    //그 외의 것을 눌렀을 때 (이동, 건설, 공격 말고 다른걸 선택해서 전부 사라져야 할 때)
    public void PassiveUI()
    {
        SelectActionAnim(0);
    }

    //UI 클릭 시 애니메이션 연출. 0 = 아무것도 선택하지 않음, 1 = 이동. 2 = 건설. 3 = 공격. 4 = 파괴
    private void SelectActionAnim(int select)
    {
        for (int i = 0; i < 4; i++)
        {
            uiImages[i].raycastTarget = false; //버튼이 사라지는 동안 클릭이 안되도록 만듦
            uiImages2[i].raycastTarget = false; //버튼이 사라지는 동안 클릭이 안되도록 만듦
            if (i != select - 1)
            {
                activeTweens.Add(playerUIs[i].DOScale(Vector2.zero, uiMoveTime / 2).SetEase(Ease.InBack));
            }
            else
            {
                activeTweens.Add(uiImages[i].DOFade(0, uiMoveTime));
                activeTweens.Add(uiImages[i].gameObject.transform.GetChild(0).GetComponent<Image>().DOFade(0, uiMoveTime));
                switch (i)
                {
                    case 0:
                        activeTweens.Add(playerUIs[i].DOAnchorPos(new Vector2(Mathf.Cos(115.5f) * uiMoveDistance, Mathf.Sin(115.5f) * uiMoveDistance), uiMoveTime));
                        break;
                    case 1:
                        activeTweens.Add(playerUIs[i].DOAnchorPos(new Vector2(0, uiMoveDistance), uiMoveTime));
                        break;
                    case 2:
                        activeTweens.Add(playerUIs[i].DOAnchorPos(new Vector2(Mathf.Cos(25.5f) * uiMoveDistance, Mathf.Sin(25.5f) * uiMoveDistance), uiMoveTime));
                        break;
                    case 4:
                        activeTweens.Add(playerUIs[i].DOAnchorPos(new Vector2(0, -uiMoveDistance), uiMoveTime));
                        break;
                    default: break;
                }

            }

        }

    }
}
