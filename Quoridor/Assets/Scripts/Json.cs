using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public static class Json 
{
    public static void SaveWallData(WallData wallData)
    {
        string data = JsonUtility.ToJson(wallData);
        string savePath = "Assets/Save";
        string saveFile = savePath + "/WallData.json";
        File.WriteAllText(saveFile, data);
    }
    public static WallData LoadWallData()
    {
        string path = "Assets/wallData.json";
        string data = File.ReadAllText(path);

        WallData wallData = JsonUtility.FromJson<WallData>(data);
        return wallData;
    }
}
[Serializable]
public class WallData
{
    public float[] wallX;
    public float[] wallY;
    public float[] wallZ;
}
