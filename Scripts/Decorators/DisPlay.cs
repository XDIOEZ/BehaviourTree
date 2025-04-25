using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;
using Unity.Collections;

public class DisPlay : DecoratorNode
{
    // 在检查器中显示当前状态（需添加InInspector属性）
    public State currentState;

    protected override void OnStart()
    {
        // 重置状态（可选）
        currentState = State.Running;
    }

    protected override void OnStop()
    {
        // 清理或重置状态（可选）
        currentState = State.Failure;
    }

    protected override State OnUpdate()
    {
        Debug.Log(currentState+"--"+child.description);
        currentState = child.Update();

        return currentState;
    }
}