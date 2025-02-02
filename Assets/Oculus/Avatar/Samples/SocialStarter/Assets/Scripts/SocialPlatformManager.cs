// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;

using AOT;

using Oculus.Platform;
using Oculus.Platform.Models;

using UnityEngine;

using Application = UnityEngine.Application;
using CAPI = Oculus.Avatar.CAPI;
using Random = UnityEngine.Random;

// This class coordinates communication with the Oculus Platform
// Service running in your device.
public class SocialPlatformManager : MonoBehaviour
{
    private static readonly Vector3 START_ROTATION_ONE = new Vector3(0, 180, 0);
    private static readonly Vector3 START_POSITION_ONE = new Vector3(0, 4,   5);

    private static readonly Vector3 START_ROTATION_TWO = new Vector3(0, 0, 0);
    private static readonly Vector3 START_POSITION_TWO = new Vector3(0, 4, -5);

    private static readonly Vector3 START_ROTATION_THREE = new Vector3(0, 270, 0);
    private static readonly Vector3 START_POSITION_THREE = new Vector3(5, 4,   0);

    private static readonly Vector3 START_ROTATION_FOUR = new Vector3(0,  90, 0);
    private static readonly Vector3 START_POSITION_FOUR = new Vector3(-5, 4,  0);

    private static readonly Color BLACK = new Color(0.0f, 0.0f, 0.0f);
    private static readonly Color WHITE = new Color(1.0f, 1.0f, 1.0f);
    private static readonly Color CYAN  = new Color(0.0f, 1.0f, 1.0f);
    private static readonly Color BLUE  = new Color(0.0f, 0.0f, 1.0f);
    private static readonly Color GREEN = new Color(0.0f, 1.0f, 0.0f);

    protected static SocialPlatformManager s_instance = null;

    // animating the mouth for voip
    public static readonly float VOIP_SCALE = 2f;

    public OvrAvatar localAvatarPrefab;
    public OvrAvatar remoteAvatarPrefab;

    public GameObject helpPanel;
    public Material   riftMaterial;
    public Material   gearMaterial;

    // GameObject that represents the center sphere as a visual status indicator of the room
    public GameObject roomSphere;
    public GameObject roomFloor;

    protected State        currentState;
    protected MeshRenderer floorMesh;
    protected MeshRenderer helpMesh;

    protected OvrAvatar  localAvatar;
    protected GameObject localPlayerHead;
    protected GameObject localTrackingSpace;

    // my Application-scoped Oculus ID
    protected ulong myID;

    // my Oculus user name
    protected string     myOculusID;
    protected P2PManager p2pManager;

    // Local player
    private uint packetSequence = 0;

    // Remote players
    protected Dictionary<ulong, RemotePlayer> remoteUsers = new Dictionary<ulong, RemotePlayer>();
    protected RoomManager                     roomManager;
    protected MeshRenderer                    sphereMesh;

    private   float       voiceCurrent = 0.0f;
    protected VoipManager voipManager;

    public virtual void Update()
    {
        // Look for updates from remote users
        p2pManager.GetRemotePackets();

        // update avatar mouths to match voip volume
        foreach (var kvp in remoteUsers)
        {
            if (kvp.Value.voipSource == null)
            {
                if (kvp.Value.RemoteAvatar.MouthAnchor != null)
                {
                    kvp.Value.voipSource          = kvp.Value.RemoteAvatar.MouthAnchor.AddComponent<VoipAudioSourceHiLevel>();
                    kvp.Value.voipSource.senderID = kvp.Value.remoteUserID;
                }
            }

            if (kvp.Value.voipSource != null)
            {
                var remoteVoiceCurrent = Mathf.Clamp(kvp.Value.voipSource.peakAmplitude * VOIP_SCALE, 0f, 1f);
                kvp.Value.RemoteAvatar.VoiceAmplitude = remoteVoiceCurrent;
            }
        }

        if (localAvatar != null)
        {
            localAvatar.VoiceAmplitude = Mathf.Clamp(voiceCurrent * VOIP_SCALE, 0f, 1f);
        }

        Request.RunCallbacks();
    }

    #region Initialization and Shutdown

