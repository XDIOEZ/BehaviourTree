using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

public class CustomRepeat : DecoratorNode
{

    [Tooltip("执行间隔时间（秒）")]
    public float repeatInterval = 0.2f;

    [Tooltip("是否立即执行一次")]
    public bool startImmediately = true;

    [Tooltip("子节点成功后是否重启")]
    public bool restartOnSuccess = true;

    [Tooltip("子节点失败后是否重启")]
    public bool restartOnFailure = false;

    private float lastExecutionTime;

    protected override void OnStart()
    {
        if (startImmediately)
        {
            lastExecutionTime = Time.time - repeatInterval;
        }
        else
        {
            lastExecutionTime = Time.time;
        }
    }

    protected override void OnStop() { }

    protected override State OnUpdate()
    {
        if (Time.time - lastExecutionTime > repeatInterval)
        {

            lastExecutionTime = Time.time;

            State childState = child.Update();

            // ✅ 打印调试信息
      //      Debug.Log($"[CustomRepeat]");

            switch (childState)
            {
                case State.Running:
                    return State.Running;
                case State.Failure:
                    return restartOnFailure ? State.Running : State.Failure;
                case State.Success:
                    return restartOnSuccess ? State.Running : State.Success;
            }
        }

        return State.Running;
    }
}
