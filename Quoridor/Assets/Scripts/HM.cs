using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace HM
{
    namespace Utils
    {
        public static class TouchUtil
        {
            public enum ETouchState { None, Began, Moved, Ended }; //모바일과 에디터 모두 가능하게 터치 & 마우스 처리
            // 모바일 or 에디터 마우스 터치좌표 처리
            public static void TouchSetUp(ref ETouchState touchState, ref Vector2 touchPosition)
            {
#if UNITY_EDITOR
                if (Input.GetMouseButtonDown(0))
                {
                    if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Began; }
                }
                else if (Input.GetMouseButton(0)) { if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Moved; } }
                else if (Input.GetMouseButtonUp(0)) { if (EventSystem.current.IsPointerOverGameObject() == false) { touchState = ETouchState.Ended; } }
                else touchState = ETouchState.None;
                touchPosition = Input.mousePosition;
#else
                if (Input.touchCount > 0)
                {

                    Touch touch = Input.GetTouch(0);
                    if (EventSystem.current.IsPointerOverGameObject(touch.fingerId) == true) return;
                    if (touch.phase == TouchPhase.Began) touchState = ETouchState.Began;
                    else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) touchState = ETouchState.Moved;
                    else if (touch.phase == TouchPhase.Ended) if (touchState != ETouchState.None) touchState = ETouchState.Ended;
                    touchPosition = touch.position;
                }
                else touchState = ETouchState.None;
#endif
            }
        }
        public static class MathUtil
        {
            public static float GetTaxiDistance(Vector2Int start, Vector2Int end)
            {
                return Mathf.Abs(start.x - end.x) + Mathf.Abs(start.y - end.y);
            }
        }
    }
    namespace Containers
    {
        public class EnemyValues
        {
            private Vector3 mPosition; // position
            private int mMoveCtrl; // moveCtrl

            public int hp; // 유닛 hp
            public int maxHp; // 유닛 최대 hp
            public int attack;
            public int damageResistance;
            public int moveCtrl
            {
                get
                {
                    return mMoveCtrl;
                }

                set
                {
                    // Debug.Log($"SetPreMoveCtrl : {index}: {value}");
                    value = Mathf.Max(value, 0);
                    // Debug.Log($"SetMoveCtrl : {index}: {value}");
                    Enemy correctEnemy = EnemyManager.GetEnemy(mPosition);
                    correctEnemy.moveCtrl[1] = value;
                    mMoveCtrl = value;
                }
            }
            public int maxMoveCtrl; // 유닛이 가질 수 있는 최대 행동력
            public int uniqueNum; // 어떤 유닛을 생성할지 정하는 번호
            public int index; // 생성 순서, EnemyBox 내 Index
            public Vector3 position // position이 변경될때 일어나는 것
            {
                get
                {
                    return mPosition;
                }

                set
                {
                    GameObject enemyBox = GameObject.FindWithTag("EnemyBox");
                    // enemyBox.transform.GetChild(spawnNum).position = value;
                    // mPosition = value;
                    foreach (Transform enemyPos in enemyBox.transform)
                    {
                        Debug.Log($"EV: {value}");
                        if (enemyPos.position == mPosition) // 만약 
                        {
                            enemyPos.position = value;
                            mPosition = value;
                        }
                    }
                }
            }

            public EnemyValues(int hp, int moveCtrl, int uniqueNum, int index, Vector3 position)
            {
                this.hp = hp;
                mMoveCtrl = moveCtrl;
                this.uniqueNum = uniqueNum;
                this.index = index;
                mPosition = position;
            }
        }

        public class PlayerValues
        {
            public GameObject player; // 해당 게임 오브젝트
            public int hp; // 체력 최소값 10, 최댓값 100
            public int attack; // 최소값 1, 최댓값 50
            public int damageResistance; // 0%, 51%
            public int index; // 해당 캐릭터 고유번호

            public int mMoveCtrl; // 행동력
            public int moveCtrl // 프로퍼티
            {
                get
                {
                    return mMoveCtrl;
                }

                set
                {

                }
            }

            public Vector3 mPosition; // 해당 위치
            public Vector3 position
            {
                get
                {
                    return mPosition;
                }

                set
                {
                    player.transform.position = position;
                    mPosition = value;
                }
            }

            public PlayerValues(int hp, int moveCtrl, int index, Vector3 position)
            {
                this.hp = hp;
                mMoveCtrl = moveCtrl;
                this.index = index;
                mPosition = position;
            }
        }
    }
    namespace Physics
    {
        public static class HMPhysics
        {
            public static bool[] CheckRay(Vector3 start, Vector2 direction) // return [isOuterWall, canSetPreview, 적과의 충돌이 있는지]
            {
                RaycastHit2D outerWallHit = Physics2D.Raycast(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("OuterWall")); // 외벽에 의해 완전히 막힘
                RaycastHit2D wallHit = Physics2D.Raycast(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("Wall")); // 벽에 의해 완전히 막힘
                RaycastHit2D[] semiWallHit = Physics2D.RaycastAll(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("SemiWall")); // 벽에 의해 "반" 막힘
                RaycastHit2D[] tokenHit = Physics2D.RaycastAll(start, direction.normalized, GameManager.gridSize * direction.magnitude, LayerMask.GetMask("Token")).OrderBy(h => h.distance).ToArray(); // 적에 의해 완전히 막힘

                bool fullBlock = false;
                // Debug.Log($"{(bool)tokenHit} - {(tokenHit ? tokenHit.collider.gameObject.name : i)}");
                if (outerWallHit)
                {
                    return new bool[] { true, false, tokenHit.Length > 1 };
                }
                if (!wallHit)
                { // 벽에 의해 완전히 막히지 않았고
                    for (int j = 0; j < semiWallHit.Length; j++)
                    { // 반벽이 2개가 겹쳐있을 경우에
                        for (int k = j + 1; k < semiWallHit.Length; k++)
                        {
                            float wallDistance = Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance);
                            if (wallDistance > 0.1f) continue;
                            if (semiWallHit[j].transform.rotation == semiWallHit[k].transform.rotation || Mathf.Abs(semiWallHit[j].distance - semiWallHit[k].distance) < 0.000001f)
                            {
                                fullBlock = true; // 완전 막힘으로 처리
                                break;
                            }
                        }
                        if (fullBlock) break;
                    }
                    if (!fullBlock)
                    { // 완전 막히지 않았고 적이 공격 범주에 있다면 공격한다.
                        return new bool[] { false, true, tokenHit.Length > 1 };
                    }
                }
                return new bool[] { false, false, tokenHit.Length > 1 };
            }
        }
    }
}
