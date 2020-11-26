// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonDownListener : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        if (onButtonDown != null)
        {
            onButtonDown.Invoke();
        }
    }

    public event Action onButtonDown;
}
