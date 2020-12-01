// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Audio;
//#if UNITY_EDITOR
using UnityEditor;

using System;
using System.Reflection;

//#endif

namespace OVR
{
    public enum PreloadSounds
    {
        Default,      // default unity behavior
        Preload,      // audio clips are forced to preload
        ManualPreload // audio clips are forced to not preload, preloading must be done manually
    }

    public enum Fade
    {
        In,
        Out
    }

    [Serializable]
    public class SoundGroup
    {
        public string          name       = string.Empty;
        public SoundFX[]       soundList  = new SoundFX[0];
        public AudioMixerGroup mixerGroup = null; // default = AudioManager.defaultMixerGroup

        [Range(0, 64)]
        public int maxPlayingSounds = 0; // default = 0, unlimited

        // TODO: this preload behavior is not yet implemented
        public PreloadSounds preloadAudio   = PreloadSounds.Default; // default = true, audio clip data will be preloaded
        public float         volumeOverride = 1.0f;                  // default = 1.0

        [HideInInspector]
        public int playingSoundCount = 0;

        public SoundGroup(string name)
        {
            this.name = name;
        }

        public SoundGroup()
        {
            mixerGroup       = null;
            maxPlayingSounds = 0;
            preloadAudio     = PreloadSounds.Default;
            volumeOverride   = 1.0f;
        }

        public void IncrementPlayCount()
        {
            playingSoundCount = Mathf.Clamp(++playingSoundCount, 0, maxPlayingSounds);
        }

        public void DecrementPlayCount()
        {
            playingSoundCount = Mathf.Clamp(--playingSoundCount, 0, maxPlayingSounds);
        }

        public bool CanPlaySound() => (maxPlayingSounds == 0) || (playingSoundCount < maxPlayingSounds);
    }

    /*
    -----------------------
    
     AudioManager
    
    -----------------------
    */
    public partial class AudioManager : MonoBehaviour
    {
        private static FastList<string> names        = new FastList<string>();
        private static string[]         defaultSound = new string[1] {"Default Sound"};
        private static SoundFX          nullSound    = new SoundFX();
        private static bool             hideWarnings = false;

        [Tooltip("Make the audio manager persistent across all scene loads")]
        public bool makePersistent = true; // true = don't destroy on load

        [Tooltip("Enable the OSP audio plugin features")]
        public bool enableSpatializedAudio = true; // true = enable spatialized audio

        [Tooltip("Always play spatialized sounds with no reflections (Default)")]
        public bool enableSpatializedFastOverride = false; // true = disable spatialized reflections override

        [Tooltip("The audio mixer asset used for snapshot blends, etc.")]
        public AudioMixer audioMixer = null;

        [Tooltip("The audio mixer group used for the pooled emitters")]
        public AudioMixerGroup defaultMixerGroup = null;

        [Tooltip("The audio mixer group used for the reserved pool emitter")]
        public AudioMixerGroup reservedMixerGroup = null;

        [Tooltip("The audio mixer group used for voice chat")]
        public AudioMixerGroup voiceChatMixerGroup = null;

        [Tooltip("Log all PlaySound calls to the Unity console")]
        public bool verboseLogging = false; // true = log all PlaySounds

        [Tooltip("Maximum sound emitters")]
        public int maxSoundEmitters = 32; // total number of sound emitters created

        [Tooltip("Default volume for all sounds modulated by individual sound FX volumes")]
        public float volumeSoundFX = 1.0f; // user pref: volume of all sound FX

        [Tooltip("Sound FX fade time")]
        public float soundFxFadeSecs = 1.0f; // sound FX fade time

        public float audioMinFallOffDistance = 1.0f;  // minimum falloff distance
        public float audioMaxFallOffDistance = 25.0f; // maximum falloff distance

        public SoundGroup[] soundGroupings = new SoundGroup[0];

        private       Dictionary<string, SoundFX> soundFXCache = null;
        public static bool                        enableSpatialization => (Instance != null) ? Instance.enableSpatializedAudio : false;

        public static AudioManager Instance { get; private set; } = null;

        public static float           NearFallOff   => Instance.audioMinFallOffDistance;
        public static float           FarFallOff    => Instance.audioMaxFallOffDistance;
        public static AudioMixerGroup EmitterGroup  => Instance.defaultMixerGroup;
        public static AudioMixerGroup ReservedGroup => Instance.reservedMixerGroup;
        public static AudioMixerGroup VoipGroup     => Instance.voiceChatMixerGroup;

