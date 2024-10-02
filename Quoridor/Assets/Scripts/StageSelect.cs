using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class StageSelect : MonoBehaviour
{
    public enum EStageType { Normal, Elite, Rest, Shop, Boss }
    [System.Serializable]
    public class Stage
    {
        public EStageType stageType;
        public int field;
        public int id;
        public bool isEnable;
        public GameObject stageObject;
    }
    public int chapter;
    int fieldCount;

    public static int seed = 0;

    [System.Serializable]
    public class StageTypeProbability
    {
        public int normalStage;
        public int eliteStage;
        public int restStage;
        public int shopStage;
        public int totalCount { get { return normalStage + eliteStage + restStage + shopStage; } }
        public EStageType GetRandomStageType()
        {
            int random = Random.Range(0, totalCount);
            if (random < normalStage) return EStageType.Normal;
            else if (random - normalStage < eliteStage) return EStageType.Elite;
            else if (random - normalStage - eliteStage < restStage) return EStageType.Rest;
            else return EStageType.Shop;
        }
    }
    [SerializeField]
    StageTypeProbability stageTypeProbability;

    [SerializeField]
    GameObject stagePrefab;
    [SerializeField]
    List<Sprite> stageImageList;
    [SerializeField]
    GameObject linePrefab;

    float xInterval = 10;
    float yInterval = 10;

    public List<List<Stage>> stageList = new List<List<Stage>>();

    public static float maxCameraSize;
    public static Vector2 mapCenterPosition;

    int GetIndex(int i)
    {
        return i * i + 2 * i;
    }
    private void Start()
    {
        if (StageManager.currentStage == 0)
        {
            seed = (int)System.DateTime.Now.Ticks + System.DateTime.Now.Millisecond;
            StageManager.currentStage = 1;
            StageManager.clearedStageID.Clear();
        }
        Random.InitState(seed);
        Debug.Log(seed);
        foreach (var stageId in StageManager.clearedStageID)
        {
            Debug.Log(stageId);
        }

        CreateStageList();
        InstantiateStage();
        SetCamera();
        DrawTree();
    }
    void CreateStageList()
    {
        fieldCount = 15 + chapter;
        List<int> leftMidBottomIndex = GetLeftMidBottomIndex(fieldCount);
        for (int index = 0; index < fieldCount; index++)
        {
            int tileCount = 2 * index + 3;
            int stageFullCount = GetFullCount(stageList);
            for (int j = 0; j < tileCount; j++)
            {
                Stage stage = new Stage();

                stage.field = index;
                if (index == fieldCount - 1) stage.stageType = EStageType.Boss;
                else if (index == fieldCount - 2) stage.stageType = EStageType.Rest;
                else stage.stageType = index < 2 ? EStageType.Normal : stageTypeProbability.GetRandomStageType();

                stage.id = stageFullCount + j;
                if (stageList.Count() <= index) stageList.Add(new List<Stage>());
                if (index == 0) stage.isEnable = true;
                else
                {
                    int idWithoutIndex = stage.id - stageList[index - 1].Last().id - 1;
                    List<int> parentIdList = new List<int>();
                    if (idWithoutIndex > 0 && idWithoutIndex < stageList[index - 1].Count) parentIdList.Add(idWithoutIndex);
                    if (idWithoutIndex > 1 && idWithoutIndex < stageList[index - 1].Count + 1) parentIdList.Add(idWithoutIndex - 1);
                    if (idWithoutIndex > 2 && idWithoutIndex < stageList[index - 1].Count + 2) parentIdList.Add(idWithoutIndex - 2);
                    if (StageManager.clearedStageID.Intersect(parentIdList.Select(id => id + stageList[index - 1][0].id)).Count() > 0)
                        stage.isEnable = true;
                }
                stageList[index].Add(stage);
            }
        }
        int stageCount = stageList.Count();
    }
    public void InstantiateStage()
    {
        foreach (var stageSet in stageList)
        {
            foreach (var stage in stageSet)
            {
                GameObject stageObject = Instantiate(stagePrefab, new Vector3((stage.id - GetIndex(stage.field) - (stageSet.Count() / 2 - 1)) * xInterval, stage.field * yInterval, 0), Quaternion.identity);
                stage.stageObject = stageObject;
                stageObject.GetComponent<StageComponent>().stageData = stage;
                stageObject.GetComponent<SpriteRenderer>().sprite = stageImageList[(int)stage.stageType];
                stageObject.transform.GetChild(1).gameObject.SetActive(StageManager.clearedStageID.Contains(stage.id));
                if (!stage.isEnable)
                {
                    stageObject.GetComponent<SpriteRenderer>().color = new Color(0.5f, 0.5f, 0.5f, 0.2f);
                }
                if (stage.stageType != EStageType.Boss)
                {
                    for (int i = -1; i < 2; i++)
                    {
                        Vector2 direction = new Vector2(i * xInterval, yInterval);
                        GameObject lineObject = Instantiate(linePrefab, stageObject.transform);
                        lineObject.transform.localPosition = new Vector3(0, 0, 1);
                        lineObject.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
                        lineObject.transform.localScale = new Vector3(direction.magnitude, 1, 1);
                    }
                }
            }
        }
    }
    public void SetCamera()
    {
        mapCenterPosition = new Vector2(xInterval, (fieldCount - 1) * yInterval / 2);
        maxCameraSize = (fieldCount - 1) * yInterval / 2 + 5;
        Camera.main.transform.Translate(stageList[StageManager.currentStage - 1][stageList[StageManager.currentStage - 1].Count / 2].stageObject.transform.position);
    }
    public void DrawTree()
    {
        string tree = "";

        foreach (var stageSet in stageList)
        {
            string stageString = "";
            foreach (var stage in stageSet)
            {
                stageString += $"[{stage.stageType}]{stage.id}";
                stageString += ",        ";
            }
            tree += stageString + '\n';
        }
        Debug.Log(tree);
    }
    static int GetFullCount<T>(List<List<T>> list)
    {
        int count = 0;
        foreach (var set in list)
        {
            count += set.Count();
        }
        return count;
    }
    List<int> GetLeftMidBottomIndex(int maxIndex)
    {
        List<int> indexList = new List<int>();
        int index = 0;
        while (maxIndex > 0)
        {
            maxIndex -= 2 * index + 3;
            index++;
        }
        for (int i = 0; i < index; i++)
        {
            indexList.Add(i * (i + 2));
            indexList.Add((i + 1) * (i + 2) - 1);
            indexList.Add((i + 2) * (i + 2) - 2);
        }
        return indexList;
    }
}
