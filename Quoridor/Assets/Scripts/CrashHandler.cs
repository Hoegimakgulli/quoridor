using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrashHandler : MonoBehaviour
{
    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
        Init();
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }
    List<string> logs = new List<string>();
    string filePath;
    string fileName;
    void SaveLogs()
    {
        if (!System.IO.Directory.Exists(filePath))
        {
            System.IO.Directory.CreateDirectory(filePath);
        }
        try
        {
            System.IO.File.WriteAllLines(filePath + fileName, logs);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
    void HandleLog(string logString, string stackTrace, LogType type)
    {
        logs.Add($"[{type}]{logString}");
        switch (type)
        {
            case LogType.Error:
            case LogType.Assert:
            case LogType.Exception:
                {
                    // Debug.Log($"<color=red>[CrashHandler] {type}</color>: {logString}");
                    // Debug.Log($"<color=red>[CrashHandler]</color> Stack Trace: {stackTrace}");
                    logs.Add($"Stack Trace: {stackTrace}");
                    break;
                }
            case LogType.Warning:
            case LogType.Log:
                break;
        }
        SaveLogs();
    }
    public void Init()
    {
        filePath = Application.persistentDataPath + "/logs/";
        fileName = "log-" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + $"{GetComponent<GameManager>().currentStage}" + ".log";
        logs.Clear();
#if UNITY_ANDROID
        // 기기 모델명 가져오기
        string deviceModel = CallAndroidFunction<string>("GetDeviceModel");
        logs.Add("Device Model: " + deviceModel);

        // 안드로이드 버전 가져오기
        string androidVersion = CallAndroidFunction<string>("GetAndroidVersion");
        logs.Add("Android Version: " + androidVersion);
#endif
    }

    // Android Java 클래스명
    private const string androidClassName = "com.HoegidongMakguli.Quoridor.DeviceInfo";

    // Android Java 함수 호출하는 메서드
    private T CallAndroidFunction<T>(string functionName)
    {
        AndroidJavaClass androidClass = new AndroidJavaClass(androidClassName);
        try
        {
            T result = androidClass.CallStatic<T>(functionName);
            androidClass.Dispose();
            return result;
        }
        catch (AndroidJavaException e)
        {
            Debug.LogError("Android Java Exception: " + e.Message);
            androidClass.Dispose();
            return default(T);
        }
    }
}
