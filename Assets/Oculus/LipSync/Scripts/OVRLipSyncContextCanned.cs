// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

[RequireComponent(typeof(AudioSource))]

//-------------------------------------------------------------------------------------
// ***** OVRLipSyncContextCanned
//
/// <summary>
/// OVRLipSyncContextCanned drives a canned phoneme sequence based on a pre-generated asset.
///
/// </summary>
public class OVRLipSyncContextCanned : OVRLipSyncContextBase
{
    [Tooltip("Pre-computed viseme sequence asset. Compute from audio in Unity with Tools -> Oculus -> Generate Lip Sync Assets.")]
    public OVRLipSyncSequence currentSequence;

    /// <summary>
    ///     Run processes that need to be updated in game thread
    /// </summary>
    private void Update()
    {
        if (audioSource.isPlaying && currentSequence != null)
        {
            var currentFrame = currentSequence.GetFrameAtTime(audioSource.time);
            Frame.CopyInput(currentFrame);
        }
    }
}
