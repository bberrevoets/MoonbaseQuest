// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OVRTouchSample
{
    public enum HandPoseId
    {
        Default,
        Generic,
        PingPongBall,
        Controller
    }

    // Stores pose-specific data such as the animation id and allowing gestures.
    public class HandPose : MonoBehaviour
    {
        [SerializeField]
        private bool m_allowPointing = false;

        [SerializeField]
        private bool m_allowThumbsUp = false;

        [SerializeField]
        private HandPoseId m_poseId = HandPoseId.Default;

        public bool AllowPointing => m_allowPointing;

        public bool AllowThumbsUp => m_allowThumbsUp;

        public HandPoseId PoseId => m_poseId;
    }
}
