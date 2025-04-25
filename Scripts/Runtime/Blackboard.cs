using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheKiwiCoder {

    // This is the blackboard container shared between all nodes.
    // Use this to store temporary data that multiple nodes need read and write access to.
    // Add other properties here that make sense for your specific use case.
    [System.Serializable]
    public class Blackboard {
        [Tooltip("目标位置")]
        public Vector3 TargetPosition;
        [Tooltip("行为树挂接对象")]
        public GameObject BTUser;
        [Tooltip("目标对象")]
        public Transform target;
    }
}