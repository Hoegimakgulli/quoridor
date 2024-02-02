using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PreviewWall : MonoBehaviour
{
    SpriteRenderer spriteRenderer;
    Button completeBuildButton;
    public bool isBlock = false;
    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        completeBuildButton = GameObject.Find("BuildComplete").GetComponent<Button>();
    }
    private void FixedUpdate()
    {
        if (isBlock)
        {
            gameObject.tag = "CantBuild";
            spriteRenderer.color = new Color(1, 0, 0, 0.4f);
            completeBuildButton.interactable = false;
        }
        else
        {
            gameObject.tag = "Wall";
            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            completeBuildButton.interactable = true;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.gameObject.layer == 6 || isBlock)
        {
            gameObject.tag = "CantBuild";
            spriteRenderer.color = new Color(1, 0, 0, 0.4f);
            completeBuildButton.interactable = false;
        }
        else
        {
            gameObject.tag = "Wall";
            spriteRenderer.color = new Color(1, 1, 1, 0.4f);
            completeBuildButton.interactable = true;
        }
    }
    private void OnTriggerExit2D(Collider2D other)
    {
        gameObject.tag = "Wall";
        spriteRenderer.color = new Color(1, 1, 1, 0.4f);
        completeBuildButton.interactable = true;
    }
}
