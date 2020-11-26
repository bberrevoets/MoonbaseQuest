// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.EventSystems;

public class HandedInputSelector : MonoBehaviour
{
    private OVRCameraRig   m_CameraRig;
    private OVRInputModule m_InputModule;

    private void Start()
    {
        m_CameraRig   = FindObjectOfType<OVRCameraRig>();
        m_InputModule = FindObjectOfType<OVRInputModule>();
    }

    private void Update()
    {
        if (OVRInput.GetActiveController() == OVRInput.Controller.LTouch)
        {
            SetActiveController(OVRInput.Controller.LTouch);
        }
        else
        {
            SetActiveController(OVRInput.Controller.RTouch);
        }
    }

    private void SetActiveController(OVRInput.Controller c)
    {
        Transform t;
        if (c == OVRInput.Controller.LTouch)
        {
            t = m_CameraRig.leftHandAnchor;
        }
        else
        {
            t = m_CameraRig.rightHandAnchor;
        }

        m_InputModule.rayTransform = t;
    }
}
