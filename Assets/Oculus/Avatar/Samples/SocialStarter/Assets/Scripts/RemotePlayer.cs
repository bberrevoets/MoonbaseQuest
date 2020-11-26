// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using Oculus.Platform;

using UnityEngine;

public class RemotePlayer
{
    // the result of the last connection state update message
    public PeerConnectionState p2pConnectionState;

    // the last received root transform position updates, equivalent to local tracking space transform
    public Vector3 receivedRootPosition;

    // the previous received positions to interpolate from
    public Vector3 receivedRootPositionPrior;

    // the last received root transform rotation updates, equivalent to local tracking space transform
    public Quaternion receivedRootRotation;

    // the previous received rotations to interpolate from
    public Quaternion receivedRootRotationPrior;

    public OvrAvatar RemoteAvatar;
    public ulong     remoteUserID;

    public bool stillInRoom;

    // the last reported state of the VOIP connection
    public PeerConnectionState voipConnectionState;

    // the voip tracker for the player
    public VoipAudioSourceHiLevel voipSource;
}