    public virtual void Awake()
    {
        LogOutputLine("Start Log.");

        // Grab the MeshRenderers. We'll be using the material colour to visually show status
        helpMesh   = helpPanel.GetComponent<MeshRenderer>();
        sphereMesh = roomSphere.GetComponent<MeshRenderer>();
        floorMesh  = roomFloor.GetComponent<MeshRenderer>();

        // Set up the local player
        localTrackingSpace = transform.Find("OVRCameraRig/TrackingSpace").gameObject;
        localPlayerHead    = transform.Find("OVRCameraRig/TrackingSpace/CenterEyeAnchor").gameObject;

        // make sure only one instance of this manager ever exists
        if (s_instance != null)
        {
            Destroy(gameObject);
            return;
        }

        s_instance = this;
        DontDestroyOnLoad(gameObject);

        TransitionToState(State.INITIALIZING);

        Core.AsyncInitialize().OnComplete(InitCallback);

        roomManager = new RoomManager();
        p2pManager  = new P2PManager();
        voipManager = new VoipManager();
    }

    private void InitCallback(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        var launchDetails = ApplicationLifecycle.GetLaunchDetails();
        LogOutput("App launched with LaunchType " + launchDetails.LaunchType);

        // First thing we should do is perform an entitlement check to make sure
        // we successfully connected to the Oculus Platform Service.
        Entitlements.IsUserEntitledToApplication().OnComplete(IsEntitledCallback);
    }

    public virtual void Start()
    {
        // noop here, but is being overridden in PlayerController
    }

    private void IsEntitledCallback(Message msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        // Next get the identity of the user that launched the Application.
        Users.GetLoggedInUser().OnComplete(GetLoggedInUserCallback);
    }

    private void GetLoggedInUserCallback(Message<User> msg)
    {
        if (msg.IsError)
        {
            TerminateWithError(msg);
            return;
        }

        myID       = msg.Data.ID;
        myOculusID = msg.Data.OculusID;

        localAvatar                  = Instantiate(localAvatarPrefab);
        localAvatar.CanOwnMicrophone = false;
        localTrackingSpace           = transform.Find("OVRCameraRig/TrackingSpace").gameObject;

        localAvatar.transform.SetParent(localTrackingSpace.transform, false);
        localAvatar.transform.localPosition = new Vector3(0, 0, 0);
        localAvatar.transform.localRotation = Quaternion.identity;

        if (Application.platform == RuntimePlatform.Android)
        {
            helpPanel.transform.SetParent(localAvatar.transform.Find("body"), false);
            helpPanel.transform.localPosition = new Vector3(0, 1.0f, 1.0f);
            helpMesh.material                 = gearMaterial;
        }
        else
        {
            helpPanel.transform.SetParent(localAvatar.transform.Find("hand_left"), false);
            helpPanel.transform.localPosition = new Vector3(0, 0.2f, 0.2f);
            helpMesh.material                 = riftMaterial;
        }

        localAvatar.oculusUserID               =  myID.ToString();
        localAvatar.RecordPackets              =  true;
        localAvatar.PacketRecorded             += OnLocalAvatarPacketRecorded;
        localAvatar.EnableMouthVertexAnimation =  true;

        var rotation = Quaternion.identity;

        switch (Random.Range(0, 4))
        {
            case 0:
                rotation.eulerAngles    = START_ROTATION_ONE;
                transform.localPosition = START_POSITION_ONE;
                transform.localRotation = rotation;
                break;

            case 1:
                rotation.eulerAngles    = START_ROTATION_TWO;
                transform.localPosition = START_POSITION_TWO;
                transform.localRotation = rotation;
                break;

            case 2:
                rotation.eulerAngles    = START_ROTATION_THREE;
                transform.localPosition = START_POSITION_THREE;
                transform.localRotation = rotation;
                break;

            case 3:
            default:
                rotation.eulerAngles    = START_ROTATION_FOUR;
                transform.localPosition = START_POSITION_FOUR;
                transform.localRotation = rotation;
                break;
        }

        TransitionToState(State.CHECKING_LAUNCH_STATE);

        // If the user launched the app by accepting the notification, then we want to
        // join that room.  If not, try to find a friend's room to join
        if (!roomManager.CheckForInvite())
        {
            LogOutput("No invite on launch, looking for a friend to join.");
            Users.GetLoggedInUserFriendsAndRooms()
                 .OnComplete(GetLoggedInUserFriendsAndRoomsCallback);
        }

        Voip.SetMicrophoneFilterCallback(MicFilter);
    }

