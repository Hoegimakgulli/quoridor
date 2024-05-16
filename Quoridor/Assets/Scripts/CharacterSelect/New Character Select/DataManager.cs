using UnityEngine;
using System.IO;
using System.Data.Common;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;

public class DataManager : MonoBehaviour
{
    static GameObject container;


    static DataManager dm;

    public static DataManager DM
    {
        get
        {
            if(dm == null)
            {
                container = new GameObject();
                container.name = "Data Manager";
                dm = container.AddComponent(typeof(DataManager)) as DataManager;
                DontDestroyOnLoad(container);
            }
            return dm;
        }
    }

    string GameDataFileName = "GameData.json";

    public GameData data = new GameData();


    //저장된 데이터 불러오기
    public void LoadGameData()
    {
        string filePath = Application.persistentDataPath + "/" + GameDataFileName;

        if (File.Exists(filePath))
        {
            string fromJsonData = File.ReadAllText(filePath);
            data = JsonUtility.FromJson<GameData>(fromJsonData);
        }

        data.ResetData(); SaveGameData();  //고정 데이타 초기화 (임시)
    }

    //데이터 저장
    public void SaveGameData()
    {
        string toJsonData = JsonUtility.ToJson(data, true);
        string filePath = Application.persistentDataPath + "/" + GameDataFileName;

        File.WriteAllText(filePath, toJsonData);
    }

    //(임시) 캐릭터 잠금 해제
    public void UnlockCharacter(int characterNum)
    {
        data.isUnlockCharacter[characterNum] = true;
        SaveGameData();
    }

    //(임시) 캐릭터 잠금
    public void LockCharacter(int characterNum)
    {
        data.isUnlockCharacter[characterNum] = false;
        SaveGameData();
    }

    //(임시) 캐릭터의 잠금 해제 여부를 bool 배열로 반환
    public bool[] GetUnlockCharacter()
    {
        return data.isUnlockCharacter;
    }

    //(임시) 캐릭터의 이름을 string 배열로 반환
    public string[] GetCharacterName()
    {
        return data.characterName;
    }
    
    //(임시) 캐릭터의 스토리를 string 배열로 반환
    public string[] GetCharacterStory()
    {
        return data.characterStory;
    }

    //(임시) 캐릭터의 스킬 텍스트를 string 배열로 변환
    public string[] GetCharacterSkill()
    {
        return data.characterSkill;
    }

    //(임시) 이번 스테이지에서 캐릭 몇명 선택 가능한지를 반환
    public int GetSelectCharacterNum()
    {
        return data.selectNum;
    }
    
    //(임시) 현재 파티 구성원을 저장함
    public void SetPartyList(List<int> inputList)
    {
        data.partyList = inputList;
        SaveGameData();
    }

    //(임시) 저장된 파티 구성원을 반환
    public List<int> GetPartyList()
    {
        return data.partyList;
    }
}
