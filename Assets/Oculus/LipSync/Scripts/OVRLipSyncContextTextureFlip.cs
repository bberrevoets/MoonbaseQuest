// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class OVRLipSyncContextTextureFlip : MonoBehaviour
{
    // PUBLIC

    // Manually assign the material
    public Material material = null;

    // Set the textures for each viseme. We should follow the viseme order as specified
    // by the Phoneme list
    [Tooltip("The texture used for each viseme.")]
    [OVRNamedArray(new[]
    {
            "sil", "PP", "FF", "TH", "DD", "kk", "CH",
            "SS", "nn", "RR", "aa", "E", "ih", "oh", "ou"
    })]
    public Texture[] Textures = new Texture[OVRLipSync.VisemeCount];

    // smoothing amount
    [Range(1, 100)]
    [Tooltip("Smoothing of 1 will yield only the current predicted viseme," +
             "100 will yield an extremely smooth viseme response.")]
    public int smoothAmount = 70;

    // PRIVATE

    // Look for a Phoneme Context (should be set at the same level as this component)
    private OVRLipSyncContextBase lipsyncContext = null;

    // Capture the old viseme frame (we will write back into this one)
    private OVRLipSync.Frame oldFrame = new OVRLipSync.Frame();

    /// <summary>
    ///     Start this instance.
    /// </summary>
    private void Start()
    {
        // make sure there is a phoneme context assigned to this object
        lipsyncContext = GetComponent<OVRLipSyncContextBase>();
        if (lipsyncContext == null)
        {
            Debug.LogWarning("LipSyncContextTextureFlip.Start WARNING:" +
                             " No lip sync context component set to object");
        }
        else
        {
            // Send smoothing amount to context
            lipsyncContext.Smoothing = smoothAmount;
        }

        if (material == null)
        {
            Debug.LogWarning("LipSyncContextTextureFlip.Start WARNING:" +
                             " Lip sync context texture flip has no material target to control!");
        }
    }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update()
    {
        if ((lipsyncContext != null) && (material != null))
        {
            // trap inputs and send signals to phoneme engine for testing purposes

            // get the current viseme frame
            var frame = lipsyncContext.GetCurrentPhonemeFrame();
            if (frame != null)
            {
                // Perform smoothing here if on original provider
                if (lipsyncContext.provider == OVRLipSync.ContextProviders.Original)
                {
                    // Go through the current and old
                    for (var i = 0; i < frame.Visemes.Length; i++)
                    {
                        // Convert 1-100 to old * (0.00 - 0.99)
                        var smoothing = ((smoothAmount - 1) / 100.0f);
                        oldFrame.Visemes[i] =
                                oldFrame.Visemes[i] * smoothing +
                                frame.Visemes[i] * (1.0f - smoothing);
                    }
                }
                else
                {
                    oldFrame.Visemes = frame.Visemes;
                }

                SetVisemeToTexture();
            }
        }

        // Update smoothing value in context
        if (smoothAmount != lipsyncContext.Smoothing)
        {
            lipsyncContext.Smoothing = smoothAmount;
        }
    }

    /// <summary>
    ///     Sets the viseme to texture.
    /// </summary>
    private void SetVisemeToTexture()
    {
        // This setting will run through all the Visemes, find the
        // one with the greatest amplitude and set it to max value.
        // all other visemes will be set to zero.
        var gV = -1;
        var gA = 0.0f;

        for (var i = 0; i < oldFrame.Visemes.Length; i++)
        {
            if (oldFrame.Visemes[i] > gA)
            {
                gV = i;
                gA = oldFrame.Visemes[i];
            }
        }

        if ((gV != -1) && (gV < Textures.Length))
        {
            var t = Textures[gV];

            if (t != null)
            {
                material.SetTexture("_MainTex", t);
            }
        }
    }
}
