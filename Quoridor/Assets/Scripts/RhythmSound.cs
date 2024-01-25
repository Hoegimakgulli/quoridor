using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RhythmSound : MonoBehaviour
{
    RectTransform bar;
    RectTransform needle;
    RectTransform[] energies = new RectTransform[100];
    TMP_Text text;
    TMP_Text comboText;
    Coroutine coroutine;

    bool isPlaying = true;
    bool isRight = true;
    float needlePos = 0;
    public float needleSpeed = 50;
    int soundPower = 0;
    int combo = 0;

    void Start()
    {
        text = transform.GetChild(3).GetComponent<TMP_Text>();
        comboText = transform.GetChild(4).GetComponent<TMP_Text>();
        bar = transform.GetChild(0).GetChild(0).GetComponent<RectTransform>();
        needle = transform.GetChild(1).GetComponent<RectTransform>();
        for(int i = 0; i < 100; i++)
        {
            energies[i] = transform.GetChild(2).GetChild(i).gameObject.GetComponent<RectTransform>();
            energies[i].gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (isPlaying)
        {
            if (isRight)
            {
                needlePos += Time.deltaTime * needleSpeed;
                if (needlePos > bar.rect.width / 2)
                {
                    isRight = false;
                    needlePos = bar.rect.width / 2;
                }
            }
            else
            {
                needlePos -= Time.deltaTime * needleSpeed;
                if (needlePos < (bar.rect.width / -2))
                {
                    isRight = true;
                    needlePos = bar.rect.width / -2;
                }
            }
            needle.anchoredPosition = new Vector2(needlePos, -67);

            if (Input.GetKeyDown(KeyCode.Space))
            {
                Judgment();
            }
        }
        else if(Input.GetKeyDown(KeyCode.R))
        {
            isPlaying = true;
        }
    }

    void Judgment()
    {
        if((-37.5 <= needlePos && -22.5 >= needlePos) || (22.5 <= needlePos && 37.5 >= needlePos)) //플러스 판정
        {
            PlusProduction();
        }
        else if(-10 <= needlePos && 10 >= needlePos) //종료 판정
        {
            isPlaying = false;
        }
        else //마이너스 판정
        {
            MinusProduction();
        }
    }

    void PlusProduction()
    {
        energies[soundPower].GetComponent<Image>().DOFade(1, 0);
        energies[soundPower].anchoredPosition = new Vector2(0,99);
        energies[soundPower].gameObject.SetActive(true);
        energies[soundPower].DOAnchorPos(new Vector2(0, soundPower), 0.3f);

        soundPower++;
        needleSpeed += 1f;
        if(soundPower == 100)
        {
            isPlaying=false;
        }
        combo++;
        Color color;
        ColorUtility.TryParseHtmlString("#00FFFF", out color);
        text.color = color;
        StopCoroutine("TextEffect");
        StartCoroutine("TextEffect", "Volume UP!");

        comboText.gameObject.SetActive(true);
        comboText.text = "Combo\n" + combo;
    }

    void MinusProduction()
    {
        combo = 0;
        for(int i = 0; i < 5; i++)
        {
            if (soundPower == 0) break;
            soundPower--;
            energies[soundPower].DOAnchorPos(energies[soundPower].anchoredPosition - new Vector2(50, 0), 0.2f);
            energies[soundPower].GetComponent<Image>().DOFade(0, 0.2f);
            needleSpeed -= 1f;
        }
        Color color;
        ColorUtility.TryParseHtmlString("#EA345C", out color);
        text.color = color;
        StopCoroutine("TextEffect");
        StartCoroutine("TextEffect", "Volume DOWN..");
        comboText.gameObject.SetActive(false);
    }

    IEnumerator TextEffect(string str)
    {
        text.text = str;
        text.transform.DOKill();
        text.transform.DOScale(Vector2.zero, 0);
        comboText.transform.DOScale(new Vector2(1, 1), 0);
        text.transform.DOScale(new Vector2(1, 1), 0.2f).SetEase(Ease.OutBack);
        comboText.transform.DOScale(new Vector2(1.5f, 1.5f), 0.1f).SetEase(Ease.OutSine);
        yield return new WaitForSeconds(0.1f);
        comboText.transform.DOScale(new Vector2(1, 1), 0.1f).SetEase(Ease.InSine);
        yield return new WaitForSeconds(0.3f);
        text.transform.DOScale(new Vector2(0, 0), 0.2f);
    }
}