    private void GetLoggedInUserFriendsAndRoomsCallback(Message<UserAndRoomList> msg)
    {
        if (msg.IsError)
        {
            return;
        }

        foreach (var el in msg.Data)
        {
            // see if any friends are in a joinable room
            if (el.User == null)
            {
                continue;
            }

            if (el.RoomOptional == null)
            {
                continue;
            }

            if (el.RoomOptional.IsMembershipLocked)
            {
                continue;
            }

            if (el.RoomOptional.Joinability != RoomJoinability.CanJoin)
            {
                continue;
            }

            if (el.RoomOptional.JoinPolicy == RoomJoinPolicy.None)
            {
                continue;
            }

            LogOutput("Trying to join room " + el.RoomOptional.ID + ", friend " + el.User.OculusID);
            roomManager.JoinExistingRoom(el.RoomOptional.ID);
            return;
        }

        LogOutput("No friend to join. Creating my own room.");
        // didn't find any open rooms, start a new room
        roomManager.CreateRoom();
        TransitionToState(State.CREATING_A_ROOM);
    }

    public void OnLocalAvatarPacketRecorded(object sender, OvrAvatar.PacketEventArgs args)
    {
        var size   = CAPI.ovrAvatarPacket_GetSize(args.Packet.ovrNativePacket);
        var toSend = new byte[size];

        CAPI.ovrAvatarPacket_Write(args.Packet.ovrNativePacket, size, toSend);

        foreach (var kvp in remoteUsers)
        {
            //LogOutputLine("Sending avatar Packet to  " + kvp.Key);
            // Root is local tracking space transform
            p2pManager.SendAvatarUpdate(kvp.Key, localTrackingSpace.transform, packetSequence, toSend);
        }

        packetSequence++;
    }

    public void OnApplicationQuit()
    {
        roomManager.LeaveCurrentRoom();

        foreach (var kvp in remoteUsers)
        {
            p2pManager.Disconnect(kvp.Key);
            voipManager.Disconnect(kvp.Key);
        }

        LogOutputLine("End Log.");
    }

    public void AddUser(ulong userID, ref RemotePlayer remoteUser)
    {
        remoteUsers.Add(userID, remoteUser);
    }

    public void LogOutputLine(string line)
    {
        Debug.Log(Time.time + ": " + line);
    }

    // For most errors we terminate the Application since this example doesn't make
    // sense if the user is disconnected.
    public static void TerminateWithError(Message msg)
    {
        s_instance.LogOutputLine("Error: " + msg.GetError().Message);
        Application.Quit();
    }

    #endregion

    #region Properties

    public static State CurrentState => s_instance.currentState;

    public static ulong MyID
    {
        get
        {
            if (s_instance != null)
            {
                return s_instance.myID;
            }

            return 0;
        }
    }

    public static string MyOculusID
    {
        get
        {
            if (s_instance != null && s_instance.myOculusID != null)
            {
                return s_instance.myOculusID;
            }

            return string.Empty;
        }
    }

    #endregion

    #region State Management

    public enum State
    {
        // loading platform library, checking application entitlement,
        // getting the local user info
        INITIALIZING,

        // Checking to see if we were launched from an invite
        CHECKING_LAUNCH_STATE,

        // Creating a room to join
        CREATING_A_ROOM,

        // in this state we've create a room, and hopefully
        // sent some invites, and we're waiting people to join
        WAITING_IN_A_ROOM,

        // in this state we're attempting to join a room from an invite
        JOINING_A_ROOM,

        // we're in a room with others
        CONNECTED_IN_A_ROOM,

        // Leaving a room
        LEAVING_A_ROOM,

        // shutdown any connections and leave the current room
        SHUTDOWN
    }

    public static void TransitionToState(State newState)
    {
        if (s_instance)
        {
            s_instance.LogOutputLine("State " + s_instance.currentState + " -> " + newState);
        }

        if (s_instance && s_instance.currentState != newState)
        {
            s_instance.currentState = newState;

            // state transition logic
            switch (newState)
            {
                case State.SHUTDOWN:
                    s_instance.OnApplicationQuit();
                    break;
            }
        }

        SetSphereColorForState();
    }

