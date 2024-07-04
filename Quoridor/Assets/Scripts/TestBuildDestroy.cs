using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class DataCommunicator
{
    public static Dictionary<string, Messanger> data = new Dictionary<string, Messanger>();
    public static void Set(string key, Messanger value)
    {
        Debug.Log($"Set {key} to DataCommunicator");
        value.Log();
        if (data.ContainsKey(key))
        {
            data[key] = value;
            return;
        }
        data.Add(key, value);
    }
    public static Messanger Get(string key)
    {
        if (!data.ContainsKey(key))
        {
            throw new KeyNotFoundException("Key not found");
        }
        return data[key];
    }
    public static bool TryGet(string key, out Messanger value)
    {
        Debug.Log($"TryGet {key} from DataCommunicator");
        return data.TryGetValue(key, out value);
    }
}
public class Messanger
{
    Dictionary<string, object> data = new Dictionary<string, object>();
    public Messanger(params KeyValuePair<string, object>[] parameters)
    {
        foreach (var param in parameters)
        {
            data.Add(param.Key, param.Value);
        }
    }
    public Messanger(params object[] parameters)
    {
        if (parameters.Length % 2 != 0)
        {
            throw new ArgumentException("Invalid parameters");
        }
        for (int i = 0; i < parameters.Length; i += 2)
        {
            data.Add((string)parameters[i], parameters[i + 1]);
        }
    }
    public void Add(string key, object value)
    {
        data.Add(key, value);
    }
    public T Get<T>(string key)
    {
        if (!data.ContainsKey(key))
        {
            throw new KeyNotFoundException("Key not found");
        }
        if (data[key].GetType() != typeof(T))
        {
            throw new InvalidCastException("Invalid cast");
        }
        return (T)data[key];
    }
    public bool TryGet<T>(string key, out T value)
    {
        if (data.TryGetValue(key, out object obj))
        {
            if (obj.GetType() == typeof(T))
            {
                value = (T)obj;
                return true;
            }
        }
        value = default;
        return false;
    }
    public void Log()
    {
        foreach (var item in data)
        {
            Debug.Log($"{item.Key} : {item.Value}");
        }

    }
}
public class TestBuildDestroy : MonoBehaviour
{
    [SerializeField]
    InputField mapInput, buildInput, destroyInput;
    public int mapWallCount, buildWallCount, destroyWallCount;
    public void OnClickStart()
    {
        DataCommunicator.Set("MaxWallData", new Messanger("MapWallCount", mapWallCount, "BuildWallCount", buildWallCount, "DestroyWallCount", destroyWallCount));
        SceneManager.LoadScene("SampleScene");
    }
    private void Awake()
    {
        mapInput.onValueChanged.AddListener(OnValueChangedMap);
        buildInput.onValueChanged.AddListener(OnValueChangedBuild);
        destroyInput.onValueChanged.AddListener(OnValueChangedDestroy);
    }
    public void OnValueChangedMap(string value)
    {
        value = mapInput.text;
        if (int.TryParse(value, out int i))
        {
            mapWallCount = i;
            Debug.Log(i);
        }
    }
    public void OnValueChangedBuild(string value)
    {
        value = buildInput.text;
        if (int.TryParse(value, out int i))
        {
            buildWallCount = i;
            Debug.Log(i);
        }
    }
    public void OnValueChangedDestroy(string value)
    {
        value = destroyInput.text;
        if (int.TryParse(value, out int i))
        {
            destroyWallCount = i;
            Debug.Log(i);
        }
    }
}
