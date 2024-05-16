using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            DataManager.DM.UnlockCharacter(0);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            DataManager.DM.UnlockCharacter(1);
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            DataManager.DM.UnlockCharacter(2);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            DataManager.DM.UnlockCharacter(3);
        }
        if (Input.GetKeyDown(KeyCode.G))
        {
            DataManager.DM.UnlockCharacter(4);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            DataManager.DM.UnlockCharacter(5);
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            DataManager.DM.LockCharacter(0);
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            DataManager.DM.LockCharacter(1);
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            DataManager.DM.LockCharacter(2);
        }
        if (Input.GetKeyDown(KeyCode.V))
        {
            DataManager.DM.LockCharacter(3);
        }
        if (Input.GetKeyDown(KeyCode.B))
        {
            DataManager.DM.LockCharacter(4);
        }
        if (Input.GetKeyDown(KeyCode.N))
        {
            DataManager.DM.LockCharacter(5);
        }


        if (Input.GetKeyDown(KeyCode.O))
        {
            DataManager.DM.UnlockCharacter(12);
        }

        if(Input.GetKeyDown(KeyCode.K))
        {
            DataManager.DM.LoadGameData();
        }
    }
}
