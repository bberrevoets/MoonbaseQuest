// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using Oculus.Avatar;

using UnityEngine;

public class GazeTarget : MonoBehaviour
{
    private static ovrAvatarGazeTargets    RuntimeTargetList;
    public         ovrAvatarGazeTargetType Type;

    static GazeTarget()
    {
        // This size has to match the 'MarshalAs' attribute in the ovrAvatarGazeTargets declaration.
        RuntimeTargetList.targets     = new ovrAvatarGazeTarget[128];
        RuntimeTargetList.targetCount = 1;
    }

    private void Start()
    {
        UpdateGazeTarget();
        transform.hasChanged = false;
    }

    private void Update()
    {
        if (transform.hasChanged)
        {
            transform.hasChanged = false;
            UpdateGazeTarget();
        }
    }

    private void OnDestroy()
    {
        var targetIds = new uint[1];
        targetIds[0] = (uint) transform.GetInstanceID();
        CAPI.ovrAvatar_RemoveGazeTargets(1, targetIds);
    }

    private void UpdateGazeTarget()
    {
        var target = CreateOvrGazeTarget((uint) transform.GetInstanceID(), transform.position, Type);
        RuntimeTargetList.targets[0] = target;
        CAPI.ovrAvatar_UpdateGazeTargets(RuntimeTargetList);
    }

    private ovrAvatarGazeTarget CreateOvrGazeTarget(uint targetId, Vector3 targetPosition, ovrAvatarGazeTargetType targetType) =>
            new ovrAvatarGazeTarget
            {
                    id = targetId,
                    // Do coordinate system switch.
                    worldPosition = new Vector3(targetPosition.x, targetPosition.y, -targetPosition.z),
                    type          = targetType
            };
}
