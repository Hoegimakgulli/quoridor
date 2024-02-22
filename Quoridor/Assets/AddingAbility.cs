    using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AddingAbility : MonoBehaviour
{
    private Text inputText;
    //private GameObject player;
    private PlayerAbility playerAbility;
    // Start is called before the first frame update
    void Start()
    {
        //inputText = transform.GetChild(2).GetComponent<Text>();
        playerAbility = GameObject.FindWithTag("Player").GetComponent<PlayerAbility>();
    }

    public void AddAbility()
    {
        inputText = transform.GetChild(2).GetComponent<Text>();
        playerAbility.AddAbility(int.Parse(inputText.text));
    }
}
