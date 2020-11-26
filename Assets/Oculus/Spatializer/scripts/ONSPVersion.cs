// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Runtime.InteropServices;

using UnityEngine;

public class ONSPVersion : MonoBehaviour
{
    // Import functions
    public const string strONSPS = "AudioPluginOculusSpatializer";

    /// <summary>
    ///     Awake this instance.
    /// </summary>
    private void Awake()
    {
        var major = 0;
        var minor = 0;
        var patch = 0;

        ONSP_GetVersion(ref major, ref minor, ref patch);

        var version = string.Format
                ("ONSP Version: {0:F0}.{1:F0}.{2:F0}", major, minor, patch);

        Debug.Log(version);
    }

    /// <summary>
    ///     Start this instance.
    /// </summary>
    private void Start() { }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update() { }

    [DllImport(strONSPS)]
    private static extern void ONSP_GetVersion(ref int Major, ref int Minor, ref int Patch);
}