        /*
        -----------------------
        Awake()
        -----------------------
        */
        private void Awake()
        {
            Init();
        }

        /*
        -----------------------
        Update()
        -----------------------
        */
        private void Update()
        {
            // update the free and playing lists
            UpdateFreeEmitters();
        }

        /*
        -----------------------
        OnDestroy()
        -----------------------
        */
        private void OnDestroy()
        {
            // we only want the initialized audio manager instance cleaning up the sound emitters
            if (Instance == this)
            {
                if (soundEmitterParent != null)
                {
                    Destroy(soundEmitterParent);
                }
            }

            ///TODO - if you change scenes you'll want to call OnPreSceneLoad to detach the sound emitters
            ///from anything they might be parented to or they will get destroyed with that object
            ///there should only be one instance of the AudioManager across the life of the game/app
            ///GameManager.OnPreSceneLoad -= OnPreSceneLoad;
        }

        /*
        -----------------------
        Init()
        -----------------------
        */
        private void Init()
        {
            if (Instance != null)
            {
                if (Application.isPlaying && (Instance != this))
                {
                    enabled = false;
                }

                return;
            }

            Instance = this;

            ///TODO - if you change scenes you'll want to call OnPreSceneLoad to detach the sound emitters
            ///from anything they might be parented to or they will get destroyed with that object
            ///there should only be one instance of the AudioManager across the life of the game/app
            ///GameManager.OnPreSceneLoad += OnPreSceneLoad;

            // make sure the first one is a null sound
            nullSound.name = "Default Sound";

            // build the sound FX cache
            RebuildSoundFXCache();

            // create the sound emitters
            if (Application.isPlaying)
            {
                InitializeSoundSystem();
                if (makePersistent && (transform.parent == null))
                {
                    // don't destroy the audio manager on scene loads
                    DontDestroyOnLoad(gameObject);
                }
            }

            #if UNITY_EDITOR
            Debug.Log("[AudioManager] Initialized...");
            #endif
        }

        /*
        -----------------------
        RebuildSoundFXCache()
        -----------------------
        */
        private void RebuildSoundFXCache()
        {
            // build the SoundFX dictionary for quick name lookups
            var count = 0;
            for (var group = 0; group < soundGroupings.Length; group++)
            {
                count += soundGroupings[group].soundList.Length;
            }

            soundFXCache = new Dictionary<string, SoundFX>(count + 1);
            // add the null sound
            soundFXCache.Add(nullSound.name, nullSound);
            // add the rest
            for (var group = 0; group < soundGroupings.Length; group++)
            {
                for (var i = 0; i < soundGroupings[group].soundList.Length; i++)
                {
                    if (soundFXCache.ContainsKey(soundGroupings[group].soundList[i].name))
                    {
                        Debug.LogError("ERROR: Duplicate Sound FX name in the audio manager: '" + soundGroupings[group].name + "' > '" + soundGroupings[group].soundList[i].name + "'");
                    }
                    else
                    {
                        soundGroupings[group].soundList[i].Group = soundGroupings[group];
                        soundFXCache.Add(soundGroupings[group].soundList[i].name, soundGroupings[group].soundList[i]);
                    }
                }

                soundGroupings[group].playingSoundCount = 0;
            }
        }

        /*
        -----------------------
        FindSoundFX()
        -----------------------
        */
        public static SoundFX FindSoundFX(string name, bool rebuildCache = false)
        {
            #if UNITY_EDITOR
            if (Instance == null)
            {
                Debug.LogError("ERROR: audio manager not yet initialized or created!" + " Time: " + Time.time);
                return null;
            }
            #endif
            if (string.IsNullOrEmpty(name))
            {
                return nullSound;
            }

            if (rebuildCache)
            {
                Instance.RebuildSoundFXCache();
            }

            if (!Instance.soundFXCache.ContainsKey(name))
            {
                #if DEBUG_BUILD || UNITY_EDITOR
                Debug.LogError("WARNING: Missing Sound FX in cache: " + name);
                #endif
                return nullSound;
            }

            return Instance.soundFXCache[name];
        }

