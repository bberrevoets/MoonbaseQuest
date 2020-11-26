// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Platform;
using Oculus.Platform.Models;

using UnityEngine;

using CAPI = Oculus.Avatar.CAPI;

// Helper class to manage a Peer-to-Peer connection to the other user.
// The connection is used to send and received the Transforms for the
// Avatars.  The Transforms are sent via unreliable UDP at a fixed
// frequency.
public class P2PManager
{
    public P2PManager()
    {
        Net.SetPeerConnectRequestCallback(PeerConnectRequestCallback);
        Net.SetConnectionStateChangedCallback(ConnectionStateChangedCallback);
    }

    #region Message Sending

    public void SendAvatarUpdate(ulong userID, Transform rootTransform, uint sequence, byte[] avatarPacket)
    {
        const int UPDATE_DATA_LENGTH = 41;
        var       sendBuffer         = new byte[avatarPacket.Length + UPDATE_DATA_LENGTH];

        var offset = 0;
        PackByte((byte) MessageType.Update, sendBuffer, ref offset);

        PackULong(SocialPlatformManager.MyID, sendBuffer, ref offset);

        PackFloat(rootTransform.position.x, sendBuffer, ref offset);
        // Lock to floor height
        PackFloat(0f,                       sendBuffer, ref offset);
        PackFloat(rootTransform.position.z, sendBuffer, ref offset);
        PackFloat(rootTransform.rotation.x, sendBuffer, ref offset);
        PackFloat(rootTransform.rotation.y, sendBuffer, ref offset);
        PackFloat(rootTransform.rotation.z, sendBuffer, ref offset);
        PackFloat(rootTransform.rotation.w, sendBuffer, ref offset);

        PackUInt32(sequence, sendBuffer, ref offset);

        Debug.Assert(offset == UPDATE_DATA_LENGTH);

        Buffer.BlockCopy(avatarPacket, 0, sendBuffer, offset, avatarPacket.Length);
        Net.SendPacket(userID, sendBuffer, SendPolicy.Unreliable);
    }

    #endregion

    // packet header is a message type byte
    private enum MessageType : byte
    {
        Update = 1
    }

    #region Connection Management

    public void ConnectTo(ulong userID)
    {
        // ID comparison is used to decide who calls Connect and who calls Accept
        if (SocialPlatformManager.MyID < userID)
        {
            Net.Connect(userID);
            SocialPlatformManager.LogOutput("P2P connect to " + userID);
        }
    }

    public void Disconnect(ulong userID)
    {
        if (userID != 0)
        {
            Net.Close(userID);

            var remote = SocialPlatformManager.GetRemoteUser(userID);
            if (remote != null)
            {
                remote.p2pConnectionState = PeerConnectionState.Unknown;
            }
        }
    }

    private void PeerConnectRequestCallback(Message<NetworkingPeer> msg)
    {
        SocialPlatformManager.LogOutput("P2P request from " + msg.Data.ID);

        var remote = SocialPlatformManager.GetRemoteUser(msg.Data.ID);
        if (remote != null)
        {
            SocialPlatformManager.LogOutput("P2P request accepted from " + msg.Data.ID);
            Net.Accept(msg.Data.ID);
        }
    }

    private void ConnectionStateChangedCallback(Message<NetworkingPeer> msg)
    {
        SocialPlatformManager.LogOutput("P2P state to " + msg.Data.ID + " changed to  " + msg.Data.State);

        var remote = SocialPlatformManager.GetRemoteUser(msg.Data.ID);
        if (remote != null)
        {
            remote.p2pConnectionState = msg.Data.State;

            if (msg.Data.State == PeerConnectionState.Timeout &&
                // ID comparison is used to decide who calls Connect and who calls Accept
                SocialPlatformManager.MyID < msg.Data.ID)
            {
                // keep trying until hangup!
                Net.Connect(msg.Data.ID);
                SocialPlatformManager.LogOutput("P2P re-connect to " + msg.Data.ID);
            }
        }
    }

    #endregion

    #region Message Receiving

