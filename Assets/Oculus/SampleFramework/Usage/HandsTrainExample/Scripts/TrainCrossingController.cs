// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections;

using UnityEngine;
using UnityEngine.Assertions;

namespace OculusSampleFramework
{
    public class TrainCrossingController : MonoBehaviour
    {
        [SerializeField] private AudioSource       _audioSource        = null;
        [SerializeField] private AudioClip[]       _crossingSounds     = null;
        [SerializeField] private MeshRenderer      _lightSide1Renderer = null;
        [SerializeField] private MeshRenderer      _lightSide2Renderer = null;
        [SerializeField] private SelectionCylinder _selectionCylinder  = null;
        private                  int               _colorId            = Shader.PropertyToID("_Color");

        private Material         _lightsSide1Mat;
        private Material         _lightsSide2Mat;
        private InteractableTool _toolInteractingWithMe = null;

        private Coroutine _xingAnimationCr = null;

        private void Awake()
        {
            Assert.IsNotNull(_audioSource);
            Assert.IsNotNull(_crossingSounds);
            Assert.IsNotNull(_lightSide1Renderer);
            Assert.IsNotNull(_lightSide2Renderer);
            Assert.IsNotNull(_selectionCylinder);

            _lightsSide1Mat = _lightSide1Renderer.material;
            _lightsSide2Mat = _lightSide2Renderer.material;
        }

        private void Update()
        {
            if (_toolInteractingWithMe == null)
            {
                _selectionCylinder.CurrSelectionState = SelectionCylinder.SelectionState.Off;
            }
            else
            {
                _selectionCylinder.CurrSelectionState = (
                                                            _toolInteractingWithMe.ToolInputState == ToolInputState.PrimaryInputDown ||
                                                            _toolInteractingWithMe.ToolInputState == ToolInputState.PrimaryInputDownStay)
                                                                ? SelectionCylinder.SelectionState.Highlighted
                                                                : SelectionCylinder.SelectionState.Selected;
            }
        }

        private void OnDestroy()
        {
            if (_lightsSide1Mat != null)
            {
                Destroy(_lightsSide1Mat);
            }

            if (_lightsSide2Mat != null)
            {
                Destroy(_lightsSide2Mat);
            }
        }

        public void CrossingButtonStateChanged(InteractableStateArgs obj)
        {
            var inActionState = obj.NewInteractableState == InteractableState.ActionState;
            if (inActionState)
            {
                ActivateTrainCrossing();
            }

            _toolInteractingWithMe = obj.NewInteractableState > InteractableState.Default ? obj.Tool : null;
        }

        private void ActivateTrainCrossing()
        {
            var maxSoundIndex = _crossingSounds.Length - 1;
            var audioClip     = _crossingSounds[(int) (Random.value * maxSoundIndex)];
            _audioSource.clip        = audioClip;
            _audioSource.timeSamples = 0;
            _audioSource.Play();
            if (_xingAnimationCr != null)
            {
                StopCoroutine(_xingAnimationCr);
            }

            _xingAnimationCr = StartCoroutine(AnimateCrossing(audioClip.length * 0.75f));
        }

        private IEnumerator AnimateCrossing(float animationLength)
        {
            ToggleLightObjects(true);

            var animationEndTime = Time.time + animationLength;

            var lightBlinkDuration  = animationLength * 0.1f;
            var lightBlinkStartTime = Time.time;
            var lightBlinkEndTime   = Time.time + lightBlinkDuration;
            var lightToBlinkOn      = _lightsSide1Mat;
            var lightToBlinkOff     = _lightsSide2Mat;
            var onColor             = new Color(1.0f, 1.0f, 1.0f, 1.0f);
            var offColor            = new Color(1.0f, 1.0f, 1.0f, 0.0f);

            while (Time.time < animationEndTime)
            {
                var t = (Time.time - lightBlinkStartTime) / lightBlinkDuration;
                lightToBlinkOn.SetColor(_colorId, Color.Lerp(offColor, onColor,  t));
                lightToBlinkOff.SetColor(_colorId, Color.Lerp(onColor, offColor, t));

                // switch which lights blink on and off when time runs out
                if (Time.time > lightBlinkEndTime)
                {
                    var temp = lightToBlinkOn;
                    lightToBlinkOn      = lightToBlinkOff;
                    lightToBlinkOff     = temp;
                    lightBlinkStartTime = Time.time;
                    lightBlinkEndTime   = Time.time + lightBlinkDuration;
                }

                yield return null;
            }

            ToggleLightObjects(false);
        }

        private void AffectMaterials(Material[] materials, Color newColor)
        {
            foreach (var material in materials)
            {
                material.SetColor(_colorId, newColor);
            }
        }

        private void ToggleLightObjects(bool enableState)
        {
            _lightSide1Renderer.gameObject.SetActive(enableState);
            _lightSide2Renderer.gameObject.SetActive(enableState);
        }
    }
}
