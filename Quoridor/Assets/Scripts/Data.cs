using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum EDataType { Ability }
public interface IData
{
    EDataType dataType { get; }
}
public static class Data
{
    public class AbilityData : IData
    {
        public EDataType dataType { get { return EDataType.Ability; } }
        public int id;
        public List<bool> boolData;
        public List<int> intData;
        public List<float> floatData;
        public List<string> stringData;
        public List<Vector3> Vector3Data;
        public List<Vector2> Vector2Data;
        public List<Vector2Int> Vector2IntData;

        public AbilityData() { }
        public AbilityData(int id, bool[] boolArray = null, int[] intArray = null, float[] floatArray = null, string[] stringArray = null, Vector3[] Vector3Array = null, Vector2[] Vector2Array = null, Vector2Int[] Vector2IntArray = null)
        {
            this.id = id;
            this.boolData = new List<bool>(boolArray);
            this.intData = new List<int>(intArray);
            this.floatData = new List<float>(floatArray);
            this.stringData = new List<string>(stringArray);
            this.Vector3Data = new List<Vector3>(Vector3Array);
            this.Vector2Data = new List<Vector2>(Vector2Array);
            this.Vector2IntData = new List<Vector2Int>(Vector2IntArray);
        }
        public AbilityData(int id, List<bool> boolData = null, List<int> intData = null, List<float> floatData = null, List<string> stringData = null, List<Vector3> Vector3Data = null, List<Vector2> Vector2Data = null, List<Vector2Int> Vector2IntData = null)
        {
            this.id = id;
            this.boolData = boolData;
            this.intData = intData;
            this.floatData = floatData;
            this.stringData = stringData;
            this.Vector3Data = Vector3Data;
            this.Vector2Data = Vector2Data;
            this.Vector2IntData = Vector2IntData;
        }
    }
    // public static void SaveData(this IData data, string fileName)
    // {
    //     string filePath = Application.persistentDataPath + "/" + fileName + ".json";
    //     switch (data.dataType)
    //     {
    //         case EDataType.Ability:
    //             AbilityData abilityData = (AbilityData)data;


    //             var json = JsonConvert.SerializeObject(jsonList);
    //             Debug.Log($"Final: {json}, in {filePath}");
    //             File.WriteAllText(filePath, json);
    //             needSave = false;
    //             break;
    //     }
    // }
}
