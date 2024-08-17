using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HM.Containers;

namespace CharacterState
{
    public class StateManager
    {
        //private static List<Character> playerStates;
        //private static List<Character> enemyStates;

        //public List<Dictionary<string, object>> stateDatas = CSVReader.Read("CharacterStateDatas");

        //// 초기 데이터 셋 저장
        //private void Start()
        //{
        //    Debug.Log("Test CharacterState Start");
        //    for(int count = 0; count < stateDatas.Count; count++)
        //    {
        //        if (stateDatas[count]["playerable"].ToString() == "TRUE")
        //        {
        //            playerStates.Add(
        //                new Character((int)stateDatas[count]["id"], (bool)stateDatas[count]["playerable"], stateDatas[count]["ch_name"].ToString(), (Character.ECharacterType)stateDatas[count]["ch_type"]
        //                , (Character.EPositionType)stateDatas[count]["position"], (int)stateDatas[count]["hp"], (int)stateDatas[count]["atk"], (float)stateDatas[count]["rs"]
        //                , (int)stateDatas[count]["tia"], (int)stateDatas[count]["skill_id"], (int)stateDatas[count]["mov_rg"], (int)stateDatas[count]["atk_rg"]));
        //        }

        //        else if (stateDatas[count]["playerable"].ToString() == "FALSE")
        //        {
        //            enemyStates.Add(
        //                new Character((int)stateDatas[count]["id"], (bool)stateDatas[count]["playerable"], stateDatas[count]["ch_name"].ToString(), (Character.ECharacterType)stateDatas[count]["ch_type"]
        //                , (Character.EPositionType)stateDatas[count]["position"], (int)stateDatas[count]["hp"], (int)stateDatas[count]["atk"], (float)stateDatas[count]["rs"]
        //                , (int)stateDatas[count]["tia"], (int)stateDatas[count]["skill_id"], (int)stateDatas[count]["mov_rg"], (int)stateDatas[count]["atk_rg"]));
        //        }
        //    }
        //}
    }
}
