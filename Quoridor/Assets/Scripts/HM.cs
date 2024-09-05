using HM.Containers;
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

        public static class PathFinding
        {
            public static List<Path> FinalPathList;
            public static List<Vector2> FinalVectorList;
            public static Vector2Int bottomLeft = new Vector2Int(0, 0), topRight = new Vector2Int(8, 8), startPos, targetPos;
            public static Vector2Int topLeft = new Vector2Int(0, 8), bottomRight = new Vector2Int(8, 0);
            public static bool allowDiagonal, dontCrossCorner;

            public static int sizeX, sizeY;
            public static Path[,] PathArray;
            public static Path StartNode, TargetNode, CurNode;
            public static List<Path> OpenList, ClosedList;

            private static GameObject blockEmemyObj;
            private static GameManager gameManager;
            private static GameObject enemyBox;

            // 함수 오버로딩 피함 Vector 매개변수 - GetAstarVector, Gameobject - GetAStarGame~
            public static List<Vector2> GetAStarVector(Vector2 startPathPos, Vector2 endPathPos)
            {
                gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
                enemyBox = GameObject.FindWithTag("EnemyBox");
                FinalVectorList = new List<Vector2>();
                sizeX = topRight.x - bottomLeft.x + 1;
                sizeY = topRight.y - bottomLeft.y + 1;
                PathArray = new Path[sizeX, sizeY];

                for (int i = 0; i < sizeX; i++)
                {
                    for (int j = 0; j < sizeY; j++)
                    {
                        PathArray[i, j] = new Path(i + bottomLeft.x, j + bottomLeft.y);
                    }
                }

                // startPos, endPos 기존 position을 girdSize로 나눠서 정수화 시켜준 다음 좌표계 (0, 0)을 왼쪽 아래 가장자리로 바꿔줌
                Vector2 trimPos;
                trimPos = startPathPos / GameManager.gridSize;
                startPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);
                trimPos = endPathPos / GameManager.gridSize;
                targetPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);

                StartNode = PathArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
                TargetNode = PathArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

                // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
                OpenList = new List<Path>() { StartNode };
                ClosedList = new List<Path>();
                FinalPathList = new List<Path>();

                while (OpenList.Count > 0)
                {
                    // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
                    CurNode = OpenList[0];
                    for (int i = 1; i < OpenList.Count; i++)
                    {
                        if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];
                    }

                    OpenList.Remove(CurNode);
                    ClosedList.Add(CurNode);


                    // 마지막
                    if (CurNode == TargetNode)
                    {
                        Path TargetCurNode = TargetNode.ParentNode;
                        while (TargetCurNode != StartNode)
                        {
                            FinalPathList.Add(TargetCurNode);
                            TargetCurNode = TargetCurNode.ParentNode;
                        }
                        FinalPathList.Add(StartNode);
                        FinalPathList.Reverse();

                        foreach(Path node in FinalPathList) FinalVectorList.Add(new Vector2(node.x, node.y));
                        return FinalVectorList;
                        //for (int i = 0; i < FinalPathList.Count; i++) print(i + "번째는 " + FinalPathList[i].x + ", " + FinalPathList[i].y);
                    }

                    void OpenListAdd(int checkX, int checkY)
                    {
                        // graph 상 (0,0) == 0과 같음
                        int startGraphPosition = (int)(CurNode.y * 9 + CurNode.x);
                        int endGraphPosition = (int)(checkY * 9 + checkX);

                        bool CheckEnemyPos(Vector2 currentPos)
                        {
                            foreach (Transform enemy in enemyBox.transform)
                            {
                                Vector2 enemyPos = enemy.position;
                                if (currentPos == enemyPos && currentPos != new Vector2((TargetNode.x - 4) * GameManager.gridSize, (TargetNode.y - 4) * GameManager.gridSize))
                                {
                                    blockEmemyObj = enemy.gameObject;
                                    return true;
                                }
                            }
                            return false;
                        }

                        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
                        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1 && !ClosedList.Contains(PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
                        {
                            // start 지점으로 부터 end 지점 사이에 벽이 있는지 확인
                            if (gameManager.wallData.mapGraph[startGraphPosition, endGraphPosition] == 0) return;
                            if (CheckEnemyPos(new Vector2((checkX - 4) * GameManager.gridSize, (checkY - 4) * GameManager.gridSize))) return;
                            // 대각선 허용시, 벽 사이로 통과 안됨
                            if (allowDiagonal)
                            {
                                if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
                                {
                                    if (checkY - CurNode.y == 1)
                                    {
                                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + 9] == 0) return; // 아래에서 위로 올라가는 경우
                                    }
                                    else if (checkY - CurNode.y == -1)
                                    {
                                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition - 9] == 0) return; // 위에서 아래로 내려가는 경우
                                    }
                                }
                            }

                            // 코너를 가로질러 가지 않을시, 이동 중에 수직수평 장애물이 있으면 안됨
                            if (dontCrossCorner)
                            {
                                //if (PathArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall || PathArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;
                            }

                            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
                            Path NeighborNode = PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
                            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);

                            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
                            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
                            {
                                NeighborNode.G = MoveCost;
                                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                                NeighborNode.ParentNode = CurNode;

                                OpenList.Add(NeighborNode);
                            }
                        }
                    }

                    if (allowDiagonal)
                    {
                        // 다음에 들어갈 좌표 전달
                        OpenListAdd(CurNode.x + 1, CurNode.y + 1); // 오른쪽 위
                        OpenListAdd(CurNode.x - 1, CurNode.y + 1); // 왼쪽 위
                        OpenListAdd(CurNode.x - 1, CurNode.y - 1); // 왼쪽 아래
                        OpenListAdd(CurNode.x + 1, CurNode.y - 1); // 오른쪽 아래
                    }

                    // ↑ → ↓ ←
                    OpenListAdd(CurNode.x, CurNode.y + 1); // 위
                    OpenListAdd(CurNode.x + 1, CurNode.y); // 오른쪽
                    OpenListAdd(CurNode.x, CurNode.y - 1); // 아래
                    OpenListAdd(CurNode.x - 1, CurNode.y); // 왼쪽
                }

                if (FinalPathList.Count == 0)
                {
                    GetAStarVector(startPathPos, blockEmemyObj.transform.position);
                }
                return null;
            }

            public static List<Vector2> GetAStarGameObject(GameObject startObj, GameObject endObj)
            {
                gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
                enemyBox = GameObject.FindWithTag("EnemyBox");
                FinalVectorList = new List<Vector2>();

                if (startObj.name.Contains("EnemyShieldSoldier")) // 만약 이동하는 객체가 방패병일 경우 벽처리로 해놨던 방패를 비활성화 후 이동 실시
                {
                    int currentShieldPos = Mathf.FloorToInt(startObj.transform.position.x / GameManager.gridSize) + 4 + ((Mathf.FloorToInt(startObj.transform.position.y / GameManager.gridSize) + 4) * 9); // mapgraph 형식으로 다듬기
                    if (currentShieldPos + 9 < 81 && startObj.GetComponent<Enemy>().ShieldTrue == true) // 방패가 위쪽 벽과 닿지 않았을 때만 실행
                    {
                        gameManager.wallData.mapGraph[currentShieldPos, currentShieldPos + 9] = 1; // 초기화 1
                        gameManager.wallData.mapGraph[currentShieldPos + 9, currentShieldPos] = 1; // 초기화 2
                        startObj.GetComponent<Enemy>().ShieldTrue = false;
                    }
                }

                sizeX = topRight.x - bottomLeft.x + 1;
                sizeY = topRight.y - bottomLeft.y + 1;
                PathArray = new Path[sizeX, sizeY];

                for (int i = 0; i < sizeX; i++)
                {
                    for (int j = 0; j < sizeY; j++)
                    {
                        PathArray[i, j] = new Path(i + bottomLeft.x, j + bottomLeft.y);
                    }
                }

                // startPos, endPos 기존 position을 girdSize로 나눠서 정수화 시켜준 다음 좌표계 (0, 0)을 왼쪽 아래 가장자리로 바꿔줌
                Vector2 trimPos;
                trimPos = startObj.transform.position / GameManager.gridSize;
                startPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);
                trimPos = endObj.transform.position / GameManager.gridSize;
                targetPos = new Vector2Int(Mathf.FloorToInt(trimPos.x) + 4, Mathf.FloorToInt(trimPos.y) + 4);

                StartNode = PathArray[startPos.x - bottomLeft.x, startPos.y - bottomLeft.y];
                TargetNode = PathArray[targetPos.x - bottomLeft.x, targetPos.y - bottomLeft.y];

                // 시작과 끝 노드, 열린리스트와 닫힌리스트, 마지막리스트 초기화
                OpenList = new List<Path>() { StartNode };
                ClosedList = new List<Path>();
                FinalPathList = new List<Path>();

                while (OpenList.Count > 0)
                {
                    // 열린리스트 중 가장 F가 작고 F가 같다면 H가 작은 걸 현재노드로 하고 열린리스트에서 닫힌리스트로 옮기기
                    CurNode = OpenList[0];
                    for (int i = 1; i < OpenList.Count; i++)
                    {
                        if (OpenList[i].F <= CurNode.F && OpenList[i].H < CurNode.H) CurNode = OpenList[i];
                    }

                    OpenList.Remove(CurNode);
                    ClosedList.Add(CurNode);


                    // 마지막
                    if (CurNode == TargetNode)
                    {
                        Path TargetCurNode = TargetNode.ParentNode;
                        while (TargetCurNode != StartNode)
                        {
                            FinalPathList.Add(TargetCurNode);
                            TargetCurNode = TargetCurNode.ParentNode;
                        }
                        FinalPathList.Add(StartNode);
                        FinalPathList.Reverse();

                        foreach (Path node in FinalPathList) FinalVectorList.Add(new Vector2(node.x, node.y));
                        return FinalVectorList;
                        //for (int i = 0; i < FinalPathList.Count; i++) print(i + "번째는 " + FinalPathList[i].x + ", " + FinalPathList[i].y);
                    }

                    void OpenListAdd(int checkX, int checkY)
                    {
                        // graph 상 (0,0) == 0과 같음
                        int startGraphPosition = (int)(CurNode.y * 9 + CurNode.x);
                        int endGraphPosition = (int)(checkY * 9 + checkX);

                        bool CheckEnemyPos(Vector2 currentPos)
                        {
                            foreach (Transform enemy in enemyBox.transform)
                            {
                                Vector2 enemyPos = enemy.position;
                                if (currentPos == enemyPos && currentPos != new Vector2((TargetNode.x - 4) * GameManager.gridSize, (TargetNode.y - 4) * GameManager.gridSize))
                                {
                                    blockEmemyObj = enemy.gameObject;
                                    return true;
                                }
                            }
                            return false;
                        }

                        // 상하좌우 범위를 벗어나지 않고, 벽이 아니면서, 닫힌리스트에 없다면
                        if (checkX >= bottomLeft.x && checkX < topRight.x + 1 && checkY >= bottomLeft.y && checkY < topRight.y + 1 && !ClosedList.Contains(PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y]))
                        {
                            // start 지점으로 부터 end 지점 사이에 벽이 있는지 확인
                            if (gameManager.wallData.mapGraph[startGraphPosition, endGraphPosition] == 0) return;
                            if (CheckEnemyPos(new Vector2((checkX - 4) * GameManager.gridSize, (checkY - 4) * GameManager.gridSize))) return;
                            // 대각선 허용시, 벽 사이로 통과 안됨
                            if (allowDiagonal)
                            {
                                if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + (checkX - CurNode.x)] == 0)
                                {
                                    if (checkY - CurNode.y == 1)
                                    {
                                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition + 9] == 0) return; // 아래에서 위로 올라가는 경우
                                    }
                                    else if (checkY - CurNode.y == -1)
                                    {
                                        if (gameManager.wallData.mapGraph[startGraphPosition, startGraphPosition - 9] == 0) return; // 위에서 아래로 내려가는 경우
                                    }
                                }
                            }

                            // 코너를 가로질러 가지 않을시, 이동 중에 수직수평 장애물이 있으면 안됨
                            if (dontCrossCorner)
                            {
                                //if (PathArray[CurNode.x - bottomLeft.x, checkY - bottomLeft.y].isWall || PathArray[checkX - bottomLeft.x, CurNode.y - bottomLeft.y].isWall) return;
                            }

                            // 이웃노드에 넣고, 직선은 10, 대각선은 14비용
                            Path NeighborNode = PathArray[checkX - bottomLeft.x, checkY - bottomLeft.y];
                            int MoveCost = CurNode.G + (CurNode.x - checkX == 0 || CurNode.y - checkY == 0 ? 10 : 14);

                            // 이동비용이 이웃노드G보다 작거나 또는 열린리스트에 이웃노드가 없다면 G, H, ParentNode를 설정 후 열린리스트에 추가
                            if (MoveCost < NeighborNode.G || !OpenList.Contains(NeighborNode))
                            {
                                NeighborNode.G = MoveCost;
                                NeighborNode.H = (Mathf.Abs(NeighborNode.x - TargetNode.x) + Mathf.Abs(NeighborNode.y - TargetNode.y)) * 10;
                                NeighborNode.ParentNode = CurNode;

                                OpenList.Add(NeighborNode);
                            }
                        }
                    }

                    if (allowDiagonal)
                    {
                        // 다음에 들어갈 좌표 전달
                        OpenListAdd(CurNode.x + 1, CurNode.y + 1); // 오른쪽 위
                        OpenListAdd(CurNode.x - 1, CurNode.y + 1); // 왼쪽 위
                        OpenListAdd(CurNode.x - 1, CurNode.y - 1); // 왼쪽 아래
                        OpenListAdd(CurNode.x + 1, CurNode.y - 1); // 오른쪽 아래
                    }

                    // ↑ → ↓ ←
                    OpenListAdd(CurNode.x, CurNode.y + 1); // 위
                    OpenListAdd(CurNode.x + 1, CurNode.y); // 오른쪽
                    OpenListAdd(CurNode.x, CurNode.y - 1); // 아래
                    OpenListAdd(CurNode.x - 1, CurNode.y); // 왼쪽
                }

                if (FinalPathList.Count == 0)
                {
                    GetAStarGameObject(startObj, blockEmemyObj);
                }
                return null;
            }
        }
    }
    namespace Containers
    {
        public class Define
        {
            public enum EField
            {
                None = -1,
                Player,
                Enemy,
            }
        }

        public class Path
        {
            // 중앙 좌표상 (0, 0) 시작으로 x, y 좌표
            // G = 시작으로부터 이동한 거리, H = 가로, 세로로 벽을 무시하고 Player까지 이동한 거리
            public int x, y, G, H;

            public Path(int _x, int _y)
            {
                x = _x;
                y = _y;
            }

            public Path ParentNode;

            // F = G, H 총 합산값
            public int F
            {
                get
                {
                    return G + H;
                }
            }
        }

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
