// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Visual of finger tip poke tool.
/// </summary>
namespace OculusSampleFramework
{
    public class FingerTipPokeToolView : MonoBehaviour, InteractableToolView
    {
        [SerializeField] private MeshRenderer _sphereMeshRenderer = null;

        public float SphereRadius { get; private set; }

        private void Awake()
        {
            Assert.IsNotNull(_sphereMeshRenderer);
            SphereRadius = _sphereMeshRenderer.transform.localScale.z * 0.5f;
        }

        public InteractableTool InteractableTool { get; set; }

        public bool EnableState
        {
            get => _sphereMeshRenderer.enabled;
            set => _sphereMeshRenderer.enabled = value;
        }

        public bool ToolActivateState { get; set; }

        public void SetFocusedInteractable(Interactable interactable)
        {
            // nothing to see here
        }
    }
}
