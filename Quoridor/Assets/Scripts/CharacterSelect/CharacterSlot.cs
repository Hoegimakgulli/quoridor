using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlot : MonoBehaviour
{
    public CharacterSelectionManager csm;
    public int a = 1;
    public bool isSelectable = true; //활성화 여부
    private void Awake()
    {
        csm = GameObject.Find("Character Selection Manager").GetComponent<CharacterSelectionManager>();
        csm.slotObject.Add(gameObject);
    }
    private void Start()
    {
        GetComponent<Button>().onClick.AddListener(SelectThisCharacter);
        gameObject.GetComponent<RectTransform>().localScale = Vector3.one; //화면 크기에 따라 스케일이 달라져서 고정시킴
    }

    public void SelectThisCharacter()
    {
        if (csm.selectedCharacter != csm.slotObject[transform.GetSiblingIndex()])
        {
            csm.SelectCharacter(transform.GetSiblingIndex());
        }
    }


}
