// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;

using Oculus.Avatar;

using UnityEngine;

public class OvrAvatarRemoteDriver : OvrAvatarDriver
{
    private const int             MinPacketQueue    = 1;
    private const int             MaxPacketQueue    = 4;
    private       OvrAvatarPacket currentPacket     = null;
    private       float           CurrentPacketTime = 0f;

    private IntPtr CurrentSDKPacket = IntPtr.Zero;

    private int CurrentSequence = -1;

    // Used for legacy Unity only packet blending
    private bool                   isStreaming = false;
    private Queue<OvrAvatarPacket> packetQueue = new Queue<OvrAvatarPacket>();

    public void QueuePacket(int sequence, OvrAvatarPacket packet)
    {
        if (sequence > CurrentSequence)
        {
            CurrentSequence = sequence;
            packetQueue.Enqueue(packet);
        }
    }

    public override void UpdateTransforms(IntPtr sdkAvatar)
    {
        switch (Mode)
        {
            case PacketMode.SDK:
                UpdateFromSDKPacket(sdkAvatar);
                break;
            case PacketMode.Unity:
                UpdateFromUnityPacket(sdkAvatar);
                break;
        }
    }

    private void UpdateFromSDKPacket(IntPtr sdkAvatar)
    {
        if (CurrentSDKPacket == IntPtr.Zero && packetQueue.Count >= MinPacketQueue)
        {
            CurrentSDKPacket = packetQueue.Dequeue().ovrNativePacket;
        }

        if (CurrentSDKPacket != IntPtr.Zero)
        {
            var PacketDuration = CAPI.ovrAvatarPacket_GetDurationSeconds(CurrentSDKPacket);
            CAPI.ovrAvatar_UpdatePoseFromPacket(sdkAvatar, CurrentSDKPacket, Mathf.Min(PacketDuration, CurrentPacketTime));
            CurrentPacketTime += Time.deltaTime;

            if (CurrentPacketTime > PacketDuration)
            {
                CAPI.ovrAvatarPacket_Free(CurrentSDKPacket);
                CurrentSDKPacket  = IntPtr.Zero;
                CurrentPacketTime = CurrentPacketTime - PacketDuration;

                //Throw away packets deemed too old.
                while (packetQueue.Count > MaxPacketQueue)
                {
                    packetQueue.Dequeue();
                }
            }
        }
    }

    private void UpdateFromUnityPacket(IntPtr sdkAvatar)
    {
        // If we're not currently streaming, check to see if we've buffered enough
        if (!isStreaming && packetQueue.Count > MinPacketQueue)
        {
            currentPacket = packetQueue.Dequeue();
            isStreaming   = true;
        }

        // If we are streaming, update our pose
        if (isStreaming)
        {
            CurrentPacketTime += Time.deltaTime;

            // If we've elapsed past our current packet, advance
            while (CurrentPacketTime > currentPacket.Duration)
            {
                // If we're out of packets, stop streaming and
                // lock to the final frame
                if (packetQueue.Count == 0)
                {
                    CurrentPose       = currentPacket.FinalFrame;
                    CurrentPacketTime = 0.0f;
                    currentPacket     = null;
                    isStreaming       = false;
                    return;
                }

                while (packetQueue.Count > MaxPacketQueue)
                {
                    packetQueue.Dequeue();
                }

                // Otherwise, dequeue the next packet
                CurrentPacketTime -= currentPacket.Duration;
                currentPacket     =  packetQueue.Dequeue();
            }

            // Compute the pose based on our current time offset in the packet
            CurrentPose = currentPacket.GetPoseFrame(CurrentPacketTime);

            UpdateTransformsFromPose(sdkAvatar);
        }
    }
}
