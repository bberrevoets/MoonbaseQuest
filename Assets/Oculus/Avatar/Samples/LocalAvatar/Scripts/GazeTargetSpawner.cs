// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class GazeTargetSpawner : MonoBehaviour
{
    public GameObject GazeTargetPrefab;
    public int        NumberOfDummyTargets = 100;
    public int        RadiusMultiplier     = 3;

    [SerializeField]
    private bool isVisible;

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            isVisible = value;
            var dummyGazeTargets = gameObject.GetComponentsInChildren<GazeTarget>();
            for (var i = 0; i < dummyGazeTargets.Length; ++i)
            {
                var dummyMesh = dummyGazeTargets[i].GetComponent<MeshRenderer>();
                if (dummyMesh != null)
                {
                    dummyMesh.enabled = isVisible;
                }
            }
        }
    }

    private void Start()
    {
        for (var i = 0; i < NumberOfDummyTargets; ++i)
        {
            var target = Instantiate(GazeTargetPrefab, transform);
            target.name                                 += "_" + i;
            target.transform.localPosition              =  Random.insideUnitSphere * RadiusMultiplier;
            target.transform.rotation                   =  Quaternion.identity;
            target.GetComponent<MeshRenderer>().enabled =  IsVisible;
        }
    }

    private void OnValidate()
    {
        // Run through OnValidate to pick up changes from inspector
        IsVisible = isVisible;
    }
}
