// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

namespace OculusSampleFramework
{
    public class SelectionCylinder : MonoBehaviour
    {
        public enum SelectionState
        {
            Off = 0,
            Selected,
            Highlighted
        }

        private static int _colorId = Shader.PropertyToID("_Color");

        [SerializeField] private MeshRenderer _selectionMeshRenderer = null;

        private SelectionState _currSelectionState     = SelectionState.Off;
        private Color[]        _defaultSelectionColors = null, _highlightColors = null;
        private Material[]     _selectionMaterials;

        public SelectionState CurrSelectionState
        {
            get => _currSelectionState;
            set
            {
                var oldState = _currSelectionState;
                _currSelectionState = value;

                if (oldState != _currSelectionState)
                {
                    if (_currSelectionState > SelectionState.Off)
                    {
                        _selectionMeshRenderer.enabled = true;
                        AffectSelectionColor(_currSelectionState == SelectionState.Selected
                                                     ? _defaultSelectionColors
                                                     : _highlightColors);
                    }
                    else
                    {
                        _selectionMeshRenderer.enabled = false;
                    }
                }
            }
        }

        private void Awake()
        {
            _selectionMaterials = _selectionMeshRenderer.materials;
            var numColors = _selectionMaterials.Length;
            _defaultSelectionColors = new Color[numColors];
            _highlightColors        = new Color[numColors];
            for (var i = 0; i < numColors; i++)
            {
                _defaultSelectionColors[i] = _selectionMaterials[i].GetColor(_colorId);
                _highlightColors[i]        = new Color(1.0f, 1.0f, 1.0f, _defaultSelectionColors[i].a);
            }

            CurrSelectionState = SelectionState.Off;
        }

        private void OnDestroy()
        {
            if (_selectionMaterials != null)
            {
                foreach (var selectionMaterial in _selectionMaterials)
                {
                    if (selectionMaterial != null)
                    {
                        Destroy(selectionMaterial);
                    }
                }
            }
        }

        private void AffectSelectionColor(Color[] newColors)
        {
            var numColors = newColors.Length;
            for (var i = 0; i < numColors; i++)
            {
                _selectionMaterials[i].SetColor(_colorId, newColors[i]);
            }
        }
    }
}
