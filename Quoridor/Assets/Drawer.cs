using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using DG.Tweening;

public class Drawer : MonoBehaviour
{
    List<RectTransform> children = new List<RectTransform>(); //������� ��Ÿ���ų� ����� UI�� (�ڽĵ�)
    List<Vector2> childrenPos = new List<Vector2>(); //�ڽĵ��� ���� ��ġ
    RectTransform rt;
    Vector2 originPos; //�ڽ��� ���� ��ġ
    Vector2 originAnchor; //�ڽ��� ���� ��Ŀ
    Vector2 canvasSize;
    Sequence sequence;

    public bool isActive = false;
    public float drawTime = 0.5f;

    void Start()
    {
        //HideChildren(1, 0);
        rt = GetComponent<RectTransform>();
        isActive = false;
        originPos = rt.anchoredPosition;
        originAnchor = rt.anchorMax;
        ResetChildren();
        canvasSize = transform.root.GetComponent<RectTransform>().sizeDelta;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.D))
        {
            DrawPassiveChildren(1, drawTime);
        }
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            HideChildren(2);
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            HideChildren(1);
        }
        if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            HideChildren(3);
        }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            HideChildren(4);
        }
        if(Input.GetKeyDown (KeyCode.Q))
        {
            DrawActiveChildren(drawTime);
        }
            /*if (isActive == false)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    HideChildren(2, 0);
                    DrawChildren(drawTime);
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    HideChildren(1, 0);
                    DrawChildren(drawTime);
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    HideChildren(3, 0);
                    DrawChildren(drawTime);
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    HideChildren(4, 0);
                    DrawChildren(drawTime);
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    //isActive = false;
                    HideChildren(2, drawTime);
                    isActive = false;
                }
                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    //isActive = false;
                    HideChildren(1, drawTime);
                    isActive = false;
                }
                if (Input.GetKeyDown(KeyCode.DownArrow))
                {
                    //isActive = false;
                    HideChildren(3, drawTime);
                    isActive = false;
                }
                if (Input.GetKeyDown(KeyCode.UpArrow))
                {
                    //isActive = false;
                    HideChildren(4, drawTime);
                    isActive = false;
                }
            }*/

            //if(Input.GetKeyDown(KeyCode.D)) { }
        }

    //Draw ȿ���� ���� UI�� �߰��Ǿ��� �� ����
    void ResetChildren()
    {
        children.Clear();
        for(int i = 0; i < transform.childCount; i++)
        {
            children.Add(transform.GetChild(i).GetComponent<RectTransform>());
            childrenPos.Add(children[i].anchoredPosition);
        }
    }
    
    //UI���� ��� ���� (Draw �� ȣ��. 1:��  2:��  3:��  4:��)
    void HideChildren(int direction)
    {
        switch(direction)
        {
            case 1:
                SetAnchors(new Vector2(1, originAnchor.y));
                rt.anchoredPosition = new Vector2(0, originPos.y);
                for(int i = 0; i < children.Count; i++)
                {
                    children[i].anchoredPosition = new Vector2(children[i].rect.width, childrenPos[i].y);
                }
                break;
            case 2:
                SetAnchors(new Vector2(0, originAnchor.y));
                rt.anchoredPosition = new Vector2(0, originPos.y);
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].anchoredPosition = new Vector2(-children[i].rect.width, childrenPos[i].y);
                }
                break;
            case 3:
                SetAnchors(new Vector2(originAnchor.x, 0));
                rt.anchoredPosition = new Vector2(originPos.x, 0);
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].anchoredPosition = new Vector2(childrenPos[i].x, -children[i].rect.height);
                }
                break;
            case 4:
                SetAnchors(new Vector2(originAnchor.x, 1));
                rt.anchoredPosition = new Vector2(originPos.x, 0);
                for (int i = 0; i < children.Count; i++)
                {
                    children[i].anchoredPosition = new Vector2(childrenPos[i].x, children[i].rect.height);
                }
                break;
        }
    }

    //draw�� �ڽĵ��� Ȱ��ȭ��Ű�� �Լ�.
    void DrawActiveChildren(float time)
    {
        isActive = true;
        Vector2 a = new Vector2((rt.anchorMin.x - originAnchor.x) * canvasSize.x, (rt.anchorMin.y - originAnchor.y) * canvasSize.y);
        for (int i = 0; i < children.Count; i++)
        {
            if (rt.anchoredPosition.x == 0)
                children[i].anchoredPosition = a + new Vector2(childrenPos[i].x, childrenPos[i].y);
            else
                children[i].anchoredPosition = a + new Vector2(childrenPos[i].x, childrenPos[i].y);
        
            //children[i].anchoredPosition = a;
        }
        SetAnchors(originAnchor);
        rt.anchoredPosition = originPos;

        sequence.Kill();
        sequence = DOTween.Sequence();
        sequence.Append(children[0].DOAnchorPos(childrenPos[0], time));
        for (int i = 1; i < children.Count; i++)
        {
            sequence.Insert(time * i / children.Count, children[i].DOAnchorPos(childrenPos[i], time));
        }
    }

    //draw�� �ڽĵ��� ��Ȱ��ȭ��Ű�� �Լ�.   1:��  2:��  3:��  4:��
    void DrawPassiveChildren(int direction, float time)
    {
        switch (direction)
        {
            case 1:
                SetAnchors(new Vector2(1, originAnchor.y));
                FixationChildren();
                rt.anchoredPosition = new Vector2(0, originPos.y);

                sequence.Kill();
                sequence = DOTween.Sequence();
                sequence.Append(children[0].DOAnchorPos(new Vector2(children[0].rect.width, childrenPos[0].y), time));
                for (int i = 1; i < children.Count; i++)
                {
                    sequence.Insert(time * i / children.Count, children[i].DOAnchorPos(new Vector2(children[i].rect.width, childrenPos[i].y), time));
                }
            break;
            case 2:
                break;
            case 3:
                break;
            case 4:
                break;
        }
    }

    //��Ŀ ����
    void SetAnchors(Vector2 anchor)
    {
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
    }

    //��Ŀ �ٲ� �ڽ� ��ġ ����
    void FixationChildren()
    {
        Vector2 fixationVec = new Vector2((-rt.anchorMin.x + originAnchor.x) * canvasSize.x, (-rt.anchorMin.y + originAnchor.y) * canvasSize.y);
        for (int i = 0; i < children.Count; i++)
        {
            /*if (rt.anchoredPosition.x == originPos.x)
            {
                child.anchoredPosition += fixationVec - new Vector2(0, rt.anchoredPosition.y);
                Debug.Log("dd");
            }
            else
            {
                child.anchoredPosition += fixationVec - new Vector2(rt.anchoredPosition.x, 0);
                Debug.Log("ss");
            }*/

            children[i].anchoredPosition = childrenPos[i] + fixationVec;
        }
    }
}
