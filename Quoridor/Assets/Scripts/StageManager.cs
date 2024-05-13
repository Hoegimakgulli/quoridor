using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HM.Utils;
using UnityEditor;
using UnityEngine.SceneManagement;

public class StageManager : MonoBehaviour
{
    public static int currentStage = 1; // 현재 스테이지
    public static List<int> clearedStageID = new List<int>(); // 클리어한 스테이지 ID

    TouchUtil.ETouchState touchState = TouchUtil.ETouchState.None; // 터치 상태
    Vector2 touchPosition = Vector2.zero; // 터치 좌표
    Vector2 touchBeforePosition = Vector2.zero; // 이전 터치 좌표

    public int stageToChange = -1; // 바꿀 스테이지

    [SerializeField]
    StageBuildNumber stageBuildNumber; // 스테이지 빌드 넘버

    [System.Serializable]
    public struct StageBuildNumber
    {
        public int normalStage;
        public int eliteStage;
        public int restStage;
        public int shopStage;
        public int bossStage;
    }
    float zoomSpeed = 1000f; // 줌 속도
    private void Awake()
    {
        if (stageToChange != -1) currentStage = stageToChange;
        Debug.Log(currentStage);
    }
    private void Update()
    {
        touchBeforePosition = touchPosition;
        TouchUtil.TouchSetUp(ref touchState, ref touchPosition); // 터치 설정
        ChooseStage(); // 스테이지 선택

        Zoom(); // 줌
        MoveCamera(); // 카메라 이동
    }
    void Zoom()
    {
        // #if UNITY_ANDROID || UNITY_IOS
        if (Input.touchCount == 2)
        {
            Touch touchZero = Input.GetTouch(0);
            Touch touchOne = Input.GetTouch(1);

            Vector2 touchZeroPrevPos = touchZero.position - touchZero.deltaPosition;
            Vector2 touchOnePrevPos = touchOne.position - touchOne.deltaPosition;

            float prevTouchDeltaMag = (touchZeroPrevPos - touchOnePrevPos).magnitude;
            float touchDeltaMag = (touchZero.position - touchOne.position).magnitude;

            float deltaMagnitudeDiff = prevTouchDeltaMag - touchDeltaMag;

            Camera.main.orthographicSize += deltaMagnitudeDiff * 0.1f;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 10f, StageSelect.maxCameraSize);
        }
        // #else
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            Camera.main.orthographicSize -= scroll * zoomSpeed * Time.deltaTime;
            Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize, 10f, StageSelect.maxCameraSize);
        }
        // #endif
    }
    void MoveCamera()
    {
        if (touchState == TouchUtil.ETouchState.Moved)
        {
            Vector2 touchDeltaPosition = (Vector2)Camera.main.ScreenToWorldPoint(touchBeforePosition) - (Vector2)Camera.main.ScreenToWorldPoint(touchPosition);
            Camera.main.transform.Translate(touchDeltaPosition);
        }
    }
    void ChooseStage()
    {
        if (touchState == TouchUtil.ETouchState.Ended)
        {
            Vector2 pos = Camera.main.ScreenToWorldPoint(touchPosition);
            RaycastHit2D hit = Physics2D.Raycast(pos, Vector3.forward, 100f, LayerMask.GetMask("Stage"));
            if (hit)
            {
                StageSelect.Stage stage = hit.collider.GetComponent<StageComponent>().stageData;
                if (!stage.isEnable || stage.field != currentStage - 1) return;
                clearedStageID.Add(stage.id);
                switch (stage.stageType)
                {
                    case StageSelect.EStageType.Normal:
                        SceneManager.LoadScene(stageBuildNumber.normalStage);
                        break;
                    case StageSelect.EStageType.Elite:
                        SceneManager.LoadScene(stageBuildNumber.eliteStage);
                        break;
                    case StageSelect.EStageType.Rest:
                        SceneManager.LoadScene(stageBuildNumber.restStage);
                        break;
                    case StageSelect.EStageType.Shop:
                        SceneManager.LoadScene(stageBuildNumber.shopStage);
                        break;
                    case StageSelect.EStageType.Boss:
                        SceneManager.LoadScene(stageBuildNumber.bossStage);
                        break;
                    default:
                        Debug.LogError("Stage Type Error");
                        break;
                }
            }
        }
    }
}
