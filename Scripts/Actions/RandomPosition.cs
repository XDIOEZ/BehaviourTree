using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class RandomPosition : ActionNode
{
    public WorldType worldType;
    public Vector2 min = Vector2.one * -10;
    public Vector2 max = Vector2.one * 10;

    public enum WorldType 
    {
       DDD,
       DD
    }

    protected override void OnStart() 
    {
    }

    protected override void OnStop() {
    }

    protected override State OnUpdate() 
    {
        if (worldType == WorldType.DDD)
        {
            blackboard.TargetPosition.x = Random.Range(min.x, max.x);
            blackboard.TargetPosition.z = Random.Range(min.y, max.y);
            blackboard.TargetPosition = context.transform.position + blackboard.TargetPosition;
        }
        else if (worldType == WorldType.DD)
        {
           
            blackboard.TargetPosition.x = Random.Range(min.x, max.x);
            blackboard.TargetPosition.y = Random.Range(min.y, max.y);
            blackboard.TargetPosition = context.transform.position + blackboard.TargetPosition;
        }
    
        return State.Success;

    }
}
