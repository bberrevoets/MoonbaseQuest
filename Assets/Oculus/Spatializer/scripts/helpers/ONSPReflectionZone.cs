// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;

public struct ReflectionSnapshot
{
    public AudioMixerSnapshot mixerSnapshot;
    public float              fadeTime;
}

public class ONSPReflectionZone : MonoBehaviour
{
    // Push/pop list
    private static Stack<ReflectionSnapshot> snapshotList    = new Stack<ReflectionSnapshot>();
    private static ReflectionSnapshot        currentSnapshot = new ReflectionSnapshot();
    public         AudioMixerSnapshot        mixerSnapshot   = null;
    public         float                     fadeTime        = 0.0f;

    /// <summary>
    ///     Start this instance.
    /// </summary>
    private void Start() { }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update() { }

    /// <summary>
    ///     Raises the trigger enter event.
    /// </summary>
    /// <param name="other">Other.</param>
    private void OnTriggerEnter(Collider other)
    {
        if (CheckForAudioListener(other.gameObject))
        {
            PushCurrentMixerShapshot();
        }
    }

    /// <summary>
    ///     Raises the trigger exit event.
    /// </summary>
    /// <param name="other">Other.</param>
    private void OnTriggerExit(Collider other)
    {
        if (CheckForAudioListener(other.gameObject))
        {
            PopCurrentMixerSnapshot();
        }
    }

    // * * * * * * * * * * * * *
    // Private functions

    /// <summary>
    ///     Checks for audio listener.
    /// </summary>
    /// <returns><c>true</c>, if for audio listener was checked, <c>false</c> otherwise.</returns>
    /// <param name="gameObject">Game object.</param>
    private bool CheckForAudioListener(GameObject gameObject)
    {
        var al = gameObject.GetComponentInChildren<AudioListener>();
        if (al != null)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Pushs the current mixer snapshot onto the snapshot stack
    /// </summary>
    private void PushCurrentMixerShapshot()
    {
        var css = currentSnapshot;
        snapshotList.Push(css);

        // Set the zone reflection values
        // NOTE: There will be conditions that might need resolution when dealing with volumes that 
        // overlap. Best practice is to never have volumes half-way inside other volumes; larger
        // volumes should completely contain smaller volumes
        SetReflectionValues();
    }

    /// <summary>
    ///     Pops the current reflection values from reflectionsList stack.
    /// </summary>
    private void PopCurrentMixerSnapshot()
    {
        var snapshot = snapshotList.Pop();

        // Set the popped reflection values
        SetReflectionValues(ref snapshot);
    }

    /// <summary>
    ///     Sets the reflection values. This is done when entering a zone (use zone values).
    /// </summary>
    private void SetReflectionValues()
    {
        if (mixerSnapshot != null)
        {
            Debug.Log("Setting off snapshot " + mixerSnapshot.name);
            mixerSnapshot.TransitionTo(fadeTime);

            // Set the current snapshot to be equal to this one
            currentSnapshot.mixerSnapshot = mixerSnapshot;
            currentSnapshot.fadeTime      = fadeTime;
        }
        else
        {
            Debug.Log("Mixer snapshot not set - Please ensure play area has at least one encompassing snapshot.");
        }
    }

    /// <summary>
    ///     Sets the reflection values. This is done when exiting a zone (use popped values).
    /// </summary>
    /// <param name="rm">Rm.</param>
    private void SetReflectionValues(ref ReflectionSnapshot mss)
    {
        if (mss.mixerSnapshot != null)
        {
            Debug.Log("Setting off snapshot " + mss.mixerSnapshot.name);
            mss.mixerSnapshot.TransitionTo(mss.fadeTime);

            // Set the current snapshot to be equal to this one
            currentSnapshot.mixerSnapshot = mss.mixerSnapshot;
            currentSnapshot.fadeTime      = mss.fadeTime;
        }
        else
        {
            Debug.Log("Mixer snapshot not set - Please ensure play area has at least one encompassing snapshot.");
        }
    }
}
