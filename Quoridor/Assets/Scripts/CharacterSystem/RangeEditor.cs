using System.Collections;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor;
using UnityEngine;
using CharacterDefinition;

//[CustomEditor(typeof(RangeFrame), true)]
//[CanEditMultipleObjects]
//public class RangeEditor : Editor
//{
//    public override void OnInspectorGUI()
//    {
//        base.OnInspectorGUI();

//        serializedObject.Update();

//        RangeFrame rangeFrame = target as RangeFrame;

//        GUIStyle style = EditorStyles.helpBox;
//        GUILayout.BeginVertical(style);
//        EditorGUILayout.PropertyField(serializedObject.FindProperty("moveProperties"), true);
//        if(GUILayout.Button("Create Move Property"))
//        {
//            rangeFrame.moveProperties.Add(new RangeSetting());
//        }
//        GUILayout.EndVertical();

//        //EditorGUILayout.PropertyField(serializedObject.FindProperty("attackProperties"), true);
//        //EditorGUILayout.PropertyField(serializedObject.FindProperty("abilityProperties"), true);



//        //foreach(RangeSetting item in settingData.Item1)
//        //{
//        //    EPlayerRangeField type = (EPlayerRangeField)EditorGUILayout.EnumPopup("Range", item._playerField);
//        //    switch (type)
//        //    {
//        //        case EPlayerRangeField.Move:
//        //            break;
//        //        case EPlayerRangeField.Attack:
//        //            EditorGUILayout.Fild
//        //            break;
//        //        case EPlayerRangeField.Ability:
//        //            break;
//        //        default:
//        //            break;
//        //    }
//        //}

//        //// 변경된 프로퍼티를 저장해줍니다.
//        serializedObject.ApplyModifiedProperties();
//    }
//}
