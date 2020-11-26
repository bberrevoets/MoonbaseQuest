// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OculusSampleFramework
{
    public class GrabbableCrosshair : MonoBehaviour
    {
        public enum CrosshairState
        {
            Disabled,
            Enabled,
            Targeted
        }

        [SerializeField] private GameObject m_targetedCrosshair = null;

        [SerializeField] private GameObject m_enabledCrosshair = null;

        private Transform m_centerEyeAnchor;

        private CrosshairState m_state = CrosshairState.Disabled;

        private void Start()
        {
            m_centerEyeAnchor = GameObject.Find("CenterEyeAnchor").transform;
        }

        private void Update()
        {
            if (m_state != CrosshairState.Disabled)
            {
                transform.LookAt(m_centerEyeAnchor);
            }
        }

        public void SetState(CrosshairState cs)
        {
            m_state = cs;
            if (cs == CrosshairState.Disabled)
            {
                m_targetedCrosshair.SetActive(false);
                m_enabledCrosshair.SetActive(false);
            }
            else if (cs == CrosshairState.Enabled)
            {
                m_targetedCrosshair.SetActive(false);
                m_enabledCrosshair.SetActive(true);
            }
            else if (cs == CrosshairState.Targeted)
            {
                m_targetedCrosshair.SetActive(true);
                m_enabledCrosshair.SetActive(false);
            }
        }
    }
}
