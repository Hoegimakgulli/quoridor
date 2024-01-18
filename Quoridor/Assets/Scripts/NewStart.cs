using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NewStart : MonoBehaviour
{
    public bool hasPlayData = false; //���� �����Ͱ� �����ϴ��� ����
    private GameObject newGamePopup;

    private void Awake()
    {
        if (newGamePopup == null)
        {
            newGamePopup = GameObject.Find("NewGamePopup");
        }
        newGamePopup.SetActive(false);
    }

    //NEW GAME ��ư�� ������ �� ����
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

    //���� �÷��� ������ �ʱ�ȭ
    public void InitializePlayData()
    {
        /* ������ �ʱ�ȭ�ϴ� �ڵ� �ۼ� �ʿ� */
        hasPlayData = false;
        Debug.Log("������ �ʱ�ȭ");
        StartGame();
    }
}

