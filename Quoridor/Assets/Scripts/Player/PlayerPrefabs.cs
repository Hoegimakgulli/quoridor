using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrefabs : MonoBehaviour
{
    public GameObject playerPreview; // 플레이어 위치 미리보기
    public GameObject attackPreview;
    public GameObject attackHighlight;
    public GameObject wallPreview; // 플레이어 설치벽 위치 미리보기
    public GameObject wall; // 플레이어 설치벽
    public GameObject actionUI;
    public GameObject rangeFrame; // 기물 이동 범위 및 공격 범위 담아두는 오브젝트
}
