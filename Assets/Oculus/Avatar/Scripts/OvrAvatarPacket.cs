// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

public class OvrAvatarPacket
{
    private List<byte[]>                    encodedAudioPackets = new List<byte[]>();
    private List<OvrAvatarDriver.PoseFrame> frames              = new List<OvrAvatarDriver.PoseFrame>();

    // ===============================================================
    // All code below used for unity only pose blending option.
    // ===============================================================
    private List<float> frameTimes = new List<float>();

    // Used with SDK driven packet flow
    public IntPtr ovrNativePacket = IntPtr.Zero;

    public OvrAvatarPacket() { }

    public OvrAvatarPacket(OvrAvatarDriver.PoseFrame initialPose)
    {
        frameTimes.Add(0.0f);
        frames.Add(initialPose);
    }

    private OvrAvatarPacket(List<float> frameTimes, List<OvrAvatarDriver.PoseFrame> frames, List<byte[]> audioPackets)
    {
        this.frameTimes = frameTimes;
        this.frames     = frames;
    }

    public float                     Duration   => frameTimes[frameTimes.Count - 1];
    public OvrAvatarDriver.PoseFrame FinalFrame => frames[frames.Count - 1];

    public void AddFrame(OvrAvatarDriver.PoseFrame frame, float deltaSeconds)
    {
        frameTimes.Add(Duration + deltaSeconds);
        frames.Add(frame);
    }

    public OvrAvatarDriver.PoseFrame GetPoseFrame(float seconds)
    {
        if (frames.Count == 1)
        {
            return frames[0];
        }

        // This can be replaced with a more efficient binary search
        var tailIndex = 1;
        while (tailIndex < frameTimes.Count && frameTimes[tailIndex] < seconds)
        {
            ++tailIndex;
        }

        var a     = frames[tailIndex - 1];
        var b     = frames[tailIndex];
        var aTime = frameTimes[tailIndex - 1];
        var bTime = frameTimes[tailIndex];
        var t     = (seconds - aTime) / (bTime - aTime);
        return OvrAvatarDriver.PoseFrame.Interpolate(a, b, t);
    }

    public static OvrAvatarPacket Read(Stream stream)
    {
        var reader = new BinaryReader(stream);

        // Todo: bounds check frame count
        var frameCount = reader.ReadInt32();
        var frameTimes = new List<float>(frameCount);
        for (var i = 0; i < frameCount; ++i)
        {
            frameTimes.Add(reader.ReadSingle());
        }

        var frames = new List<OvrAvatarDriver.PoseFrame>(frameCount);
        for (var i = 0; i < frameCount; ++i)
        {
            frames.Add(reader.ReadPoseFrame());
        }

        // Todo: bounds check audio packet count
        var audioPacketCount = reader.ReadInt32();
        var audioPackets     = new List<byte[]>(audioPacketCount);
        for (var i = 0; i < audioPacketCount; ++i)
        {
            var audioPacketSize = reader.ReadInt32();
            var audioPacket     = reader.ReadBytes(audioPacketSize);
            audioPackets.Add(audioPacket);
        }

        return new OvrAvatarPacket(frameTimes, frames, audioPackets);
    }

    public void Write(Stream stream)
    {
        var writer = new BinaryWriter(stream);

        // Write all of the frames
        var frameCount = frameTimes.Count;
        writer.Write(frameCount);
        for (var i = 0; i < frameCount; ++i)
        {
            writer.Write(frameTimes[i]);
        }

        for (var i = 0; i < frameCount; ++i)
        {
            var frame = frames[i];
            writer.Write(frame);
        }

        // Write all of the encoded audio packets
        var audioPacketCount = encodedAudioPackets.Count;
        writer.Write(audioPacketCount);
        for (var i = 0; i < audioPacketCount; ++i)
        {
            var packet = encodedAudioPackets[i];
            writer.Write(packet.Length);
            writer.Write(packet);
        }
    }
}

internal static class BinaryWriterExtensions
{
    public static void Write(this BinaryWriter writer, OvrAvatarDriver.PoseFrame frame)
    {
        writer.Write(frame.headPosition);
        writer.Write(frame.headRotation);
        writer.Write(frame.handLeftPosition);
        writer.Write(frame.handLeftRotation);
        writer.Write(frame.handRightPosition);
        writer.Write(frame.handRightRotation);
        writer.Write(frame.voiceAmplitude);

        writer.Write(frame.controllerLeftPose);
        writer.Write(frame.controllerRightPose);
    }

    public static void Write(this BinaryWriter writer, Vector3 vec3)
    {
        writer.Write(vec3.x);
        writer.Write(vec3.y);
        writer.Write(vec3.z);
    }

    public static void Write(this BinaryWriter writer, Vector2 vec2)
    {
        writer.Write(vec2.x);
        writer.Write(vec2.y);
    }

    public static void Write(this BinaryWriter writer, Quaternion quat)
    {
        writer.Write(quat.x);
        writer.Write(quat.y);
        writer.Write(quat.z);
        writer.Write(quat.w);
    }

    public static void Write(this BinaryWriter writer, OvrAvatarDriver.ControllerPose pose)
    {
        writer.Write((uint) pose.buttons);
        writer.Write((uint) pose.touches);
        writer.Write(pose.joystickPosition);
        writer.Write(pose.indexTrigger);
        writer.Write(pose.handTrigger);
        writer.Write(pose.isActive);
    }
}

internal static class BinaryReaderExtensions
{
    public static OvrAvatarDriver.PoseFrame ReadPoseFrame(this BinaryReader reader) =>
            new OvrAvatarDriver.PoseFrame
            {
                    headPosition      = reader.ReadVector3(),
                    headRotation      = reader.ReadQuaternion(),
                    handLeftPosition  = reader.ReadVector3(),
                    handLeftRotation  = reader.ReadQuaternion(),
                    handRightPosition = reader.ReadVector3(),
                    handRightRotation = reader.ReadQuaternion(),
                    voiceAmplitude    = reader.ReadSingle(),

                    controllerLeftPose  = reader.ReadControllerPose(),
                    controllerRightPose = reader.ReadControllerPose()
            };

    public static Vector2 ReadVector2(this BinaryReader reader) =>
            new Vector2
            {
                    x = reader.ReadSingle(),
                    y = reader.ReadSingle()
            };

    public static Vector3 ReadVector3(this BinaryReader reader) =>
            new Vector3
            {
                    x = reader.ReadSingle(),
                    y = reader.ReadSingle(),
                    z = reader.ReadSingle()
            };

    public static Quaternion ReadQuaternion(this BinaryReader reader) =>
            new Quaternion
            {
                    x = reader.ReadSingle(),
                    y = reader.ReadSingle(),
                    z = reader.ReadSingle(),
                    w = reader.ReadSingle()
            };

    public static OvrAvatarDriver.ControllerPose ReadControllerPose(this BinaryReader reader) =>
            new OvrAvatarDriver.ControllerPose
            {
                    buttons          = (ovrAvatarButton) reader.ReadUInt32(),
                    touches          = (ovrAvatarTouch) reader.ReadUInt32(),
                    joystickPosition = reader.ReadVector2(),
                    indexTrigger     = reader.ReadSingle(),
                    handTrigger      = reader.ReadSingle(),
                    isActive         = reader.ReadBoolean()
            };
}
