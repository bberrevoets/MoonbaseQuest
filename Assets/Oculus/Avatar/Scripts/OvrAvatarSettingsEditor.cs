// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

#if UNITY_EDITOR
using Oculus.Avatar;

using UnityEditor;

using UnityEngine;

[CustomEditor(typeof(OvrAvatarSettings))]
[InitializeOnLoadAttribute]
public class OvrAvatarSettingsEditor : Editor
{
    private GUIContent appIDLabel = new GUIContent("Oculus Rift App Id [?]",
            "This AppID will be used for OvrAvatar registration.");

    private GUIContent mobileAppIDLabel = new GUIContent("Oculus Go/Quest or Gear VR [?]",
            "This AppID will be used when building to the Android target");

    static OvrAvatarSettingsEditor()
    {
        #if UNITY_2017_2_OR_NEWER
        EditorApplication.playModeStateChanged += HandlePlayModeState;
        #else
        EditorApplication.playmodeStateChanged += () =>
        {
            if (EditorApplication.isPlaying)
            {
                CAPI.SendEvent("load", CAPI.AvatarSDKVersion.ToString());
            }
        };
        #endif
    }

    [MenuItem("Oculus/Avatars/Edit Settings")]
    public static void Edit()
    {
        var settings = OvrAvatarSettings.Instance;
        Selection.activeObject = settings;
        CAPI.SendEvent("edit_settings");
    }

    #if UNITY_2017_2_OR_NEWER
    private static void HandlePlayModeState(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            CAPI.SendEvent("load", CAPI.AvatarSDKVersion.ToString());
        }
    }
    #endif

    private static string MakeTextBox(GUIContent label, string variable)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label);
        GUI.changed = false;
        var result = EditorGUILayout.TextField(variable);
        if (GUI.changed)
        {
            EditorUtility.SetDirty(OvrAvatarSettings.Instance);
            GUI.changed = false;
        }

        EditorGUILayout.EndHorizontal();
        return result;
    }

    public override void OnInspectorGUI()
    {
        EditorGUILayout.BeginVertical();
        OvrAvatarSettings.AppID =
                MakeTextBox(appIDLabel, OvrAvatarSettings.AppID);
        OvrAvatarSettings.MobileAppID =
                MakeTextBox(mobileAppIDLabel, OvrAvatarSettings.MobileAppID);
        EditorGUILayout.EndVertical();
    }
}
#endif
