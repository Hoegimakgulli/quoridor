using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
public static class Json
{
    /*
    public static void SaveWallData(PositionData wallData)
    {
        string data = JsonUtility.ToJson(wallData);
        string savePath = "Assets/Save";
        string saveFile = savePath + "/WallData.json";
        File.WriteAllText(saveFile, data);
    }
    public static PositionData LoadWallData()
    {
        string path = "Assets/wallData.json";
        string data = File.ReadAllText(path);

        PositionData wallData = JsonUtility.FromJson<PositionData>(data);
        return wallData;
    }
    */
}
[Serializable]
public class PositionData
{
    /// <summary> 플레이어 기물의 위치 </summary>
    public int a;
    /// <summary> 적 기물들의 위치 (각 기물마다의 위치) </summary>
    public int b;
    /// <summary> 건설 된 벽의 위치 </summary>
    public int c;
    /// <summary> 능력을 통해 생성되는 지역 기반 오브젝트 </summary>
    public int d;
}
[Serializable]
public class EnemyData
{
    /// <summary> 현재 스테이지 생성된 적 기물의 종류 </summary>
    public int characteristic;
    /// <summary> 현재 스테이지 생성된 적 기물의 개수 </summary>
    public int enemyCount;
    /// <summary> 각 적 기물의 현재 체력 </summary>
    public int hp;
    /// <summary> 각 적 기물의 현재 행동력 </summary>
    public int cost;

}
[Serializable]
public class PlayerData
{
    /// <summary> 플레이어가 선택한 캐릭터 종류 </summary>
    public int playerType;
    /// <summary> 플레이어가 소지중인 벽의 갯수 </summary>
    public int remainWallCount;
    /// <summary> 플레이어가 소지중인 능력 </summary>
    public int playerAbilityMask;
}
[Serializable]
public class StageData
{
    /// <summary> 현재 스테이지 단계 </summary>
    public int stage;
    /// <summary> 진행중인 턴 단계 (몇번째 턴의 누구 차례인지) </summary>
    public int turn;
    /// <summary> 현재 적 코스트 </summary>
    public int cost;
}