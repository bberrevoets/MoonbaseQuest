// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

[RequireComponent(typeof(AudioSource))]

//-------------------------------------------------------------------------------------
// ***** OVRLipSyncContextBase
//
/// <summary>
/// OVRLipSyncContextBase interfaces into the Oculus phoneme recognizer.
/// This component should be added into the scene once for each Audio Source.
///
/// </summary>
public class OVRLipSyncContextBase : MonoBehaviour
{
    // * * * * * * * * * * * * *
    // Public members
    public AudioSource audioSource = null;

    [Tooltip("Which lip sync provider to use for viseme computation.")]
    public OVRLipSync.ContextProviders provider = OVRLipSync.ContextProviders.Enhanced;

    [Tooltip("Enable DSP offload on supported Android devices.")]
    public bool enableAcceleration = true;

    private int  _smoothing;
    private uint context = 0; // 0 is no context

    // * * * * * * * * * * * * *
    // Private members

    public int Smoothing
    {
        set
        {
            var result =
                    OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeSmoothing, value, 0);

            if (result != OVRLipSync.Result.Success)
            {
                if (result == OVRLipSync.Result.InvalidParam)
                {
                    Debug.LogError("OVRLipSyncContextBase.SetSmoothing: A viseme smoothing" +
                                   " parameter is invalid, it should be between 1 and 100!");
                }
                else
                {
                    Debug.LogError("OVRLipSyncContextBase.SetSmoothing: An unexpected" +
                                   " error occured.");
                }
            }

            _smoothing = value;
        }
        get => _smoothing;
    }

    public uint Context => context;

    protected OVRLipSync.Frame Frame { get; } = new OVRLipSync.Frame();

    /// <summary>
    ///     Awake this instance.
    /// </summary>
    private void Awake()
    {
        // Cache the audio source we are going to be using to pump data to the SR
        if (!audioSource)
        {
            audioSource = GetComponent<AudioSource>();
        }

        lock (this)
        {
            if (context == 0)
            {
                if (OVRLipSync.CreateContext(ref context, provider, 0, enableAcceleration)
                    != OVRLipSync.Result.Success)
                {
                    Debug.LogError("OVRLipSyncContextBase.Start ERROR: Could not create" +
                                   " Phoneme context.");
                }
            }
        }
    }

    /// <summary>
    ///     Raises the destroy event.
    /// </summary>
    private void OnDestroy()
    {
        // Create the context that we will feed into the audio buffer
        lock (this)
        {
            if (context != 0)
            {
                if (OVRLipSync.DestroyContext(context) != OVRLipSync.Result.Success)
                {
                    Debug.LogError("OVRLipSyncContextBase.OnDestroy ERROR: Could not delete" +
                                   " Phoneme context.");
                }
            }
        }
    }

    // * * * * * * * * * * * * *
    // Public Functions

    /// <summary>
    ///     Gets the current phoneme frame (lock and copy current frame to caller frame)
    /// </summary>
    /// <returns>error code</returns>
    /// <param name="inFrame">In frame.</param>
    public OVRLipSync.Frame GetCurrentPhonemeFrame() => Frame;

    /// <summary>
    ///     Sets a given viseme id blend weight to a given amount
    /// </summary>
    /// <param name="viseme">Integer viseme ID</param>
    /// <param name="amount">Integer viseme amount</param>
    public void SetVisemeBlend(int viseme, int amount)
    {
        var result =
                OVRLipSync.SendSignal(context, OVRLipSync.Signals.VisemeAmount, viseme, amount);

        if (result != OVRLipSync.Result.Success)
        {
            if (result == OVRLipSync.Result.InvalidParam)
            {
                Debug.LogError("OVRLipSyncContextBase.SetVisemeBlend: Viseme ID is invalid.");
            }
            else
            {
                Debug.LogError("OVRLipSyncContextBase.SetVisemeBlend: An unexpected" +
                               " error occured.");
            }
        }
    }

    /// <summary>
    ///     Sets a given viseme id blend weight to a given amount
    /// </summary>
    /// <param name="amount">Integer viseme amount</param>
    public void SetLaughterBlend(int amount)
    {
        var result =
                OVRLipSync.SendSignal(context, OVRLipSync.Signals.LaughterAmount, amount, 0);

        if (result != OVRLipSync.Result.Success)
        {
            Debug.LogError("OVRLipSyncContextBase.SetLaughterBlend: An unexpected" +
                           " error occured.");
        }
    }

    /// <summary>
    ///     Resets the context.
    /// </summary>
    /// <returns>error code</returns>
    public OVRLipSync.Result ResetContext()
    {
        // Reset visemes to silence etc.
        Frame.Reset();

        return OVRLipSync.ResetContext(context);
    }
}
