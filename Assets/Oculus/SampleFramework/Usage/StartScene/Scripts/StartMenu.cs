// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.IO;

using UnityEngine;
using UnityEngine.SceneManagement;

// Create menu of all scenes included in the build.
public class StartMenu : MonoBehaviour
{
    public OVROverlay   overlay;
    public OVROverlay   text;
    public OVRCameraRig vrRig;

    private void Start()
    {
        DebugUIBuilder.instance.AddLabel("Select Sample Scene");

        var n = SceneManager.sceneCountInBuildSettings;
        for (var i = 0; i < n; ++i)
        {
            var path       = SceneUtility.GetScenePathByBuildIndex(i);
            var sceneIndex = i;
            DebugUIBuilder.instance.AddButton(Path.GetFileNameWithoutExtension(path), () => LoadScene(sceneIndex));
        }

        DebugUIBuilder.instance.Show();
    }

    private void LoadScene(int idx)
    {
        DebugUIBuilder.instance.Hide();
        Debug.Log("Load scene: " + idx);
        SceneManager.LoadScene(idx);
    }
}
