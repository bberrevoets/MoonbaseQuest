// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OVR
{
    /*
    -----------------------
     
     AmbienceEmitter()
     
    -----------------------
    */
    public class AmbienceEmitter : MonoBehaviour
    {
        public SoundFXRef[] ambientSounds = new SoundFXRef[0];
        public bool         autoActivate  = true;

        [Tooltip("Automatically play the sound randomly again when checked.  Should be OFF for looping sounds")]
        public bool autoRetrigger = true;

        [MinMax(2.0f, 4.0f, 0.1f, 10.0f)]
        public Vector2 randomRetriggerDelaySecs = new Vector2(2.0f, 4.0f);

        [Tooltip("If defined, the sounds will randomly play from these transform positions, otherwise the sound will play from this transform")]
        public Transform[] playPositions = new Transform[0];

        private bool  activated    = false;
        private float fadeTime     = 0.25f;
        private int   lastPosIdx   = -1;
        private float nextPlayTime = 0.0f;
        private int   playingIdx   = -1;

        /*
        -----------------------
        Awake()
        -----------------------
        */
        private void Awake()
        {
            if (autoActivate)
            {
                activated    = true;
                nextPlayTime = Time.time + Random.Range(randomRetriggerDelaySecs.x, randomRetriggerDelaySecs.y);
            }

            // verify all the play positions are valid
            foreach (var t in playPositions)
            {
                if (t == null)
                {
                    Debug.LogWarning("[AmbienceEmitter] Invalid play positions in " + name);
                    playPositions = new Transform[0];
                    break;
                }
            }
        }

        /*
        -----------------------
        Update()
        -----------------------
        */
        private void Update()
        {
            if (activated)
            {
                if ((playingIdx == -1) || autoRetrigger)
                {
                    if (Time.time >= nextPlayTime)
                    {
                        Play();
                        if (!autoRetrigger)
                        {
                            activated = false;
                        }
                    }
                }
            }
        }

        /*
        -----------------------
        OnTriggerEnter()
        -----------------------
        */
        public void OnTriggerEnter(Collider col)
        {
            activated = !activated;
        }

        /*
        -----------------------
        Play()
        -----------------------
        */
        public void Play()
        {
            var transformToPlayFrom = transform;
            if (playPositions.Length > 0)
            {
                var idx = Random.Range(0, playPositions.Length);
                while ((playPositions.Length > 1) && (idx == lastPosIdx))
                {
                    idx = Random.Range(0, playPositions.Length);
                }

                transformToPlayFrom = playPositions[idx];
                lastPosIdx          = idx;
            }

            playingIdx = ambientSounds[Random.Range(0, ambientSounds.Length)].PlaySoundAt(transformToPlayFrom.position);
            if (playingIdx != -1)
            {
                AudioManager.FadeInSound(playingIdx, fadeTime);
                nextPlayTime = Time.time + Random.Range(randomRetriggerDelaySecs.x, randomRetriggerDelaySecs.y);
            }
        }

        /*
        -----------------------
        EnableEmitter()
        -----------------------
        */
        public void EnableEmitter(bool enable)
        {
            activated = enable;
            if (enable)
            {
                Play();
            }
            else
            {
                if (playingIdx != -1)
                {
                    AudioManager.FadeOutSound(playingIdx, fadeTime);
                }
            }
        }
    }
} // namespace OVR
