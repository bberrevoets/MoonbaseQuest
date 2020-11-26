// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

// Sequence - holds ordered entries for playback
[Serializable]
public class OVRLipSyncSequence : ScriptableObject
{
    public List<OVRLipSync.Frame> entries = new List<OVRLipSync.Frame>();
    public float                  length; // in seconds

    public OVRLipSync.Frame GetFrameAtTime(float time)
    {
        OVRLipSync.Frame frame = null;
        if (time < length && entries.Count > 0)
        {
            var percentComplete = time / length;
            frame = entries[(int) (entries.Count * percentComplete)];
        }

        return frame;
    }

    #if UNITY_EDITOR

    private static readonly int sSampleSize = 1024;

    public static OVRLipSyncSequence CreateSequenceFromAudioClip(
            AudioClip clip, bool useOfflineModel = false)
    {
        OVRLipSyncSequence sequence = null;

        if (clip.channels > 2)
        {
            Debug.LogError(clip.name +
                           ": Cannot process phonemes from an audio clip with " +
                           "more than 2 channels");
            return null;
        }

        if (clip.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            Debug.LogError(clip.name +
                           ": Cannot process phonemes from an audio clip unless " +
                           "its load type is set to DecompressOnLoad.");
            return null;
        }

        if (OVRLipSync.Initialize(clip.frequency, sSampleSize) != OVRLipSync.Result.Success)
        {
            Debug.LogError("Could not create Lip Sync engine.");
            return null;
        }

        if (clip.loadState != AudioDataLoadState.Loaded)
        {
            Debug.LogError("Clip is not loaded!");
            return null;
        }

        uint context = 0;

        var result = useOfflineModel
                             ? OVRLipSync.CreateContextWithModelFile(
                                     ref context,
                                     OVRLipSync.ContextProviders.Enhanced,
                                     Path.Combine(Application.dataPath, "Oculus/LipSync/Assets/OfflineModel/ovrlipsync_offline_model.pb"))
                             : OVRLipSync.CreateContext(ref context, OVRLipSync.ContextProviders.Enhanced);

        if (result != OVRLipSync.Result.Success)
        {
            Debug.LogError("Could not create Phoneme context. (" + result + ")");
            OVRLipSync.Shutdown();
            return null;
        }

        var frames  = new List<OVRLipSync.Frame>();
        var samples = new float[sSampleSize * clip.channels];

        var dummyFrame = new OVRLipSync.Frame();
        OVRLipSync.ProcessFrame(
                context,
                samples,
                dummyFrame,
                (clip.channels == 2) ? true : false
                );
        // frame delay in ms
        float frameDelayInMs = dummyFrame.frameDelay;

        var frameOffset = (int) (frameDelayInMs * clip.frequency / 1000);

        var totalSamples = clip.samples;
        for (var x = 0; x < totalSamples + frameOffset; x += sSampleSize)
        {
            var remainingSamples = totalSamples - x;
            if (remainingSamples >= sSampleSize)
            {
                clip.GetData(samples, x);
            }
            else if (remainingSamples > 0)
            {
                var samples_clip = new float[remainingSamples * clip.channels];
                clip.GetData(samples_clip, x);
                Array.Copy(samples_clip, samples, samples_clip.Length);
                Array.Clear(samples, samples_clip.Length, samples.Length - samples_clip.Length);
            }
            else
            {
                Array.Clear(samples, 0, samples.Length);
            }

            var frame = new OVRLipSync.Frame();
            if (clip.channels == 2)
            {
                // interleaved = stereo data, alternating floats
                OVRLipSync.ProcessFrame(context, samples, frame);
            }
            else
            {
                // mono
                OVRLipSync.ProcessFrame(context, samples, frame, false);
            }

            if (x < frameOffset)
            {
                continue;
            }

            frames.Add(frame);
        }

        Debug.Log(clip.name + " produced " + frames.Count +
                  " viseme frames, playback rate is " + (frames.Count / clip.length) +
                  " fps");
        OVRLipSync.DestroyContext(context);
        OVRLipSync.Shutdown();

        sequence         = CreateInstance<OVRLipSyncSequence>();
        sequence.entries = frames;
        sequence.length  = clip.length;

        return sequence;
    }
    #endif
}
