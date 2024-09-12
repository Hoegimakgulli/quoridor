using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(AxisFrame))]
public class AxisEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        // playerField값 가져오기
        var objectType = (Define.EPlayerAxisField)serializedObject.FindProperty("_playerField").intValue;
        EditorGUILayout.PropertyField(serializedObject.FindProperty("_playerField"));

        // Inspector에 PlayerAxisField와 같은 상태 인스펙터에 노출
        switch (objectType)
        {
            // 변수 호출
            case Define.EPlayerAxisField.Move:
                {
                }
                break;

            case Define.EPlayerAxisField.Attack:
                {
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("_attackIndex.canMultiAttack"));
                }
                break;
        }

        // 변경된 프로퍼티를 저장해줍니다.
        serializedObject.ApplyModifiedProperties();
    }
}
