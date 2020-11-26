// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;

using Random = UnityEngine.Random;

namespace OculusSampleFramework
{
    // Simple component that changes color based on grab state.
    public class ColorGrabbable : OVRGrabbable
    {
        public static readonly Color COLOR_GRAB      = new Color(1.0f, 0.5f, 0.0f, 1.0f);
        public static readonly Color COLOR_HIGHLIGHT = new Color(1.0f, 0.0f, 1.0f, 1.0f);

        private Color          m_color = Color.black;
        private bool           m_highlight;
        private MeshRenderer[] m_meshRenderers = null;

        public bool Highlight
        {
            get => m_highlight;
            set
            {
                m_highlight = value;
                UpdateColor();
            }
        }

        private void Awake()
        {
            if (m_grabPoints.Length == 0)
            {
                // Get the collider from the grabbable
                var collider = GetComponent<Collider>();
                if (collider == null)
                {
                    throw new ArgumentException("Grabbables cannot have zero grab points and no collider -- please add a grab point or collider.");
                }

                // Create a default grab point
                m_grabPoints = new Collider[1] {collider};

                // Grab points are doing double-duty as a way to identify submeshes that should be colored.
                // If unspecified, just color self.
                m_meshRenderers    = new MeshRenderer[1];
                m_meshRenderers[0] = GetComponent<MeshRenderer>();
            }
            else
            {
                m_meshRenderers = GetComponentsInChildren<MeshRenderer>();
            }

            m_color = new Color(
                    Random.Range(0.1f, 0.95f),
                    Random.Range(0.1f, 0.95f),
                    Random.Range(0.1f, 0.95f),
                    1.0f
                    );
            SetColor(m_color);
        }

        protected void UpdateColor()
        {
            if (isGrabbed)
            {
                SetColor(COLOR_GRAB);
            }
            else if (Highlight)
            {
                SetColor(COLOR_HIGHLIGHT);
            }
            else
            {
                SetColor(m_color);
            }
        }

        public override void GrabBegin(OVRGrabber hand, Collider grabPoint)
        {
            base.GrabBegin(hand, grabPoint);
            UpdateColor();
        }

        public override void GrabEnd(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            base.GrabEnd(linearVelocity, angularVelocity);
            UpdateColor();
        }

        private void SetColor(Color color)
        {
            for (var i = 0; i < m_meshRenderers.Length; ++i)
            {
                var meshRenderer = m_meshRenderers[i];
                for (var j = 0; j < meshRenderer.materials.Length; ++j)
                {
                    var meshMaterial = meshRenderer.materials[j];
                    meshMaterial.color = color;
                }
            }
        }
    }
}
