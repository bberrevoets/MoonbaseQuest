// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

// Uncomment below to test access of read-only spatializer parameters
//#define TEST_READONLY_PARAMETERS

using System.Runtime.InteropServices;

using UnityEngine;

public class ONSPAudioSource : MonoBehaviour
{
    #if TEST_READONLY_PARAMETERS
    // Spatializer read-only system parameters (global)
    static int readOnly_GlobalRelectionOn = 8;
    static int readOnly_NumberOfUsedSpatializedVoices = 9;
    #endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoadRuntimeMethod()
    {
        OSP_SetGlobalVoiceLimit(ONSPSettings.Instance.voiceLimit);
    }

    // Import functions
    public const string strONSPS = "AudioPluginOculusSpatializer";

    [DllImport(strONSPS)]
    private static extern void ONSP_GetGlobalRoomReflectionValues(ref bool  reflOn, ref bool  reverbOn,
                                                                  ref float width,  ref float height, ref float length);

    // Public

    [SerializeField]
    private bool enableSpatialization = true;

    public bool EnableSpatialization
    {
        get => enableSpatialization;
        set => enableSpatialization = value;
    }

    [SerializeField]
    private float gain = 0.0f;

    public float Gain
    {
        get => gain;
        set => gain = Mathf.Clamp(value, 0.0f, 24.0f);
    }

    [SerializeField]
    private bool useInvSqr = false;

    public bool UseInvSqr
    {
        get => useInvSqr;
        set => useInvSqr = value;
    }

    [SerializeField]
    private float near = 0.25f;

    public float Near
    {
        get => near;
        set => near = Mathf.Clamp(value, 0.0f, 1000000.0f);
    }

    [SerializeField]
    private float far = 250.0f;

    public float Far
    {
        get => far;
        set => far = Mathf.Clamp(value, 0.0f, 1000000.0f);
    }

    [SerializeField]
    private float volumetricRadius = 0.0f;

    public float VolumetricRadius
    {
        get => volumetricRadius;
        set => volumetricRadius = Mathf.Clamp(value, 0.0f, 1000.0f);
    }

    [SerializeField]
    private float reverbSend = 0.0f;

    public float ReverbSend
    {
        get => reverbSend;
        set => reverbSend = Mathf.Clamp(value, -60.0f, 20.0f);
    }

    [SerializeField]
    private bool enableRfl = false;

    public bool EnableRfl
    {
        get => enableRfl;
        set => enableRfl = value;
    }

    /// <summary>
    ///     Awake this instance.
    /// </summary>
    private void Awake()
    {
        // We might iterate through multiple sources / game object
        var source = GetComponent<AudioSource>();
        SetParameters(ref source);
    }

    /// <summary>
    ///     Start this instance.
    /// </summary>
    private void Start() { }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update()
    {
        // We might iterate through multiple sources / game object
        var source = GetComponent<AudioSource>();

        // READ-ONLY PARAMETER TEST      
        #if TEST_READONLY_PARAMETERS
        float rfl_enabled = 0.0f;
        source.GetSpatializerFloat(readOnly_GlobalRelectionOn, out rfl_enabled);
        float num_voices = 0.0f;
        source.GetSpatializerFloat(readOnly_NumberOfUsedSpatializedVoices, out num_voices);

        String readOnly = System.String.Format
        ("Read only values: refl enabled: {0:F0} num voices: {1:F0}", rfl_enabled, num_voices);
        Debug.Log(readOnly);
        #endif

        // Check to see if we should disable spatializion
        if ((Application.isPlaying == false) ||
            AudioListener.pause ||
            (source.isPlaying == false) ||
            (source.isActiveAndEnabled == false)
                )
        {
            source.spatialize = false;
        }
        else
        {
            SetParameters(ref source);
        }
    }

    private enum Parameters
    {
        P_GAIN = 0,
        P_USEINVSQR,
        P_NEAR,
        P_FAR,
        P_RADIUS,
        P_DISABLE_RFL,
        P_VSPEAKERMODE,
        P_AMBISTAT,
        P_READONLY_GLOBAL_RFL_ENABLED, // READ-ONLY
        P_READONLY_NUM_VOICES,         // READ-ONLY
        P_SENDLEVEL,
        P_NUM
    }

