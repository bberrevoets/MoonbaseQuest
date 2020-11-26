// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    public class CowController : MonoBehaviour
    {
        [SerializeField] private Animation   _cowAnimation      = null;
        [SerializeField] private AudioSource _mooCowAudioSource = null;

        private void Start()
        {
            Assert.IsNotNull(_cowAnimation);
            Assert.IsNotNull(_mooCowAudioSource);
        }

        public void PlayMooSound()
        {
            _mooCowAudioSource.timeSamples = 0;
            _mooCowAudioSource.Play();
        }

        public void GoMooCowGo()
        {
            _cowAnimation.Rewind();
            _cowAnimation.Play();
        }
    }
}
