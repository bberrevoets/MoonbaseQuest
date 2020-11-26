// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Diagnostics;
using System.Threading;

using UnityEngine;

using Debug = UnityEngine.Debug;

[RequireComponent(typeof(AudioSource))]
public class OVRLipSyncMicInput : MonoBehaviour
{
    public enum micActivation
    {
        HoldToSpeak,
        PushToSpeak,
        ConstantSpeak
    }

    // PUBLIC MEMBERS
    [Tooltip("Manual specification of Audio Source - " +
             "by default will use any attached to the same object.")]
    public AudioSource audioSource = null;

    [Tooltip("Enable a keypress to toggle the microphone device selection GUI.")]
    public bool enableMicSelectionGUI = false;

    [Tooltip("Key to toggle the microphone selection GUI if enabled.")]
    public KeyCode micSelectionGUIKey = KeyCode.M;

    [SerializeField]
    [Range(0.0f, 100.0f)]
    [Tooltip("Microphone input volume control.")]
    private float micInputVolume = 100;

    [SerializeField]
    [Tooltip("Requested microphone input frequency")]
    private int micFrequency = 48000;

    [Tooltip("Microphone input control method. Hold To Speak and Push" +
             " To Speak are driven with the Mic Activation Key.")]
    public micActivation micControl = micActivation.ConstantSpeak;

    [Tooltip("Key used to drive Hold To Speak and Push To Speak methods" +
             " of microphone input control.")]
    public KeyCode micActivationKey = KeyCode.Space;

    [Tooltip("Will contain the string name of the selected microphone device - read only.")]
    public string selectedDevice;

    private bool focused     = true;
    private bool initialized = false;

    // PRIVATE MEMBERS
    private bool micSelected = false;
    private int  minFreq, maxFreq;

    public float MicFrequency
    {
        get => micFrequency;
        set => micFrequency = (int) Mathf.Clamp(value, 0, 96000);
    }

    //----------------------------------------------------
    // MONOBEHAVIOUR OVERRIDE FUNCTIONS
    //----------------------------------------------------

    /// <summary>
    ///     Awake this instance.
    /// </summary>
    private void Awake()
    {
        // First thing to do, cache the unity audio source (can be managed by the
        // user if audio source can change)
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (!audioSource) { }
    }

    /// <summary>
    ///     Start this instance.
    /// </summary>
    private void Start()
    {
        audioSource.loop = true; // Set the AudioClip to loop
        audioSource.mute = false;

        InitializeMicrophone();
    }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update()
    {
        if (!focused)
        {
            if (Microphone.IsRecording(selectedDevice))
            {
                StopMicrophone();
            }

            return;
        }

        if (!Application.isPlaying)
        {
            StopMicrophone();
            return;
        }

        // Lazy Microphone initialization (needed on Android)
        if (!initialized)
        {
            InitializeMicrophone();
        }

        audioSource.volume = (micInputVolume / 100);

        //Hold To Speak
        if (micControl == micActivation.HoldToSpeak)
        {
            if (Input.GetKey(micActivationKey))
            {
                if (!Microphone.IsRecording(selectedDevice))
                {
                    StartMicrophone();
                }
            }
            else
            {
                if (Microphone.IsRecording(selectedDevice))
                {
                    StopMicrophone();
                }
            }
        }

        //Push To Talk
        if (micControl == micActivation.PushToSpeak)
        {
            if (Input.GetKeyDown(micActivationKey))
            {
                if (Microphone.IsRecording(selectedDevice))
                {
                    StopMicrophone();
                }
                else if (!Microphone.IsRecording(selectedDevice))
                {
                    StartMicrophone();
                }
            }
        }

        //Constant Speak
        if (micControl == micActivation.ConstantSpeak)
        {
            if (!Microphone.IsRecording(selectedDevice))
            {
                StartMicrophone();
            }
        }

        //Mic Selected = False
        if (enableMicSelectionGUI)
        {
            if (Input.GetKeyDown(micSelectionGUIKey))
            {
                micSelected = false;
            }
        }
    }

    private void OnDisable()
    {
        StopMicrophone();
    }

    /// <summary>
    ///     Raises the GU event.
    /// </summary>
    private void OnGUI()
    {
        MicDeviceGUI((Screen.width / 2) - 150, (Screen.height / 2) - 75, 300, 50, 10, -300);
    }

