using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlopeSlider : MonoBehaviour
{
    bool isSloping = false;
    Vector3 touchPoint;
    float prevAngle;
    Vector3 touchOffset;
    RectTransform rt;

    public RectTransform slider;
    public RectTransform handle;

    float scope = 0.5f;
    int soundPower = 50;
    float speed = 0f;
    float acceleration = 2f; //가속도
    bool isRightScope = false;
    

    void Start()
    {
        slider = transform.GetChild(0).gameObject.GetComponent<RectTransform>();
        handle = transform.GetChild(0).GetChild(0).gameObject.GetComponent<RectTransform>();
        rt= GetComponent<RectTransform>(); 
    }

    void Update()
    {
        if(isSloping) 
        {
            Vector3 direction = GetMousePositionInCanvas() - touchPoint;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            rt.rotation = Quaternion.RotateTowards(rt.rotation, rotation, 360 *Time.deltaTime);


            speed += acceleration * Time.deltaTime;
            scope -= 2 * transform.rotation.z * Time.deltaTime * speed;
            if (scope < 0) scope = 0;
            else if (scope > 1) scope = 1;

            handle.sizeDelta = new Vector2(Mathf.Lerp(0, slider.rect.width, scope),10);

            if (isRightScope)
            {
                if(transform.rotation.z > 0)
                {
                    isRightScope = false;
                    speed = 0;
                }
            }
            else
            {
                if(transform.rotation.z < 0)
                {
                    isRightScope= true;
                    speed = 0;
                }
            }
        }
    }

    //마우스 위치를 캔버스 상의 위치로 변환
    private Vector3 GetMousePositionInCanvas()
    {
        Vector3 mousePos = Input.mousePosition;
        Vector2 uiPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mousePos, null, out uiPos);
        return uiPos;

    }

    public void Slope()
    {
        isSloping = true;
        touchPoint = GetMousePositionInCanvas();
        prevAngle = transform.rotation.eulerAngles.z;
        touchOffset = touchPoint - (Vector3)rt.anchoredPosition;
        speed = 0;
    }

    public void SlopeEnd()
    {
        isSloping = false;
        transform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.OutElastic);
    }
}
