using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEditor;

public class PreviewWall : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Button completeBuildButton;
    public bool isBlock = false;
    RectTransform[] WallButtonT = new RectTransform[2]; //벽 설치 버튼의 렉트 트랜스폼. 0 = X버튼, 1 = O버튼
    Button OButton;
    GameManager gameManager;
    int prevRotation;

    private void Awake()
    {
        for (int i = 0; i < 2; i++)
        {
            WallButtonT[i] = transform.GetChild(0).GetChild(i).GetComponent<RectTransform>();
        }
        OButton = transform.GetChild(0).GetChild(1).GetComponent<Button>();
    }
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
        //completeBuildButton = GameObject.Find("BuildComplete").GetComponent<Button>();
    }
    private void OnEnable()
    {
        SetButtonPos();
        prevRotation = (int)transform.rotation.z;
        for (int i = 0; i < 2; i++)
        {
            WallButtonT[i].gameObject.SetActive(true);
            WallButtonT[i].DOScale(Vector2.zero, 0);
            WallButtonT[i].DOScale(new Vector2(0.01f, 0.01f), 0.3f); //벽의 미리보기나 버튼의 스케일이 달라진다면 수정해줄것.
        }
    }
    private void FixedUpdate()
    {
        SetButtonPos();
        if (isBlock)
        {
            gameObject.tag = "CantBuild";
            spriteRenderer.color = new Color(1, 0, 0, 0.4f);
            OButton.interactable = false;
        }
        else
        {
            gameObject.tag = "Wall";
            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            OButton.interactable = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer == 6 || isBlock)
        {
            gameObject.tag = "CantBuild";
            spriteRenderer.color = new Color(1, 0, 0, 0.4f);
            OButton.interactable = false;
        }
        else
        {
            gameObject.tag = "Wall";
            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            OButton.interactable = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        gameObject.tag = "Wall";
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);
        OButton.interactable = true;
    }

    //버튼들의 위치를 세팅함.
    private void SetButtonPos()
    {
        //버튼의 위치가 수정된다면 아래 if문과 if else문을 수정할 것.
        if (transform.rotation.z == 0)
        {
            WallButtonT[0].anchoredPosition = new Vector2(-1, -1);
            WallButtonT[1].anchoredPosition = new Vector2(1, -1);
            foreach (RectTransform rt in WallButtonT)
            {
                rt.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
        else
        {
            WallButtonT[0].anchoredPosition = new Vector2(-1, 1);
            WallButtonT[1].anchoredPosition = new Vector2(-1, -1);
            foreach (RectTransform rt in WallButtonT)
            {
                rt.rotation = Quaternion.Euler(0, 0, 0);
            }
        }
    }

    public void OnCompleteBuildClick() // 건설 완료 버튼
    {
        if (gameManager.player.GetComponent<Player>().BuildComplete())
        {
            for (int i = 0; i < 2; i++)
            {
                WallButtonT[i].gameObject.SetActive(false);
            }
        }
        //gameManager.player.GetComponent<Player>().BuildComplete();
        //for (int i = 0; i < 2; i++)
        //{
        //    WallButtonT[i].gameObject.SetActive(false);
        //}
        // gameManager.playerActionUI.ActiveUI();
        gameManager.uiManager.WallCountText.text = $"남은 벽 수 : {GameManager.Instance.playerMaxBuildWallCount - GameManager.Instance.playerWallCount} / {GameManager.Instance.playerMaxBuildWallCount}"; //벽 갯수 표기
    }

    //벽 건설 취소
    public void CancleBuild()
    {
        gameManager.playerActionUI.ActiveUI();
        gameManager.playerControlStatus = GameManager.EPlayerControlStatus.None;
        gameManager.player.GetComponent<Player>().ResetPreview();
    }
}