        /*
        -----------------------
        FindAudioManager()
        -----------------------
        */
        private static bool FindAudioManager()
        {
            var audioManagerObject = GameObject.Find("AudioManager");
            if ((audioManagerObject == null) || (audioManagerObject.GetComponent<AudioManager>() == null))
            {
                if (!hideWarnings)
                {
                    Debug.LogError("[ERROR] AudioManager object missing from hierarchy!");
                    hideWarnings = true;
                }

                return false;
            }

            audioManagerObject.GetComponent<AudioManager>().Init();
            return true;
        }

        /*
        -----------------------
        GetGameObject()
        -----------------------
        */
        public static GameObject GetGameObject()
        {
            if (Instance == null)
            {
                if (!FindAudioManager())
                {
                    return null;
                }
            }

            return Instance.gameObject;
        }

        /*
        -----------------------
        NameMinusGroup()
        strip off the sound group from the inspector dropdown
        -----------------------
        */
        public static string NameMinusGroup(string name)
        {
            if (name.IndexOf("/") > -1)
            {
                return name.Substring(name.IndexOf("/") + 1);
            }

            return name;
        }

        /*
        -----------------------
        GetSoundFXNames()
        used by the inspector
        -----------------------
        */
        public static string[] GetSoundFXNames(string currentValue, out int currentIdx)
        {
            currentIdx = 0;
            names.Clear();
            if (Instance == null)
            {
                if (!FindAudioManager())
                {
                    return defaultSound;
                }
            }

            names.Add(nullSound.name);
            for (var group = 0; group < Instance.soundGroupings.Length; group++)
            {
                for (var i = 0; i < Instance.soundGroupings[group].soundList.Length; i++)
                {
                    if (string.Compare(currentValue, Instance.soundGroupings[group].soundList[i].name, true) == 0)
                    {
                        currentIdx = names.Count;
                    }

                    names.Add(Instance.soundGroupings[group].name + "/" + Instance.soundGroupings[group].soundList[i].name);
                }
            }

            //names.Sort( delegate( string s1, string s2 ) { return s1.CompareTo( s2 ); } );
            return names.ToArray();
        }
        #if UNITY_EDITOR
        /*
        -----------------------
        OnPrefabReimported()
        -----------------------
        */
        public static void OnPrefabReimported()
        {
            if (Instance != null)
            {
                Debug.Log("[AudioManager] Reimporting the sound FX cache.");
                Instance.RebuildSoundFXCache();
            }
        }

        /*
        -----------------------
        PlaySound()
        used in the editor
        -----------------------
        */
        public static void PlaySound(string soundFxName)
        {
            if (Instance == null)
            {
                if (!FindAudioManager())
                {
                    return;
                }
            }

            var soundFX = FindSoundFX(soundFxName, true);
            if (soundFX == null)
            {
                return;
            }

            var clip = soundFX.GetClip();
            if (clip != null)
            {
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtilClass      = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                var method = audioUtilClass.GetMethod(
                        "PlayClip",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new[] {typeof(AudioClip)},
                        null);
                method.Invoke(null, new object[] {clip});
            }
        }

        /*
        -----------------------
        IsSoundPlaying()
        used in the editor
        -----------------------
        */
        public static bool IsSoundPlaying(string soundFxName)
        {
            if (Instance == null)
            {
                if (!FindAudioManager())
                {
                    return false;
                }
            }

            var soundFX = FindSoundFX(soundFxName, true);
            if (soundFX == null)
            {
                return false;
            }

            var clip = soundFX.GetClip();
            if (clip != null)
            {
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtilClass      = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                var method = audioUtilClass.GetMethod(
                        "IsClipPlaying",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new[] {typeof(AudioClip)},
                        null);
                return Convert.ToBoolean(method.Invoke(null, new object[] {clip}));
            }

            return false;
        }

        /*
        -----------------------
        StopSound()
        used in the editor
        -----------------------
        */
        public static void StopSound(string soundFxName)
        {
            if (Instance == null)
            {
                if (!FindAudioManager())
                {
                    return;
                }
            }

            var soundFX = FindSoundFX(soundFxName, true);
            if (soundFX == null)
            {
                return;
            }

            var clip = soundFX.GetClip();
            if (clip != null)
            {
                var unityEditorAssembly = typeof(AudioImporter).Assembly;
                var audioUtilClass      = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                var method = audioUtilClass.GetMethod(
                        "StopClip",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new[] {typeof(AudioClip)},
                        null);
                method.Invoke(null, new object[] {clip});
            }
        }
        #endif
    }
} // namespace OVR
