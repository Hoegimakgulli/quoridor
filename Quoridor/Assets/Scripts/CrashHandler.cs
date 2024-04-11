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
        System.IO.File.WriteAllLines(filePath + fileName, logs);
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
    }
}
