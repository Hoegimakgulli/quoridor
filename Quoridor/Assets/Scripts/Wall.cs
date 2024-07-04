using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WallData
{
    public int[,] mapGraph = new int[81, 81]; //DFS용 맵 그래프
    public List<GameObject> wallObjectList = new List<GameObject>(); // 설치된 벽 오브젝트 리스트
    private List<int[]> unavailableWallInfoList = new List<int[]>(); // 설치할 수 없는 벽 정보 리스트
    public void Reset()
    {
        wallObjectList.Clear();
        mapGraph = new int[81, 81];
        for (int i = 0; i < mapGraph.GetLength(0); i++) // 맵 그래프 초기화
        {
            int row = i / 9;
            int col = i % 9;
            if (row > 0)
            {
                mapGraph[i, (row - 1) * 9 + col] = 1;
            }
            if (row < 8)
            {
                mapGraph[i, (row + 1) * 9 + col] = 1;
            }
            if (col > 0)
            {
                mapGraph[i, row * 9 + (col - 1)] = 1;
            }
            if (col < 8)
            {
                mapGraph[i, row * 9 + (col + 1)] = 1;
            }
        }
    }
    //[디버그용] 맵그래프 출력
    public void PrintMap()
    {
        string log = "";
        for (int i = 0; i < mapGraph.GetLength(0); i++)
        {
            for (int row = 8; row >= 0; row--)
            {
                string rowInfo = "";
                for (int col = 0; col < 9; col++)
                {
                    rowInfo = rowInfo + " " + mapGraph[i, row * 9 + col].ToString();
                    if (mapGraph[i, row * 9 + col] == 1)
                    {
                        Vector3 start = new Vector3((i % 9) - 4, (i / 9) - 4, 0) * GameManager.gridSize;
                        Vector3 end = new Vector3(col - 4, row - 4, 0) * GameManager.gridSize;
                        Vector3 dir = end - start;
                        Vector3 interval = (i % 2 == 0) ? Vector3.zero : new Vector3(0.1f, 0.1f, 0);
                        Debug.DrawRay(start + interval, dir.normalized, Color.green, 1f);
                    }
                }
                log += rowInfo + '\n';
            }
            log += '\n';
        }
        Debug.Log(log);
    }
    //DFS 알고리즘을 이용한 벽에 갇혀있는지 체크 (true 면 갇혀있음, false 면 갇혀있지 않음)
    public bool CheckStuck(List<Vector2Int> playerGridPositionList, List<Vector2Int> enemyGridPositionList, int[,] mapGraph = null)
    {
        if (mapGraph == null)
        {
            mapGraph = this.mapGraph;
        }
        foreach (var playerPosition in playerGridPositionList)
        {
            bool[] visited = new bool[81];
            int playerGraphPosition = (int)((playerPosition.y + 4) * 9 + playerPosition.x + 4);

            void DFS(int now)
            {
                visited[now] = true;
                for (int next = 0; next < 81; next++)
                {
                    if (mapGraph[now, next] == 0)
                        continue;
                    if (visited[next])
                        continue;
                    DFS(next);
                }
            }
            DFS(playerGraphPosition);
            // Debug.Log(visited[enemyGraphPosition]);
            foreach (Vector2Int enemyPosition in enemyGridPositionList)
            {
                int enemyGraphPosition = (int)((enemyPosition.y + 4) * 9 + enemyPosition.x + 4);
                if (!visited[enemyGraphPosition]) return true;
            }
        }
        return false;
    }
    public void SetWallPreview(int x, int y, int rotation, ref GameObject previewObject)
    {
        if (previewObject == null)
        {
            throw new ArgumentNullException("previewObject is null");
        }
        bool? resultOrNull = CanSetWall(x, y, rotation, true);
        if (resultOrNull == null)
        {
            previewObject.SetActive(false);
            return;
        }
        bool result = (bool)resultOrNull;
        previewObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0) * GameManager.gridSize;
        previewObject.transform.rotation = Quaternion.Euler(0, 0, rotation * 90);
        previewObject.SetActive(true);
        if (previewObject.TryGetComponent<PreviewWall>(out var previewWall))
        {
            previewWall.isBlock = !result;
        }
        else
        {
            throw new ArgumentException("previewObject is not a PreviewWall");
        }
    }
    public void SetWallBasedPreview(GameObject previewObject, ref GameObject wallObject)
    {
        if (previewObject == null)
        {
            throw new ArgumentNullException("previewObject is null");
        }
        if (wallObject == null)
        {
            throw new ArgumentNullException("wallObject is null");
        }
        if (previewObject.activeSelf)
        {
            Vector2Int pos = GameManager.ChangeCoord(previewObject.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize);
            int rotation = (int)((previewObject.transform.rotation.eulerAngles.z / 90) % 2);
            Debug.Log($"pos: {pos}, rotation: {rotation}");
            SetWall(pos.x, pos.y, rotation, ref wallObject);
        }
        else
        {
            wallObject.SetActive(false);
        }
    }
    public void SetWall(int x, int y, int rotation, ref GameObject wallObject)
    {
        if (wallObject == null)
        {
            throw new ArgumentNullException("wallObject is null");
        }
        bool? resultOrNull = CanSetWall(x, y, rotation);
        if (resultOrNull == null)
        {
            throw new ArgumentOutOfRangeException("좌표가 범위를 벗어났습니다.");
        }
        bool result = (bool)resultOrNull;
        if (result)
        {
            wallObject.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0) * GameManager.gridSize;
            wallObject.transform.rotation = Quaternion.Euler(0, 0, rotation * 90);
            wallObject.SetActive(true);
            wallObjectList.Add(wallObject);

            unavailableWallInfoList.Add(new int[] { x, y, rotation });
            unavailableWallInfoList.Add(new int[] { x, y, (rotation + 1) % 2 });
            if (rotation == 0)
            {
                unavailableWallInfoList.Add(new int[] { x, y + 1, rotation });
                unavailableWallInfoList.Add(new int[] { x, y - 1, rotation });
            }
            else
            {
                unavailableWallInfoList.Add(new int[] { x + 1, y, rotation });
                unavailableWallInfoList.Add(new int[] { x - 1, y, rotation });
            }
        }
        else
        {
            throw new ArgumentException("설치할 수 없는 벽 구조입니다.");
        }
    }
    public void RemoveWall(ref GameObject wallObject)
    {
        if (wallObject == null)
        {
            throw new ArgumentNullException("wallObject is null");
        }
        Vector2Int pos = GameManager.ChangeCoord(wallObject.transform.position - new Vector3(0.5f, 0.5f, 0) * GameManager.gridSize);
        int rotation = (int)((wallObject.transform.rotation.eulerAngles.z / 90) % 2);
        wallObjectList.Remove(wallObject);
        unavailableWallInfoList.Remove(new int[] { pos.x, pos.y, rotation });
        unavailableWallInfoList.Remove(new int[] { pos.x, pos.y, (rotation + 1) % 2 });
        if (rotation == 0)
        {
            unavailableWallInfoList.Remove(new int[] { pos.x, pos.y + 1, rotation });
            unavailableWallInfoList.Remove(new int[] { pos.x, pos.y - 1, rotation });
        }
        else
        {
            unavailableWallInfoList.Remove(new int[] { pos.x + 1, pos.y, rotation });
            unavailableWallInfoList.Remove(new int[] { pos.x - 1, pos.y, rotation });
        }
        if (pos.x < -4 || pos.x > 3 || pos.y < -4 || pos.y > 3)
        {
            throw new ArgumentOutOfRangeException("좌표가 범위를 벗어났습니다.");
        }
        if (rotation == 0) // 세로 벽이면
        {
            int wallGraphPosition = (pos.y + 4) * 9 + pos.x + 4; // 벽좌표를 그래프좌표로 변환
                                                                 // 벽 넘어로 못넘어가게 그래프에서 설정
            mapGraph[wallGraphPosition, wallGraphPosition + 1] = 1;
            mapGraph[wallGraphPosition + 1, wallGraphPosition] = 1;
            mapGraph[wallGraphPosition + 9, wallGraphPosition + 10] = 1;
            mapGraph[wallGraphPosition + 10, wallGraphPosition + 9] = 1;
        }
        if (rotation == 1) // 가로 벽이면
        {
            int wallGraphPosition = (pos.y + 4) * 9 + pos.x + 4;// 벽좌표를 그래프좌표로 변환
                                                                // 벽 넘어로 못넘어가게 그래프에서 설정
            mapGraph[wallGraphPosition, wallGraphPosition + 9] = 1;
            mapGraph[wallGraphPosition + 9, wallGraphPosition] = 1;
            mapGraph[wallGraphPosition + 1, wallGraphPosition + 10] = 1;
            mapGraph[wallGraphPosition + 10, wallGraphPosition + 1] = 1;
        }
    }
    public bool? CanSetWall(int x, int y, int rotation, bool isFake = false)
    {
        if (x < -4 || x > 3 || y < -4 || y > 3)
        {
            return null;
        }
        if (unavailableWallInfoList.Any(wallInfo => wallInfo.SequenceEqual(new int[] { x, y, rotation })))
        {
            return false;
        }
        int[,] tempMapGraph = (int[,])mapGraph.Clone();
        if (rotation == 0) // 세로 벽이면
        {
            int wallGraphPosition = (y + 4) * 9 + x + 4; // 벽좌표를 그래프좌표로 변환
                                                         // 벽 넘어로 못넘어가게 그래프에서 설정
            tempMapGraph[wallGraphPosition, wallGraphPosition + 1] = 0;
            tempMapGraph[wallGraphPosition + 1, wallGraphPosition] = 0;
            tempMapGraph[wallGraphPosition + 9, wallGraphPosition + 10] = 0;
            tempMapGraph[wallGraphPosition + 10, wallGraphPosition + 9] = 0;
        }
        if (rotation == 1) // 가로 벽이면
        {
            int wallGraphPosition = (y + 4) * 9 + x + 4;// 벽좌표를 그래프좌표로 변환
                                                        // 벽 넘어로 못넘어가게 그래프에서 설정
            tempMapGraph[wallGraphPosition, wallGraphPosition + 9] = 0;
            tempMapGraph[wallGraphPosition + 9, wallGraphPosition] = 0;
            tempMapGraph[wallGraphPosition + 1, wallGraphPosition + 10] = 0;
            tempMapGraph[wallGraphPosition + 10, wallGraphPosition + 1] = 0;
        }
        if (!CheckStuck(GameManager.playerGridPositionList, GameManager.enemyValueList.Select(e => GameManager.ChangeCoord(e.position)).ToList(), tempMapGraph))
        {
            if (!isFake) mapGraph = tempMapGraph.Clone() as int[,];
            return true;
        }
        return false;
    }
}