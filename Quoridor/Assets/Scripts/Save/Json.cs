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
    /// <summary> �÷��̾� �⹰�� ��ġ </summary>
    public int a;
    /// <summary> �� �⹰���� ��ġ (�� �⹰������ ��ġ) </summary>
    public int b;
    /// <summary> �Ǽ� �� ���� ��ġ </summary>
    public int c;
    /// <summary> �ɷ��� ���� �����Ǵ� ���� ��� ������Ʈ </summary>
    public int d;
}
[Serializable]
public class EnemyData
{
    /// <summary> ���� �������� ������ �� �⹰�� ���� </summary>
    public int characteristic;
    /// <summary> ���� �������� ������ �� �⹰�� ���� </summary>
    public int enemyCount;
    /// <summary> �� �� �⹰�� ���� ü�� </summary>
    public int hp;
    /// <summary> �� �� �⹰�� ���� �ൿ�� </summary>
    public int cost;

}
[Serializable]
public class PlayerData
{
    /// <summary> �÷��̾ ������ ĳ���� ���� </summary>
    public int playerType;
    /// <summary> �÷��̾ �������� ���� ���� </summary>
    public int remainWallCount;
    /// <summary> �÷��̾ �������� �ɷ� </summary>
    public int playerAbilityMask;
}
[Serializable]
public class StageData
{
    /// <summary> ���� �������� �ܰ� </summary>
    public int stage;
    /// <summary> �������� �� �ܰ� (���° ���� ���� ��������) </summary>
    public int turn;
    /// <summary> ���� �� �ڽ�Ʈ </summary>
    public int cost;
}