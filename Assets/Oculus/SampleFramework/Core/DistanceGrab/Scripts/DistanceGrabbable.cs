// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OculusSampleFramework
{
    public class DistanceGrabbable : OVRGrabbable
    {
        public string m_materialColorField;

        private GrabbableCrosshair    m_crosshair;
        private GrabManager           m_crosshairManager;
        private bool                  m_inRange;
        private MaterialPropertyBlock m_mpb;
        private Renderer              m_renderer;
        private bool                  m_targeted;

        public bool InRange
        {
            get => m_inRange;
            set
            {
                m_inRange = value;
                RefreshCrosshair();
            }
        }

        public bool Targeted
        {
            get => m_targeted;
            set
            {
                m_targeted = value;
                RefreshCrosshair();
            }
        }

        protected override void Start()
        {
            base.Start();
            m_crosshair        = gameObject.GetComponentInChildren<GrabbableCrosshair>();
            m_renderer         = gameObject.GetComponent<Renderer>();
            m_crosshairManager = FindObjectOfType<GrabManager>();
            m_mpb              = new MaterialPropertyBlock();
            RefreshCrosshair();
            m_renderer.SetPropertyBlock(m_mpb);
        }

        private void RefreshCrosshair()
        {
            if (m_crosshair)
            {
                if (isGrabbed)
                {
                    m_crosshair.SetState(GrabbableCrosshair.CrosshairState.Disabled);
                }
                else if (!InRange)
                {
                    m_crosshair.SetState(GrabbableCrosshair.CrosshairState.Disabled);
                }
                else
                {
                    m_crosshair.SetState(Targeted ? GrabbableCrosshair.CrosshairState.Targeted : GrabbableCrosshair.CrosshairState.Enabled);
                }
            }

            if (m_materialColorField != null)
            {
                m_renderer.GetPropertyBlock(m_mpb);
                if (isGrabbed || !InRange)
                {
                    m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorOutOfRange);
                }
                else if (Targeted)
                {
                    m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorHighlighted);
                }
                else
                {
                    m_mpb.SetColor(m_materialColorField, m_crosshairManager.OutlineColorInRange);
                }

                m_renderer.SetPropertyBlock(m_mpb);
            }
        }
    }
}
