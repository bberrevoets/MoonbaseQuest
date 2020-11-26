// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 23/11/2020
// ==========================================================================

using UnityEngine;

public class PointCanvasControl : MonoBehaviour
{
    private void Start()
    {
        // destroy the canvas after time
        Destroy(gameObject, 3);
    }

    private void Update()
    {
        // Control the rotation of the panel
        if (!(Camera.main is null))
        {
            transform.LookAt(Camera.main.transform);
        }
    }
}