    private static void SetSphereColorForState()
    {
        switch (s_instance.currentState)
        {
            case State.INITIALIZING:
            case State.SHUTDOWN:
                s_instance.sphereMesh.material.color = BLACK;
                break;

            case State.WAITING_IN_A_ROOM:
                s_instance.sphereMesh.material.color = WHITE;
                break;

            case State.CONNECTED_IN_A_ROOM:
                s_instance.sphereMesh.material.color = CYAN;
                break;
        }
    }

    public static void SetFloorColorForState(bool host)
    {
        if (host)
        {
            s_instance.floorMesh.material.color = BLUE;
        }
        else
        {
            s_instance.floorMesh.material.color = GREEN;
        }
    }

    public static void MarkAllRemoteUsersAsNotInRoom()
    {
        foreach (var kvp in s_instance.remoteUsers)
        {
            kvp.Value.stillInRoom = false;
        }
    }

    public static void MarkRemoteUserInRoom(ulong userID)
    {
        var remoteUser = new RemotePlayer();

        if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
        {
            remoteUser.stillInRoom = true;
        }
    }

    public static void ForgetRemoteUsersNotInRoom()
    {
        var toPurge = new List<ulong>();

        foreach (var kvp in s_instance.remoteUsers)
        {
            if (kvp.Value.stillInRoom == false)
            {
                toPurge.Add(kvp.Key);
            }
        }

        foreach (var key in toPurge)
        {
            RemoveRemoteUser(key);
        }
    }

    public static void LogOutput(string line)
    {
        s_instance.LogOutputLine(Time.time + ": " + line);
    }

    public static bool IsUserInRoom(ulong userID) => s_instance.remoteUsers.ContainsKey(userID);

    public static void AddRemoteUser(ulong userID)
    {
        var remoteUser = new RemotePlayer();

        remoteUser.RemoteAvatar                            = Instantiate(s_instance.remoteAvatarPrefab);
        remoteUser.RemoteAvatar.oculusUserID               = userID.ToString();
        remoteUser.RemoteAvatar.ShowThirdPerson            = true;
        remoteUser.RemoteAvatar.EnableMouthVertexAnimation = true;
        remoteUser.p2pConnectionState                      = PeerConnectionState.Unknown;
        remoteUser.voipConnectionState                     = PeerConnectionState.Unknown;
        remoteUser.stillInRoom                             = true;
        remoteUser.remoteUserID                            = userID;

        s_instance.AddUser(userID, ref remoteUser);
        s_instance.p2pManager.ConnectTo(userID);
        s_instance.voipManager.ConnectTo(userID);

        s_instance.LogOutputLine("Adding User " + userID);
    }

    public static void RemoveRemoteUser(ulong userID)
    {
        var remoteUser = new RemotePlayer();

        if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
        {
            Destroy(remoteUser.RemoteAvatar.MouthAnchor.GetComponent<VoipAudioSourceHiLevel>(), 0);
            Destroy(remoteUser.RemoteAvatar.gameObject,                                         0);
            s_instance.remoteUsers.Remove(userID);

            s_instance.LogOutputLine("Removing User " + userID);
        }
    }

    public void UpdateVoiceData(short[] pcmData, int numChannels)
    {
        if (localAvatar != null)
        {
            localAvatar.UpdateVoiceData(pcmData, numChannels);
        }

        var voiceMax = 0.0f;
        var floats   = new float[pcmData.Length];
        for (var n = 0; n < pcmData.Length; n++)
        {
            var cur = floats[n] = pcmData[n] / (float) short.MaxValue;
            if (cur > voiceMax)
            {
                voiceMax = cur;
            }
        }

        voiceCurrent = voiceMax;
    }

    [MonoPInvokeCallback(typeof(Oculus.Platform.CAPI.FilterCallback))]
    public static void MicFilter(short[] pcmData, UIntPtr pcmDataLength, int frequency, int numChannels)
    {
        s_instance.UpdateVoiceData(pcmData, numChannels);
    }

    public static RemotePlayer GetRemoteUser(ulong userID)
    {
        var remoteUser = new RemotePlayer();

        if (s_instance.remoteUsers.TryGetValue(userID, out remoteUser))
        {
            return remoteUser;
        }

        return null;
    }

    #endregion
}
