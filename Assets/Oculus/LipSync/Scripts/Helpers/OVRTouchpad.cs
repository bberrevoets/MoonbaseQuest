// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;

//-------------------------------------------------------------------------------------
// ***** OVRTouchpad
//
// OVRTouchpad is an interface class to a touchpad.
//
public static class OVRTouchpad
{
    public delegate void OVRTouchpadCallback<TouchEvent>(TouchEvent arg);

    //-------------------------
    // Input enums
    public enum TouchEvent
    {
        SingleTap,
        DoubleTap,
        Left,
        Right,
        Up,
        Down
    }

    // mouse
    private static Vector3  moveAmountMouse;
    private static float    minMovMagnitudeMouse = 25.0f;
    public static  Delegate touchPadCallbacks    = null;

    //Disable the unused variable warning
    #pragma warning disable 0414

    //Ensures that the TouchpadHelper will be created automatically upon start of the game.
    private static OVRTouchpadHelper touchpadHelper =
            (new GameObject("OVRTouchpadHelper")).AddComponent<OVRTouchpadHelper>();

    #pragma warning restore 0414

    // We will call this to create the TouchpadHelper class. This will
    // add the Touchpad game object into the world and we can call into
    // TouchEvent static functions to hook delegates into for touch capture
    public static void Create()
    {
        // Does nothing but call constructor to add game object into scene
    }

    // Update
    public static void Update()
    {
        // MOUSE INPUT

        if (Input.GetMouseButtonDown(0))
        {
            moveAmountMouse = Input.mousePosition;
        }
        else if (Input.GetMouseButtonUp(0))
        {
            moveAmountMouse -= Input.mousePosition;
            HandleInputMouse(ref moveAmountMouse);
        }
    }

    // OnDisable
    public static void OnDisable() { }

    // HandleInputMouse
    private static void HandleInputMouse(ref Vector3 move)
    {
        if (touchPadCallbacks == null)
        {
            return;
        }

        var callback = touchPadCallbacks as OVRTouchpadCallback<TouchEvent>;

        if (move.magnitude < minMovMagnitudeMouse)
        {
            callback(TouchEvent.SingleTap);
        }
        else
        {
            move.Normalize();

            // Left/Right
            if (Mathf.Abs(move.x) > Mathf.Abs(move.y))
            {
                if (move.x > 0.0f)
                {
                    callback(TouchEvent.Left);
                }
                else
                {
                    callback(TouchEvent.Right);
                }
            }
            // Up/Down
            else
            {
                if (move.y > 0.0f)
                {
                    callback(TouchEvent.Down);
                }
                else
                {
                    callback(TouchEvent.Up);
                }
            }
        }
    }

    public static void AddListener(OVRTouchpadCallback<TouchEvent> handler)
    {
        touchPadCallbacks = (OVRTouchpadCallback<TouchEvent>) touchPadCallbacks + handler;
    }
}

//-------------------------------------------------------------------------------------
// ***** OVRTouchpadHelper
//
// This singleton class gets created and stays resident in the application. It is used to
// trap the touchpad values, which get broadcast to any listener on the "Touchpad" channel.
//
// This class also demontrates how to make calls from any class that needs these events by
// setting up a listener to "Touchpad" channel.
public sealed class OVRTouchpadHelper : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Add a listener to the OVRTouchpad for testing
        OVRTouchpad.AddListener(LocalTouchEventCallback);
    }

    private void Update()
    {
        OVRTouchpad.Update();
    }

    public void OnDisable()
    {
        OVRTouchpad.OnDisable();
    }

    // LocalTouchEventCallback
    private void LocalTouchEventCallback(OVRTouchpad.TouchEvent touchEvent)
    {
        switch (touchEvent)
        {
            case (OVRTouchpad.TouchEvent.SingleTap):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-SINGLE TAP");
                break;

            case (OVRTouchpad.TouchEvent.DoubleTap):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-DOUBLE TAP");
                break;

            case (OVRTouchpad.TouchEvent.Left):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-SWIPE LEFT");
                break;

            case (OVRTouchpad.TouchEvent.Right):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-SWIPE RIGHT");
                break;

            case (OVRTouchpad.TouchEvent.Up):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-SWIPE UP");
                break;

            case (OVRTouchpad.TouchEvent.Down):
                //            OVRLipSyncDebugConsole.Clear();
                //            OVRLipSyncDebugConsole.ClearTimeout(1.5f);
                //            OVRLipSyncDebugConsole.Log("TP-SWIPE DOWN");
                break;
        }
    }
}
