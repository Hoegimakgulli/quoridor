using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSlotBag : MonoBehaviour
{
    CharacterSelectionManager csm;
  
    private RectTransform slotBackgroundRT;
    private RectTransform slotGridRT;
    
    private void Awake()
    {
        csm = GameObject.Find("Character Selection Manager").GetComponent<CharacterSelectionManager>();

        //캐릭터 슬롯 배경 및 그리드 크기 조절
        csm.charactersNum = csm.slotSprite.Count;
        slotBackgroundRT = transform.GetChild(0).gameObject.GetComponent<RectTransform>();
        slotBackgroundRT.sizeDelta = new Vector2(csm.charactersNum * 100 - (csm.charactersNum - 1) * 10, 100);
        slotGridRT = transform.GetChild(0).GetChild(0).gameObject.GetComponentInParent<RectTransform>();
        slotGridRT.sizeDelta = new Vector2(csm.charactersNum * 100, 100);
        
        //캐릭터 슬롯 생성
        for(int i = 0; i < csm.charactersNum; i++)
        {
            GameObject characterSlotSprite = new GameObject("Character Slot " + i);
            characterSlotSprite.transform.SetParent(transform.GetChild(0).GetChild(0));
            characterSlotSprite.AddComponent<RectTransform>();
            characterSlotSprite.AddComponent<Image>().sprite = csm.slotSprite[i];
            characterSlotSprite.AddComponent<CharacterSlot>();
            characterSlotSprite.AddComponent<Button>();
        }
    }

}
