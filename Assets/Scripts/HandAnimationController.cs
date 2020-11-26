// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR;

public class HandAnimationController : MonoBehaviour
{
    private static readonly  int                        TRIGGER        = Animator.StringToHash("Trigger");
    private static readonly  int                        GRIP           = Animator.StringToHash("Grip");
    [SerializeField] private InputDeviceCharacteristics controllerType = InputDeviceCharacteristics.None;
    private                  Animator                   animatorController;
    private                  bool                       isControllerDetected = false;

    private InputDevice thisController;

    // Start is called before the first frame update
    private void Start()
    {
        Initialize();
        animatorController = GetComponent<Animator>();
    }

    // Update is called once per frame
    private void Update()
    {
        if (isControllerDetected)
        {
            if (thisController.TryGetFeatureValue(CommonUsages.trigger, out var triggerValue) && triggerValue > 0.1f)
            {
                //Debug.Log($"Trigger Press {triggerValue}");
                animatorController.SetFloat(TRIGGER, triggerValue);
            }

            if (thisController.TryGetFeatureValue(CommonUsages.grip, out var gripValue) && gripValue > 0.1f)
            {
                //Debug.Log($"Grip Press {gripValue}");
                animatorController.SetFloat(GRIP, gripValue);
            }

            if (!thisController.isValid) { isControllerDetected = false; }
        }
        else
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        var controllerDevices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(controllerType, controllerDevices);

        if (controllerDevices.Count.Equals(0))
        {
            Debug.Log("List is empty.");
        }
        else
        {
            thisController       = controllerDevices[0];
            isControllerDetected = true;
            //Debug.Log(thisController.name);
        }
    }
}
