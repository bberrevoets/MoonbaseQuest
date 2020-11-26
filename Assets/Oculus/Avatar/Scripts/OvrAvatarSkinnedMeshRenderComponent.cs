// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Avatar;

using UnityEngine;

public class OvrAvatarSkinnedMeshRenderComponent : OvrAvatarRenderComponent
{
    private bool   previouslyActive = false;
    private Shader surface;
    private Shader surfaceSelfOccluding;

    internal void Initialize(ovrAvatarRenderPart_SkinnedMeshRender skinnedMeshRender, Shader surface, Shader surfaceSelfOccluding, int thirdPersonLayer, int firstPersonLayer)
    {
        this.surfaceSelfOccluding = surfaceSelfOccluding != null ? surfaceSelfOccluding : Shader.Find("OvrAvatar/AvatarSurfaceShaderSelfOccluding");
        this.surface              = surface != null ? surface : Shader.Find("OvrAvatar/AvatarSurfaceShader");
        mesh                      = CreateSkinnedMesh(skinnedMeshRender.meshAssetID, skinnedMeshRender.visibilityMask, thirdPersonLayer, firstPersonLayer);
        bones                     = mesh.bones;
        UpdateMeshMaterial(skinnedMeshRender.visibilityMask, mesh);
    }

    public void UpdateSkinnedMeshRender(OvrAvatarComponent component, OvrAvatar avatar, IntPtr renderPart)
    {
        var visibilityMask = CAPI.ovrAvatarSkinnedMeshRender_GetVisibilityMask(renderPart);
        var localTransform = CAPI.ovrAvatarSkinnedMeshRender_GetTransform(renderPart);
        UpdateSkinnedMesh(avatar, bones, localTransform, visibilityMask, renderPart);

        UpdateMeshMaterial(visibilityMask, mesh);
        var isActive = gameObject.activeSelf;

        if (mesh != null)
        {
            var changedMaterial = CAPI.ovrAvatarSkinnedMeshRender_MaterialStateChanged(renderPart);
            if (changedMaterial || (!previouslyActive && isActive))
            {
                var materialState = CAPI.ovrAvatarSkinnedMeshRender_GetMaterialState(renderPart);
                component.UpdateAvatarMaterial(mesh.sharedMaterial, materialState);
            }
        }

        previouslyActive = isActive;
    }

    private void UpdateMeshMaterial(ovrAvatarVisibilityFlags visibilityMask, SkinnedMeshRenderer rootMesh)
    {
        var shader = (visibilityMask & ovrAvatarVisibilityFlags.SelfOccluding) != 0 ? surfaceSelfOccluding : surface;
        if (rootMesh.sharedMaterial == null || rootMesh.sharedMaterial.shader != shader)
        {
            rootMesh.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
        }
    }
}
