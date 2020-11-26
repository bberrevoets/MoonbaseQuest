// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Avatar;

using UnityEngine;

public class OvrAvatarSkinnedMeshRenderPBSComponent : OvrAvatarRenderComponent
{
    private bool isMaterialInitilized = false;

    internal void Initialize(ovrAvatarRenderPart_SkinnedMeshRenderPBS skinnedMeshRenderPBS, Shader shader, int thirdPersonLayer, int firstPersonLayer)
    {
        if (shader == null)
        {
            shader = Shader.Find("OvrAvatar/AvatarSurfaceShaderPBS");
        }

        mesh                = CreateSkinnedMesh(skinnedMeshRenderPBS.meshAssetID, skinnedMeshRenderPBS.visibilityMask, thirdPersonLayer, firstPersonLayer);
        mesh.sharedMaterial = CreateAvatarMaterial(gameObject.name + "_material", shader);
        bones               = mesh.bones;
    }

    internal void UpdateSkinnedMeshRenderPBS(OvrAvatar avatar, IntPtr renderPart, Material mat)
    {
        if (!isMaterialInitilized)
        {
            isMaterialInitilized = true;
            var albedoTextureID  = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetAlbedoTextureAssetID(renderPart);
            var surfaceTextureID = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetSurfaceTextureAssetID(renderPart);
            mat.SetTexture("_Albedo",  OvrAvatarComponent.GetLoadedTexture(albedoTextureID));
            mat.SetTexture("_Surface", OvrAvatarComponent.GetLoadedTexture(surfaceTextureID));
        }

        var visibilityMask = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetVisibilityMask(renderPart);
        var localTransform = CAPI.ovrAvatarSkinnedMeshRenderPBS_GetTransform(renderPart);
        UpdateSkinnedMesh(avatar, bones, localTransform, visibilityMask, renderPart);
    }
}
