// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Runtime.InteropServices;

using UnityEngine;

public class ONSPProfiler : MonoBehaviour
{
    private const int DEFAULT_PORT = 2121;

    // Import functions
    public const string strONSPS        = "AudioPluginOculusSpatializer";
    public       bool   profilerEnabled = false;
    public       int    port            = DEFAULT_PORT;

    private void Start()
    {
        Application.runInBackground = true;
    }

    private void Update()
    {
        if (port < 0 || port > 65535)
        {
            port = DEFAULT_PORT;
        }

        ONSP_SetProfilerPort(port);
        ONSP_SetProfilerEnabled(profilerEnabled);
    }

    [DllImport(strONSPS)]
    private static extern int ONSP_SetProfilerEnabled(bool enabled);

    [DllImport(strONSPS)]
    private static extern int ONSP_SetProfilerPort(int port);
}
