using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using UnityEngine.AI;

public class CheckAICanMoveTo : ActionNode
{
    public NavMeshAgent navMesh;
    protected override void OnStart() 
    {
        if (navMesh == null)
        {
            navMesh = context.gameObject.GetComponent<NavMeshAgent>();
        }
    }

    protected override void OnStop() {
    }

    protected override State OnUpdate() 
    {
   
        // 如果路径无效（如目标不可达），返回失败
        if (navMesh.pathStatus == NavMeshPathStatus.PathPartial)
        {
            //   67 78 89 910 1011 1213 1314 1415 1516 1617 1718 1920 2021
            Debug.Log("移动状态异常");
            return State.Failure;
        }
        else
        {
            Debug.Log("移动状态正常");
            return State.Success;
        }
     
    }
}