    /// <summary>
    ///     Sets the parameters.
    /// </summary>
    /// <param name="source">Source.</param>
    public void SetParameters(ref AudioSource source)
    {
        // See if we should enable spatialization
        source.spatialize = enableSpatialization;

        source.SetSpatializerFloat((int) Parameters.P_GAIN, gain);
        // All inputs are floats; convert bool to 0.0 and 1.0
        if (useInvSqr)
        {
            source.SetSpatializerFloat((int) Parameters.P_USEINVSQR, 1.0f);
        }
        else
        {
            source.SetSpatializerFloat((int) Parameters.P_USEINVSQR, 0.0f);
        }

        source.SetSpatializerFloat((int) Parameters.P_NEAR, near);
        source.SetSpatializerFloat((int) Parameters.P_FAR,  far);

        source.SetSpatializerFloat((int) Parameters.P_RADIUS, volumetricRadius);

        if (enableRfl)
        {
            source.SetSpatializerFloat((int) Parameters.P_DISABLE_RFL, 0.0f);
        }
        else
        {
            source.SetSpatializerFloat((int) Parameters.P_DISABLE_RFL, 1.0f);
        }

        source.SetSpatializerFloat((int) Parameters.P_SENDLEVEL, reverbSend);
    }

    private static ONSPAudioSource RoomReflectionGizmoAS = null;

    /// <summary>
    /// </summary>
    private void OnDrawGizmos()
    {
        // Are we the first one created? make sure to set our static ONSPAudioSource
        // for drawing out room parameters once
        if (RoomReflectionGizmoAS == null)
        {
            RoomReflectionGizmoAS = this;
        }

        Color       c;
        const float colorSolidAlpha = 0.1f;

        // Draw the near/far spheres

        // Near (orange)
        c.r          = 1.0f;
        c.g          = 0.5f;
        c.b          = 0.0f;
        c.a          = 1.0f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, Near);
        c.a          = colorSolidAlpha;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, Near);

        // Far (red)
        c.r          = 1.0f;
        c.g          = 0.0f;
        c.b          = 0.0f;
        c.a          = 1.0f;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, Far);
        c.a          = colorSolidAlpha;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, Far);

        // VolumetricRadius (purple)
        c.r          = 1.0f;
        c.g          = 0.0f;
        c.b          = 1.0f;
        c.a          = 1.0f;
        Gizmos.color = c;
        Gizmos.DrawWireSphere(transform.position, VolumetricRadius);
        c.a          = colorSolidAlpha;
        Gizmos.color = c;
        Gizmos.DrawSphere(transform.position, VolumetricRadius);

        // Draw room parameters ONCE only, provided reflection engine is on
        if (RoomReflectionGizmoAS == this)
        {
            // Get global room parameters (write new C api to get reflection values)
            var reflOn   = false;
            var reverbOn = false;
            var width    = 1.0f;
            var height   = 1.0f;
            var length   = 1.0f;

            ONSP_GetGlobalRoomReflectionValues(ref reflOn, ref reverbOn, ref width, ref height, ref length);

            // TO DO: Get the room reflection values and render those out as well (like we do in the VST)

            if ((Camera.main != null) && reflOn)
            {
                // Set color of cube (cyan is early reflections only, white is with reverb on)
                if (reverbOn)
                {
                    c = Color.white;
                }
                else
                {
                    c = Color.cyan;
                }

                Gizmos.color = c;
                Gizmos.DrawWireCube(Camera.main.transform.position, new Vector3(width, height, length));
                c.a          = colorSolidAlpha;
                Gizmos.color = c;
                Gizmos.DrawCube(Camera.main.transform.position, new Vector3(width, height, length));
            }
        }
    }

    /// <summary>
    /// </summary>
    private void OnDestroy()
    {
        // We will null out single pointer instance
        // of the room reflection gizmo since we are being destroyed.
        // Any ONSPAS that is alive or born will re-set this pointer
        // so that we only draw it once
        if (RoomReflectionGizmoAS == this)
        {
            RoomReflectionGizmoAS = null;
        }
    }

    [DllImport("AudioPluginOculusSpatializer")]
    private static extern int OSP_SetGlobalVoiceLimit(int VoiceLimit);
}
