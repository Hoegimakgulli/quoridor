using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AxisArray
{   
    [Header ("Total Index Value")]
    public List<Vector2> axis;

    [HideInInspector] [SerializeField] Define.EPlayerAxisField _playerField;
    [HideInInspector] [SerializeField] private AttackIndex _attackIndex;
    [HideInInspector] [SerializeField] private MoveIndex _moveIndex;
}

[SerializeField]
public class AttackIndex
{
    public bool canMultiAttack = false;
}

[SerializeField]
public class MoveIndex
{
    
}

public class AxisFrame : MonoBehaviour
{
    [SerializeField] private List<AxisArray> moveIndexs = new List<AxisArray>();
    [SerializeField] private List<AxisArray> attackIndexs = new List<AxisArray>();
    [SerializeField] private List<AxisArray> abilityIndexs = new List<AxisArray>();

    public List<Vector2> SelectFieldIndex(Define.EPlayerAxisField field = Define.EPlayerAxisField.None, int index = 0)
    {
        AxisArray currentField = null;
        switch (field)
        {
            case Define.EPlayerAxisField.Move:
                currentField = moveIndexs[index];
                break;
            case Define.EPlayerAxisField.Attack:
                currentField = attackIndexs[index];
                break;
            case Define.EPlayerAxisField.Ability:
                currentField = abilityIndexs[index];
                break;
            default:
                Debug.LogError("아무런 필드값을 받지 못했습니다. AxisFrame.cs");
                break;
        }

        return currentField.axis;
    }
}
