using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CharacterDefinition;

[System.Serializable]
public class RangeSetting
{   
    [Header ("Total Property Value")]
    [SerializeField] public List<Vector2> range;
    [SerializeField] public EPlayerRangeField _playerField;
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
    [HideInInspector] [SerializeField] public List<RangeSetting> moveProperties;
    [HideInInspector] [SerializeField] public List<RangeSetting> attackProperties;
    [HideInInspector] [SerializeField] public List<RangeSetting> abilityProperties; 

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

    public (List<RangeSetting> move, List<RangeSetting> attack, List<RangeSetting> ability) GetList()
    {
        return (moveProperties, attackProperties, abilityProperties);
    }
}
