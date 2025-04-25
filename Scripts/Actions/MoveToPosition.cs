using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

/// <summary>
/// 行为树动作节点：控制角色移动到指定目标位置
/// </summary>
public class MoveToPosition : ActionNode
{
    // 移动速度（单位：单位/秒）
    public float speed = 5;

    // 停止距离阈值，当接近目标时提前停止
    public float stoppingDistance = 0.1f;

    // 是否在移动过程中更新角色朝向
    public bool updateRotation = true;

    // 加速度参数，控制移动时的加速效果
    public float acceleration = 40.0f;

    // 允许的误差范围，当剩余距离小于该值时视为到达目标
    public float tolerance = 1.0f;

    /// <summary>
    /// 节点开始执行时初始化导航参数
    /// </summary>
    protected override void OnStart()
    {
        // 设置导航代理的停止距离
        context.agent.stoppingDistance = stoppingDistance;
        // 设置移动速度
        context.agent.speed = speed;
        // 从黑板获取目标位置并设置为导航目的地
        context.agent.destination = blackboard.TargetPosition;
        // 设置是否更新旋转
        context.agent.updateRotation = updateRotation;
        // 设置加速度参数
        context.agent.acceleration = acceleration;
    }

    /// <summary>
    /// 节点停止时的清理操作（当前无操作）
    /// </summary>
    protected override void OnStop()
    {
        // 无需执行任何清理操作
    }

    /// <summary>
    /// 每帧更新时检查移动状态并返回节点状态
    /// </summary>
    /// <returns>行为树节点状态（Success/Running/Failed）</returns>
    protected override State OnUpdate()
    {
        // 如果路径仍在计算中，保持运行状态
        if (context.agent.pathPending)
        {
            return State.Running;
        }

        // 如果剩余距离小于误差范围，视为到达目标
        if (context.agent.remainingDistance < tolerance)
        {
            return State.Success;
        }

        // 如果路径无效（如目标不可达），返回失败
        if (context.agent.pathStatus == UnityEngine.AI.NavMeshPathStatus.PathInvalid)
        {
            return State.Failure;
        }

        // 默认继续运行直到满足条件
        return State.Running;
    }
}