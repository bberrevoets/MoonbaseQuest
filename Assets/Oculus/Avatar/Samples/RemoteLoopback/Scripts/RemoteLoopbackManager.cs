// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;

using Oculus.Avatar;

using UnityEngine;

using Random = UnityEngine.Random;

public class RemoteLoopbackManager : MonoBehaviour
{
    public OvrAvatar LocalAvatar;
    public OvrAvatar LoopbackAvatar;

    public SimulatedLatencySettings LatencySettings = new SimulatedLatencySettings();

    private LinkedList<PacketLatencyPair> packetQueue = new LinkedList<PacketLatencyPair>();

    private int PacketSequence = 0;

    private void Start()
    {
        LocalAvatar.RecordPackets  =  true;
        LocalAvatar.PacketRecorded += OnLocalAvatarPacketRecorded;
        var FirstValue = Random.Range(LatencySettings.FakeLatencyMin, LatencySettings.FakeLatencyMax);
        LatencySettings.LatencyValues.AddFirst(FirstValue);
        LatencySettings.LatencySum += FirstValue;
    }

    private void Update()
    {
        if (packetQueue.Count > 0)
        {
            var deadList = new List<PacketLatencyPair>();
            foreach (var packet in packetQueue)
            {
                packet.FakeLatency -= Time.deltaTime;

                if (packet.FakeLatency < 0f)
                {
                    ReceivePacketData(packet.PacketData);
                    deadList.Add(packet);
                }
            }

            foreach (var packet in deadList)
            {
                packetQueue.Remove(packet);
            }
        }
    }

    private void OnLocalAvatarPacketRecorded(object sender, OvrAvatar.PacketEventArgs args)
    {
        using (var outputStream = new MemoryStream())
        {
            var writer = new BinaryWriter(outputStream);

            if (LocalAvatar.UseSDKPackets)
            {
                var size = CAPI.ovrAvatarPacket_GetSize(args.Packet.ovrNativePacket);
                var data = new byte[size];
                CAPI.ovrAvatarPacket_Write(args.Packet.ovrNativePacket, size, data);

                writer.Write(PacketSequence++);
                writer.Write(size);
                writer.Write(data);
            }
            else
            {
                writer.Write(PacketSequence++);
                args.Packet.Write(outputStream);
            }

            SendPacketData(outputStream.ToArray());
        }
    }

    private void SendPacketData(byte[] data)
    {
        var PacketPair = new PacketLatencyPair();
        PacketPair.PacketData  = data;
        PacketPair.FakeLatency = LatencySettings.NextValue();

        packetQueue.AddLast(PacketPair);
    }

    private void ReceivePacketData(byte[] data)
    {
        using (var inputStream = new MemoryStream(data))
        {
            var reader   = new BinaryReader(inputStream);
            var sequence = reader.ReadInt32();

            OvrAvatarPacket avatarPacket;
            if (LoopbackAvatar.UseSDKPackets)
            {
                var size    = reader.ReadInt32();
                var sdkData = reader.ReadBytes(size);

                var packet = CAPI.ovrAvatarPacket_Read((uint) data.Length, sdkData);
                avatarPacket = new OvrAvatarPacket {ovrNativePacket = packet};
            }
            else
            {
                avatarPacket = OvrAvatarPacket.Read(inputStream);
            }

            LoopbackAvatar.GetComponent<OvrAvatarRemoteDriver>().QueuePacket(sequence, avatarPacket);
        }
    }

    private class PacketLatencyPair
    {
        public float  FakeLatency;
        public byte[] PacketData;
    }

    [Serializable]
    public class SimulatedLatencySettings
    {
        [Range(0.0f, 0.5f)]
        public float FakeLatencyMax = 0.25f; //250 ms max latency

        [Range(0.0f, 0.5f)]
        public float FakeLatencyMin = 0.002f; //2ms min latency

        [Range(0.0f, 1.0f)]
        public float LatencyWeight = 0.25f; // How much the latest sample impacts the current latency

        [Range(0, 10)]
        public int MaxSamples = 4; //How many samples in our window

        internal float             AverageWindow = 0f;
        internal float             LatencySum    = 0f;
        internal LinkedList<float> LatencyValues = new LinkedList<float>();

        public float NextValue()
        {
            AverageWindow = LatencySum / LatencyValues.Count;
            var RandomLatency = Random.Range(FakeLatencyMin, FakeLatencyMax);
            var FakeLatency   = AverageWindow * (1f - LatencyWeight) + LatencyWeight * RandomLatency;

            if (LatencyValues.Count >= MaxSamples)
            {
                LatencySum -= LatencyValues.First.Value;
                LatencyValues.RemoveFirst();
            }

            LatencySum += FakeLatency;
            LatencyValues.AddLast(FakeLatency);

            return FakeLatency;
        }
    }
}
