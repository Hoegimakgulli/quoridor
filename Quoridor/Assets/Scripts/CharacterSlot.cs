using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public CharacterSelectionManager csm;
    public int a = 1;
    public bool isSelectable = true; //Ȱ��ȭ ����
    private void Awake()
    {
        csm = GameObject.Find("Character Selection Manager").GetComponent<CharacterSelectionManager>();
        csm.slotObject.Add(gameObject);
    }
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(SelectThisCharacter);
        gameObject.GetComponent<RectTransform>().localScale = Vector3.one; //ȭ�� ũ�⿡ ���� �������� �޶����� ������Ŵ
    }

    public void SelectThisCharacter()
    {
        if (csm.selectedCharacter != csm.slotObject[transform.GetSiblingIndex()])
        {
            csm.SelectCharacter(transform.GetSiblingIndex());
        }
    }


}
