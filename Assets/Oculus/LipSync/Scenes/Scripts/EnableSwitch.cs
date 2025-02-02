// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class EnableSwitch : MonoBehaviour
{
    public GameObject[] SwitchTargets;

    /// <summary>
    ///     Sets the active GameObject
    /// </summary>
    /// <returns><c>true</c>, if active was set, <c>false</c> otherwise.</returns>
    /// <param name="target">Target.</param>
    public bool SetActive<T>(int target) where T : MonoBehaviour
    {
        if ((target < 0) || (target >= SwitchTargets.Length))
        {
            return false;
        }

        for (var i = 0; i < SwitchTargets.Length; i++)
        {
            SwitchTargets[i].SetActive(false);

            // Disable texture flip or morph target
            var lipsyncContextMorph =
                    SwitchTargets[i].GetComponent<OVRLipSyncContextMorphTarget>();
            if (lipsyncContextMorph)
            {
                lipsyncContextMorph.enabled = false;
            }

            var lipsyncContextTexture =
                    SwitchTargets[i].GetComponent<OVRLipSyncContextTextureFlip>();
            if (lipsyncContextTexture)
            {
                lipsyncContextTexture.enabled = false;
            }
        }

        SwitchTargets[target].SetActive(true);
        MonoBehaviour lipsyncContext = SwitchTargets[target].GetComponent<T>();
        if (lipsyncContext != null)
        {
            lipsyncContext.enabled = true;
        }

        return true;
    }
}
