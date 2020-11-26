// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;

#endif

/// <summary>
///     Simply aggregates accessors.
/// </summary>
public class LocomotionController : MonoBehaviour
{
    public OVRCameraRig CameraRig;

    //public CharacterController CharacterController;
    public CapsuleCollider CharacterController;

    //public OVRPlayerController PlayerController;
    public SimpleCapsuleWithStickMovement PlayerController;

    private void Start()
    {
        /*
        if (CharacterController == null)
        {
            CharacterController = GetComponentInParent<CharacterController>();
        }
        Assert.IsNotNull(CharacterController);
        */
        //if (PlayerController == null)
        //{
        //PlayerController = GetComponentInParent<OVRPlayerController>();
        //}
        //Assert.IsNotNull(PlayerController);
        if (CameraRig == null)
        {
            CameraRig = FindObjectOfType<OVRCameraRig>();
        }

        Assert.IsNotNull(CameraRig);
        #if UNITY_EDITOR
        OVRPlugin.SendEvent("locomotion_controller", (SceneManager.GetActiveScene().name == "Locomotion").ToString(), "sample_framework");
        #endif
    }
}