    /// <summary>
    ///     Raises the application focus event.
    /// </summary>
    /// <param name="focus">If set to <c>true</c>: focused.</param>
    private void OnApplicationFocus(bool focus)
    {
        focused = focus;

        if (!focused)
        {
            StopMicrophone();
        }
    }

    /// <summary>
    ///     Raises the application pause event.
    /// </summary>
    /// <param name="pauseStatus">If set to <c>true</c>: paused.</param>
    private void OnApplicationPause(bool pauseStatus)
    {
        focused = !pauseStatus;

        if (!focused)
        {
            StopMicrophone();
        }
    }

    /// <summary>
    ///     Initializes the microphone.
    /// </summary>
    private void InitializeMicrophone()
    {
        if (initialized)
        {
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            return;
        }

        selectedDevice = Microphone.devices[0];
        micSelected    = true;
        GetMicCaps();
        initialized = true;
    }

    //----------------------------------------------------
    // PUBLIC FUNCTIONS
    //----------------------------------------------------

    /// <summary>
    ///     Mics the device GU.
    /// </summary>
    /// <param name="left">Left.</param>
    /// <param name="top">Top.</param>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="buttonSpaceTop">Button space top.</param>
    /// <param name="buttonSpaceLeft">Button space left.</param>
    public void MicDeviceGUI(
            float left,
            float top,
            float width,
            float height,
            float buttonSpaceTop,
            float buttonSpaceLeft)
    {
        //If there is more than one device, choose one.
        if (Microphone.devices.Length >= 1 && enableMicSelectionGUI && micSelected == false)
        {
            for (var i = 0; i < Microphone.devices.Length; ++i)
            {
                if (GUI.Button(new Rect(left + ((width + buttonSpaceLeft) * i),
                                top + ((height + buttonSpaceTop) * i), width, height),
                        Microphone.devices[i]))
                {
                    StopMicrophone();
                    selectedDevice = Microphone.devices[i];
                    micSelected    = true;
                    GetMicCaps();
                    StartMicrophone();
                }
            }
        }
    }

    /// <summary>
    ///     Gets the mic caps.
    /// </summary>
    public void GetMicCaps()
    {
        if (micSelected == false)
        {
            return;
        }

        //Gets the frequency of the device
        Microphone.GetDeviceCaps(selectedDevice, out minFreq, out maxFreq);

        if (minFreq == 0 && maxFreq == 0)
        {
            Debug.LogWarning("GetMicCaps warning:: min and max frequencies are 0");
            minFreq = 44100;
            maxFreq = 44100;
        }

        if (micFrequency > maxFreq)
        {
            micFrequency = maxFreq;
        }
    }

    /// <summary>
    ///     Starts the microphone.
    /// </summary>
    public void StartMicrophone()
    {
        if (micSelected == false)
        {
            return;
        }

        //Starts recording
        audioSource.clip = Microphone.Start(selectedDevice, true, 1, micFrequency);

        var timer = Stopwatch.StartNew();

        // Wait until the recording has started
        while (!(Microphone.GetPosition(selectedDevice) > 0) && timer.Elapsed.TotalMilliseconds < 1000)
        {
            Thread.Sleep(50);
        }

        if (Microphone.GetPosition(selectedDevice) <= 0)
        {
            throw new Exception("Timeout initializing microphone " + selectedDevice);
        }

        // Play the audio source
        audioSource.Play();
    }

    /// <summary>
    ///     Stops the microphone.
    /// </summary>
    public void StopMicrophone()
    {
        if (micSelected == false)
        {
            return;
        }

        // Overriden with a clip to play? Don't stop the audio source
        if ((audioSource != null) &&
            (audioSource.clip != null) &&
            (audioSource.clip.name == "Microphone"))
        {
            audioSource.Stop();
        }

        // Reset to stop mouth movement
        var context = GetComponent<OVRLipSyncContext>();
        context.ResetContext();

        Microphone.End(selectedDevice);
    }

    //----------------------------------------------------
    // PRIVATE FUNCTIONS
    //----------------------------------------------------

    /// <summary>
    ///     Gets the averaged volume.
    /// </summary>
    /// <returns>The averaged volume.</returns>
    private float GetAveragedVolume() =>
            // We will use the SR to get average volume
            // return OVRSpeechRec.GetAverageVolume();
            0.0f;
}
