// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Avatar;

using UnityEngine;

public abstract class OvrAvatarDriver : MonoBehaviour
{
    public enum PacketMode
    {
        SDK,
        Unity
    }

    public PacketMode Mode;

    private   ovrAvatarControllerType ControllerType = ovrAvatarControllerType.Quest;
    protected PoseFrame               CurrentPose;

    private void Start()
    {
        var headsetType = OVRPlugin.GetSystemHeadsetType();
        switch (headsetType)
        {
            case OVRPlugin.SystemHeadset.Oculus_Quest:
            case OVRPlugin.SystemHeadset.Rift_S:
                ControllerType = ovrAvatarControllerType.Quest;
                break;
            case OVRPlugin.SystemHeadset.Rift_DK1:
            case OVRPlugin.SystemHeadset.Rift_DK2:
            case OVRPlugin.SystemHeadset.Rift_CV1:
            default:
                ControllerType = ovrAvatarControllerType.Touch;
                break;
        }
    }

    public          PoseFrame GetCurrentPose() => CurrentPose;
    public abstract void      UpdateTransforms(IntPtr sdkAvatar);

    public void UpdateTransformsFromPose(IntPtr sdkAvatar)
    {
        if (sdkAvatar != IntPtr.Zero)
        {
            var bodyTransform   = OvrAvatar.CreateOvrAvatarTransform(CurrentPose.headPosition, CurrentPose.headRotation);
            var inputStateLeft  = OvrAvatar.CreateInputState(OvrAvatar.CreateOvrAvatarTransform(CurrentPose.handLeftPosition,  CurrentPose.handLeftRotation),  CurrentPose.controllerLeftPose);
            var inputStateRight = OvrAvatar.CreateInputState(OvrAvatar.CreateOvrAvatarTransform(CurrentPose.handRightPosition, CurrentPose.handRightRotation), CurrentPose.controllerRightPose);

            CAPI.ovrAvatarPose_UpdateBody(sdkAvatar, bodyTransform);
            CAPI.ovrAvatarPose_UpdateHandsWithType(sdkAvatar, inputStateLeft, inputStateRight, ControllerType);
        }
    }

    public static bool GetIsTrackedRemote() => false;

    public struct ControllerPose
    {
        public ovrAvatarButton buttons;
        public ovrAvatarTouch  touches;
        public Vector2         joystickPosition;
        public float           indexTrigger;
        public float           handTrigger;
        public bool            isActive;

        public static ControllerPose Interpolate(ControllerPose a, ControllerPose b, float t) =>
                new ControllerPose
                {
                        buttons          = t < 0.5f ? a.buttons : b.buttons,
                        touches          = t < 0.5f ? a.touches : b.touches,
                        joystickPosition = Vector2.Lerp(a.joystickPosition, b.joystickPosition, t),
                        indexTrigger     = Mathf.Lerp(a.indexTrigger, b.indexTrigger, t),
                        handTrigger      = Mathf.Lerp(a.handTrigger,  b.handTrigger,  t),
                        isActive         = t < 0.5f ? a.isActive : b.isActive
                };
    }

    public struct PoseFrame
    {
        public Vector3    headPosition;
        public Quaternion headRotation;
        public Vector3    handLeftPosition;
        public Quaternion handLeftRotation;
        public Vector3    handRightPosition;
        public Quaternion handRightRotation;
        public float      voiceAmplitude;

        public ControllerPose controllerLeftPose;
        public ControllerPose controllerRightPose;

        public static PoseFrame Interpolate(PoseFrame a, PoseFrame b, float t) =>
                new PoseFrame
                {
                        headPosition        = Vector3.Lerp(a.headPosition, b.headPosition, t),
                        headRotation        = Quaternion.Slerp(a.headRotation, b.headRotation, t),
                        handLeftPosition    = Vector3.Lerp(a.handLeftPosition, b.handLeftPosition, t),
                        handLeftRotation    = Quaternion.Slerp(a.handLeftRotation, b.handLeftRotation, t),
                        handRightPosition   = Vector3.Lerp(a.handRightPosition, b.handRightPosition, t),
                        handRightRotation   = Quaternion.Slerp(a.handRightRotation, b.handRightRotation, t),
                        voiceAmplitude      = Mathf.Lerp(a.voiceAmplitude, b.voiceAmplitude, t),
                        controllerLeftPose  = ControllerPose.Interpolate(a.controllerLeftPose,  b.controllerLeftPose,  t),
                        controllerRightPose = ControllerPose.Interpolate(a.controllerRightPose, b.controllerRightPose, t)
                };
    }
}
