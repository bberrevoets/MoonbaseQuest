// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections;

using UnityEngine;
using UnityEngine.Audio;

namespace OVR
{
    /*
    -----------------------
     
     SoundEmitter()
     
    -----------------------
    */
    public class SoundEmitter : MonoBehaviour
    {
        public enum FadeState
        {
            Null,
            FadingIn,
            FadingOut,
            Ducking
        }

        public EmitterChannel channel               = EmitterChannel.Reserved;
        public bool           disableSpatialization = false;

        [NonSerialized]
        [HideInInspector]
        public AudioSource audioSource = null;

        [NonSerialized]
        [HideInInspector]
        public Transform defaultParent = null;

        [NonSerialized]
        [HideInInspector]
        public float defaultVolume = 1.0f;

        [NonSerialized]
        [HideInInspector]
        public float endPlayTime = 0.0f;

        private Transform lastParentTransform = null;

        [NonSerialized]
        [HideInInspector]
        public Action onFinished = null;

        [NonSerialized]
        [HideInInspector]
        public Action<object> onFinishedObject = null;

        [NonSerialized]
        [HideInInspector]
        public object onFinishedParam;

        [NonSerialized]
        [HideInInspector]
        public int originalIdx = -1;

        [NonSerialized]
        [HideInInspector]
        public ONSPAudioSource osp = null;

        [NonSerialized]
        [HideInInspector]
        public SoundGroup playingSoundGroup = null;

        [NonSerialized]
        [HideInInspector]
        public SoundPriority priority = SoundPriority.Default;

        private FadeState state = FadeState.Null;

        // OPTIMIZE

        public float volume
        {
            get => audioSource.volume;
            set => audioSource.volume = value;
        }

        public float pitch
        {
            get => audioSource.pitch;
            set => audioSource.pitch = value;
        }

        public AudioClip clip
        {
            get => audioSource.clip;
            set => audioSource.clip = value;
        }

        public float time
        {
            get => audioSource.time;
            set => audioSource.time = value;
        }

        public float length => (audioSource.clip != null) ? audioSource.clip.length : 0.0f;

        public bool loop
        {
            get => audioSource.loop;
            set => audioSource.loop = value;
        }

        public bool mute
        {
            get => audioSource.mute;
            set => audioSource.mute = value;
        }

        public AudioVelocityUpdateMode velocityUpdateMode
        {
            get => audioSource.velocityUpdateMode;
            set => audioSource.velocityUpdateMode = value;
        }

        public bool isPlaying => audioSource.isPlaying;

        /*
        -----------------------
        Awake()
        -----------------------
        */
        private void Awake()
        {
            // unity defaults to 'playOnAwake = true'
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // is the spatialized audio enabled?
            if (AudioManager.enableSpatialization && !disableSpatialization)
            {
                osp = GetComponent<ONSPAudioSource>();
                if (osp == null)
                {
                    osp = gameObject.AddComponent<ONSPAudioSource>();
                }
            }

            audioSource.playOnAwake = false;
            audioSource.Stop();
        }

        /*
        -----------------------
        SetPlayingSoundGroup()
        -----------------------
        */
        public void SetPlayingSoundGroup(SoundGroup soundGroup)
        {
            playingSoundGroup = soundGroup;
            if (soundGroup != null)
            {
                soundGroup.IncrementPlayCount();
            }
        }

        /*
        -----------------------
        SetOnFinished()
        -----------------------
        */
        public void SetOnFinished(Action onFinished)
        {
            this.onFinished = onFinished;
        }

        /*
        -----------------------
        SetOnFinished()
        -----------------------
        */
        public void SetOnFinished(Action<object> onFinished, object obj)
        {
            onFinishedObject = onFinished;
            onFinishedParam  = obj;
        }

        /*
        -----------------------
        SetChannel()
        -----------------------
        */
        public void SetChannel(int _channel)
        {
            channel = (EmitterChannel) _channel;
        }

