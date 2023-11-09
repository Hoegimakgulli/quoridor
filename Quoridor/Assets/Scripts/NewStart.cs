using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewStart : MonoBehaviour
{
    public bool hasPlayData = false; //이전 데이터가 존재하는지 여부
    private GameObject newGamePopup;

    private void Awake()
    {
        if (newGamePopup == null)
        {
            newGamePopup = GameObject.Find("NewGamePopup");
        }
        newGamePopup.SetActive(false);
    }

    //NEW GAME 버튼을 눌렀을 때 실행
    public void StartGame()
    {
       
        if(hasPlayData)
        {
            newGamePopup.SetActive(true);
        }
        else
        {
            SceneManager.LoadScene("CharacterSelectionScene");
        }
    }

    //기존 플레이 데이터 초기화
    public void InitializePlayData()
    {
        /* 데이터 초기화하는 코드 작성 필요 */
        hasPlayData = false;
        Debug.Log("데이터 초기화");
        StartGame();
    }
}

