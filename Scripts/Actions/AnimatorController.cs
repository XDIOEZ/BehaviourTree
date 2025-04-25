using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TheKiwiCoder;

//TODO 如何自定义特性 特性中保存当前节点在右键菜单中的地址
[NodeMenu("ActionNode/Animator/播放动画")]
public class AnimatorController : ActionNode
{

    // 通过一个枚举实现对SetTrigger和SetBool的选择
    public enum ParameterType
    {
        SetTrigger,
        SetBool
    }

    [Header("动画类型")]
    public ParameterType parameterType;

    [Header("动画参数名")]
    public string animationName;

    [Header("SetBool 的选项 (对 Trigger 无影响)")]
    public bool boolSwitch = true;
    [Header("---------------其他设置----------------")]
    [Header("检测paramName是否存在于Animator中")]
    public bool checkParamName = true;

    protected override void OnStart()
    {
        // 可选初始化逻辑
    }

    protected override void OnStop()
    {
        // 可选清理逻辑
    }

    protected override State OnUpdate()
    {
        if (context.animator == null)
        {
            Debug.LogWarning($"[{nameof(AnimatorController)}] Animator 为 null，无法播放动画。");
            return State.Failure;
        }

        if (!HasParameter(context.animator, animationName, parameterType))
        {
            Debug.LogWarning($"[{nameof(AnimatorController)}] Animator 中未找到参数：{animationName}（类型：{parameterType}）");
            return State.Failure;
        }

        switch (parameterType)
        {
            case ParameterType.SetBool:
                context.animator.SetBool(animationName, boolSwitch);
                break;

            case ParameterType.SetTrigger:
                context.animator.SetTrigger(animationName);
                break;
        }

        return State.Success;
    }

    /// <summary>
    /// 检查 Animator 是否包含指定参数
    /// </summary>
    private bool HasParameter(Animator animator, string paramName, ParameterType type)
    {
        var parameters = animator.parameters;
        foreach (var param in parameters)
        {
            if (param.name == paramName)
            {
                if ((type == ParameterType.SetTrigger && param.type == AnimatorControllerParameterType.Trigger) ||
                    (type == ParameterType.SetBool && param.type == AnimatorControllerParameterType.Bool))
                {
                    return true;
                }
            }
        }
        return false;
    }
}