        /*
        -----------------------
        SetDefaultParent()
        -----------------------
        */
        public void SetDefaultParent(Transform parent)
        {
            defaultParent = parent;
        }

        /*
        -----------------------
        SetAudioMixer()
        -----------------------
        */
        public void SetAudioMixer(AudioMixerGroup _mixer)
        {
            if (audioSource != null)
            {
                audioSource.outputAudioMixerGroup = _mixer;
            }
        }

        /*
        -----------------------
        IsPlaying()
        -----------------------
        */
        public bool IsPlaying()
        {
            if (loop && audioSource.isPlaying)
            {
                return true;
            }

            return endPlayTime > Time.time;
        }

        /*
        -----------------------
        Play()
        -----------------------
        */
        public void Play()
        {
            // overrides everything
            state       = FadeState.Null;
            endPlayTime = Time.time + length;
            StopAllCoroutines();
            audioSource.Play();
        }

        /*
        -----------------------
        Pause()
        -----------------------
        */
        public void Pause()
        {
            // overrides everything
            state = FadeState.Null;
            StopAllCoroutines();
            audioSource.Pause();
        }

        /*
        -----------------------
        Stop()
        -----------------------
        */
        public void Stop()
        {
            // overrides everything
            state = FadeState.Null;
            StopAllCoroutines();
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            if (onFinished != null)
            {
                onFinished();
                onFinished = null;
            }

            if (onFinishedObject != null)
            {
                onFinishedObject(onFinishedParam);
                onFinishedObject = null;
            }

            if (playingSoundGroup != null)
            {
                playingSoundGroup.DecrementPlayCount();
                playingSoundGroup = null;
            }
        }

        /*
        -----------------------
        GetSampleTime()
        -----------------------
        */
        private int GetSampleTime() => audioSource.clip.samples - audioSource.timeSamples;

        /*
        -----------------------
        ParentTo()
        -----------------------
        */
        public void ParentTo(Transform parent)
        {
            if (lastParentTransform != null)
            {
                Debug.LogError("[SoundEmitter] You must detach the sound emitter before parenting to another object!");
                return;
            }

            lastParentTransform = transform.parent;
            transform.parent    = parent;
        }

        /*
        -----------------------
        DetachFromParent()
        -----------------------
        */
        public void DetachFromParent()
        {
            if (lastParentTransform == null)
            {
                transform.parent = defaultParent;
                return;
            }

            transform.parent    = lastParentTransform;
            lastParentTransform = null;
        }

        /*
        -----------------------
        ResetParent()
        -----------------------
        */
        public void ResetParent(Transform parent)
        {
            transform.parent    = parent;
            lastParentTransform = null;
        }

        /*
        -----------------------
        SyncTo()
        -----------------------
        */
        public void SyncTo(SoundEmitter other, float fadeTime, float toVolume)
        {
            StartCoroutine(DelayedSyncTo(other, fadeTime, toVolume));
        }

        /*
        -----------------------
        DelayedSyncTo()
        have to wait until the end of frame to do proper sync'ing
        -----------------------
        */
        private IEnumerator DelayedSyncTo(SoundEmitter other, float fadeTime, float toVolume)
        {
            yield return new WaitForEndOfFrame();

            //audio.timeSamples = other.GetSampleTime();
            //audio.time = Mathf.Min( Mathf.Max( 0.0f, other.time - other.length ), other.time );
            audioSource.time = other.time;
            audioSource.Play();
            FadeTo(fadeTime, toVolume);
        }

        /*
        -----------------------
        FadeTo()
        -----------------------
        */
        public void FadeTo(float fadeTime, float toVolume)
        {
            //Log.Print( ">>> FADE TO: " + channel );

            // don't override a fade out 
            if (state == FadeState.FadingOut)
            {
                //Log.Print( "    ....ABORTED" );
                return;
            }

            state = FadeState.Ducking;
            StopAllCoroutines();
            StartCoroutine(FadeSoundChannelTo(fadeTime, toVolume));
        }

