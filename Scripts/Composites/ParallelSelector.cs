using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TheKiwiCoder
{
    public class ParallelSelector : CompositeNode
    {
        List<State> childrenExecutionState = new List<State>();

        protected override void OnStart()
        {
            childrenExecutionState.Clear();
            foreach (var child in children)
            {
                childrenExecutionState.Add(State.Running);
            }
        }

        protected override void OnStop()
        {
            AbortAllRunning();
        }

        protected override State OnUpdate()
        {
            bool allFailed = true;

            for (int i = 0; i < children.Count; ++i)
            {
                if (childrenExecutionState[i] == State.Running)
                {
                    var status = children[i].Update();

                    if (status == State.Success)
                    {
                        AbortAllRunning();
                        return State.Success;
                    }

                    childrenExecutionState[i] = status;
                }

                if (childrenExecutionState[i] != State.Failure)
                {
                    allFailed = false;
                }
            }

            return allFailed ? State.Failure : State.Running;
        }

        void AbortAllRunning()
        {
            for (int i = 0; i < children.Count; ++i)
            {
                if (childrenExecutionState[i] == State.Running)
                {
                    children[i].Abort();
                    childrenExecutionState[i] = State.Failure;
                }
            }
        }
    }
}
