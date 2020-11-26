// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.IO;

using UnityEditor;

using UnityEngine;

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public sealed class OvrAvatarSettings : ScriptableObject
{
    private static OvrAvatarSettings instance;

    [SerializeField]
    private string ovrAppID = "";

    [SerializeField]
    private string ovrGearAppID = "";

    public static string AppID
    {
        get => Instance.ovrAppID;
        set => Instance.ovrAppID = value;
    }

    public static string MobileAppID
    {
        get => Instance.ovrGearAppID;
        set => Instance.ovrGearAppID = value;
    }

    public static OvrAvatarSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<OvrAvatarSettings>("OvrAvatarSettings");

                // This can happen if the developer never input their App Id into the Unity Editor
                // Use a dummy object with defaults for the getters so we don't have a null pointer exception
                if (instance == null)
                {
                    instance = CreateInstance<OvrAvatarSettings>();

                    #if UNITY_EDITOR
                    // Only in the editor should we save it to disk
                    var properPath = Path.Combine(Application.dataPath, "Resources");
                    if (!Directory.Exists(properPath))
                    {
                        AssetDatabase.CreateFolder("Assets", "Resources");
                    }

                    var fullPath = Path.Combine(
                            Path.Combine("Assets", "Resources"),
                            "OvrAvatarSettings.asset"
                            );
                    AssetDatabase.CreateAsset(instance, fullPath);
                    #endif
                }
            }

            return instance;
        }

        set { instance = value; }
    }
}