    public void GetRemotePackets()
    {
        Packet packet;

        while ((packet = Net.ReadPacket()) != null)
        {
            var receiveBuffer = new byte[packet.Size];
            packet.ReadBytes(receiveBuffer);

            var offset      = 0;
            var messageType = (MessageType) ReadByte(receiveBuffer, ref offset);

            var remoteUserID = ReadULong(receiveBuffer, ref offset);
            var remote       = SocialPlatformManager.GetRemoteUser(remoteUserID);
            if (remote == null)
            {
                SocialPlatformManager.LogOutput("Unknown remote player: " + remoteUserID);
                continue;
            }

            if (messageType == MessageType.Update)
            {
                processAvatarPacket(remote, ref receiveBuffer, ref offset);
            }
            else
            {
                SocialPlatformManager.LogOutput("Invalid packet type: " + packet.Size);
            }
        }
    }

    public void processAvatarPacket(RemotePlayer remote, ref byte[] packet, ref int offset)
    {
        if (remote == null)
        {
            return;
        }

        remote.receivedRootPositionPrior = remote.receivedRootPosition;
        remote.receivedRootPosition.x    = ReadFloat(packet, ref offset);
        remote.receivedRootPosition.y    = ReadFloat(packet, ref offset);
        remote.receivedRootPosition.z    = ReadFloat(packet, ref offset);

        remote.receivedRootRotationPrior = remote.receivedRootRotation;
        remote.receivedRootRotation.x    = ReadFloat(packet, ref offset);
        remote.receivedRootRotation.y    = ReadFloat(packet, ref offset);
        remote.receivedRootRotation.z    = ReadFloat(packet, ref offset);
        remote.receivedRootRotation.w    = ReadFloat(packet, ref offset);

        remote.RemoteAvatar.transform.position = remote.receivedRootPosition;
        remote.RemoteAvatar.transform.rotation = remote.receivedRootRotation;

        // forward the remaining data to the avatar system
        var sequence = (int) ReadUInt32(packet, ref offset);

        var remainingAvatarBuffer = new byte[packet.Length - offset];
        Buffer.BlockCopy(packet, offset, remainingAvatarBuffer, 0, remainingAvatarBuffer.Length);

        var avatarPacket = CAPI.ovrAvatarPacket_Read((uint) remainingAvatarBuffer.Length, remainingAvatarBuffer);

        var ovravatarPacket = new OvrAvatarPacket {ovrNativePacket = avatarPacket};
        remote.RemoteAvatar.GetComponent<OvrAvatarRemoteDriver>().QueuePacket(sequence, ovravatarPacket);
    }

    #endregion

    #region Serialization

    private void PackByte(byte b, byte[] buf, ref int offset)
    {
        buf[offset] =  b;
        offset      += sizeof(byte);
    }

    private byte ReadByte(byte[] buf, ref int offset)
    {
        var val = buf[offset];
        offset += sizeof(byte);
        return val;
    }

    private void PackFloat(float f, byte[] buf, ref int offset)
    {
        Buffer.BlockCopy(BitConverter.GetBytes(f), 0, buf, offset, sizeof(float));
        offset += sizeof(float);
    }

    private float ReadFloat(byte[] buf, ref int offset)
    {
        var val = BitConverter.ToSingle(buf, offset);
        offset += sizeof(float);
        return val;
    }

    private void PackULong(ulong u, byte[] buf, ref int offset)
    {
        Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(ulong));
        offset += sizeof(ulong);
    }

    private ulong ReadULong(byte[] buf, ref int offset)
    {
        var val = BitConverter.ToUInt64(buf, offset);
        offset += sizeof(ulong);
        return val;
    }

    private void PackUInt32(uint u, byte[] buf, ref int offset)
    {
        Buffer.BlockCopy(BitConverter.GetBytes(u), 0, buf, offset, sizeof(uint));
        offset += sizeof(uint);
    }

    private uint ReadUInt32(byte[] buf, ref int offset)
    {
        var val = BitConverter.ToUInt32(buf, offset);
        offset += sizeof(uint);
        return val;
    }

    #endregion
}
