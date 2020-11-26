// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.UI;

namespace OculusSampleFramework
{
    public class DistanceGrabberSample : MonoBehaviour
    {
        [SerializeField] private DistanceGrabber[] m_grabbers = null;

        private bool allowGrabThroughWalls = false;

        private bool useSpherecast = false;

        public bool UseSpherecast
        {
            get => useSpherecast;
            set
            {
                useSpherecast = value;
                for (var i = 0; i < m_grabbers.Length; ++i)
                {
                    m_grabbers[i].UseSpherecast = useSpherecast;
                }
            }
        }

        public bool AllowGrabThroughWalls
        {
            get => allowGrabThroughWalls;
            set
            {
                allowGrabThroughWalls = value;
                for (var i = 0; i < m_grabbers.Length; ++i)
                {
                    m_grabbers[i].m_preventGrabThroughWalls = !allowGrabThroughWalls;
                }
            }
        }

        // Use this for initialization
        private void Start()
        {
            DebugUIBuilder.instance.AddLabel("Distance Grab Sample");
            DebugUIBuilder.instance.AddToggle("Use Spherecasting",  ToggleSphereCasting,    useSpherecast);
            DebugUIBuilder.instance.AddToggle("Grab Through Walls", ToggleGrabThroughWalls, allowGrabThroughWalls);
            DebugUIBuilder.instance.Show();

            // Forcing physics tick rate to match game frame rate, for improved physics in this sample.
            // See comment in OVRGrabber.Update for more information.
            var freq = OVRManager.display.displayFrequency;
            if (freq > 0.1f)
            {
                Debug.Log("Setting Time.fixedDeltaTime to: " + (1.0f / freq));
                Time.fixedDeltaTime = 1.0f / freq;
            }
        }

        public void ToggleSphereCasting(Toggle t)
        {
            UseSpherecast = !UseSpherecast;
        }

        public void ToggleGrabThroughWalls(Toggle t)
        {
            AllowGrabThroughWalls = !AllowGrabThroughWalls;
        }
    }
}