        /*
        -----------------------
        FadeIn()
        -----------------------
        */
        public void FadeIn(float fadeTime, float defaultVolume)
        {
            //Log.Print( ">>> FADE IN: " + channel );
            audioSource.volume = 0.0f;
            state              = FadeState.FadingIn;
            StopAllCoroutines();
            StartCoroutine(FadeSoundChannel(0.0f, fadeTime, Fade.In, defaultVolume));
        }

        /*
        -----------------------
        FadeIn()
        -----------------------
        */
        public void FadeIn(float fadeTime)
        {
            //Log.Print( ">>> FADE IN: " + channel );
            audioSource.volume = 0.0f;
            state              = FadeState.FadingIn;
            StopAllCoroutines();
            StartCoroutine(FadeSoundChannel(0.0f, fadeTime, Fade.In, defaultVolume));
        }

        /*
        -----------------------
        FadeOut()
        -----------------------
        */
        public void FadeOut(float fadeTime)
        {
            //Log.Print( ">>> FADE OUT: " + channel );
            if (!audioSource.isPlaying)
            {
                //Log.Print( "   ... SKIPPING" );
                return;
            }

            state = FadeState.FadingOut;
            StopAllCoroutines();
            StartCoroutine(FadeSoundChannel(0.0f, fadeTime, Fade.Out, audioSource.volume));
        }

        /*
        -----------------------
        FadeOutDelayed()
        -----------------------
        */
        public void FadeOutDelayed(float delayedSecs, float fadeTime)
        {
            //Log.Print( ">>> FADE OUT DELAYED: " + channel );
            if (!audioSource.isPlaying)
            {
                //Log.Print( "   ... SKIPPING" );
                return;
            }

            state = FadeState.FadingOut;
            StopAllCoroutines();
            StartCoroutine(FadeSoundChannel(delayedSecs, fadeTime, Fade.Out, audioSource.volume));
        }

        /*
        -----------------------
        FadeSoundChannelTo()
        -----------------------
        */
        private IEnumerator FadeSoundChannelTo(float fadeTime, float toVolume)
        {
            var start       = audioSource.volume;
            var end         = toVolume;
            var startTime   = Time.realtimeSinceStartup;
            var elapsedTime = 0.0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime = Time.realtimeSinceStartup - startTime;
                var t = elapsedTime / fadeTime;
                audioSource.volume = Mathf.Lerp(start, end, t);
                yield return 0;
            }

            state = FadeState.Null;
        }

        /*
        -----------------------
        FadeSoundChannel()
        -----------------------
        */
        private IEnumerator FadeSoundChannel(float delaySecs, float fadeTime, Fade fadeType, float defaultVolume)
        {
            if (delaySecs > 0.0f)
            {
                yield return new WaitForSeconds(delaySecs);
            }

            var start       = (fadeType == Fade.In) ? 0.0f : defaultVolume;
            var end         = (fadeType == Fade.In) ? defaultVolume : 0.0f;
            var restartPlay = false;

            if (fadeType == Fade.In)
            {
                if (Time.time == 0.0f)
                {
                    restartPlay = true;
                }

                audioSource.volume = 0.0f;
                audioSource.Play();
            }

            var startTime   = Time.realtimeSinceStartup;
            var elapsedTime = 0.0f;

            while (elapsedTime < fadeTime)
            {
                elapsedTime = Time.realtimeSinceStartup - startTime;
                var t = elapsedTime / fadeTime;
                audioSource.volume = Mathf.Lerp(start, end, t);
                yield return 0;

                if (restartPlay && (Time.time > 0.0f))
                {
                    audioSource.Play();
                    restartPlay = false;
                }

                if (!audioSource.isPlaying)
                {
                    break;
                }
            }

            if (fadeType == Fade.Out)
            {
                Stop();
            }

            state = FadeState.Null;
        }
    }
} // namespace OVR
