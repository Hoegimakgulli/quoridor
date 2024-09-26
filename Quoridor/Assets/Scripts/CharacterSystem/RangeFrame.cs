using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterDefinition;

[System.Serializable]
public class RangeSetting
{   
    [Header ("Total Property Value")]
    public List<Vector2> range;
}

[SerializeField]
public class AttackProperty
{
    public bool canMultiAttack = false;
}

[SerializeField]
public class MoveProperty
{
    
}

public class RangeFrame : MonoBehaviour
{
    [SerializeField] private List<RangeSetting> moveProperties = new List<RangeSetting>();
    [SerializeField] private List<RangeSetting> attackProperties = new List<RangeSetting>();
    [SerializeField] private List<RangeSetting> abilityProperties = new List<RangeSetting>();

    public List<Vector2> SelectFieldProperty(EPlayerRangeField field = EPlayerRangeField.None, int Property = 0)
    {
        RangeSetting currentField = null;
        switch (field)
        {
            case EPlayerRangeField.Move:
                currentField = moveProperties[Property];
                break;
            case EPlayerRangeField.Attack:
                currentField = attackProperties[Property];
                break;
            case EPlayerRangeField.Ability:
                currentField = abilityProperties[Property];
                break;
            default:
                Debug.LogError("아무런 필드값을 받지 못했습니다. AxisFrame.cs");
                break;
        }

        return currentField.range;
    }
}
