// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using Oculus.Avatar;

using UnityEngine;

public class OvrAvatarAssetTexture : OvrAvatarAsset
{
    private const int       ASTCHeaderSize = 16;
    public        Texture2D texture;

    public OvrAvatarAssetTexture(ulong _assetId, IntPtr asset)
    {
        assetID = _assetId;
        var           textureAssetData = CAPI.ovrAvatarAsset_GetTextureData(asset);
        TextureFormat format;
        var           textureData     = textureAssetData.textureData;
        var           textureDataSize = (int) textureAssetData.textureDataSize;

        AvatarLogger.Log(
                "OvrAvatarAssetTexture - "
                + _assetId
                + ": "
                + textureAssetData.format
                + " "
                + textureAssetData.sizeX
                + "x"
                + textureAssetData.sizeY);

        switch (textureAssetData.format)
        {
            case ovrAvatarTextureFormat.RGB24:
                format = TextureFormat.RGB24;
                break;
            case ovrAvatarTextureFormat.DXT1:
                format = TextureFormat.DXT1;
                break;
            case ovrAvatarTextureFormat.DXT5:
                format = TextureFormat.DXT5;
                break;
            case ovrAvatarTextureFormat.ASTC_RGB_6x6:
                #if UNITY_2020_1_OR_NEWER
                format = TextureFormat.ASTC_6x6;
                #else
                format = TextureFormat.ASTC_RGB_6x6;
                #endif
                textureData     =  new IntPtr(textureData.ToInt64() + ASTCHeaderSize);
                textureDataSize -= ASTCHeaderSize;
                break;
            case ovrAvatarTextureFormat.ASTC_RGB_6x6_MIPMAPS:
                #if UNITY_2020_1_OR_NEWER
                format = TextureFormat.ASTC_6x6;
                #else
                format = TextureFormat.ASTC_RGB_6x6;
                #endif
                break;
            default:
                throw new NotImplementedException(
                        string.Format("Unsupported texture format {0}",
                                textureAssetData.format.ToString()));
        }

        texture = new Texture2D(
                (int) textureAssetData.sizeX, (int) textureAssetData.sizeY,
                format, textureAssetData.mipCount > 1,
                QualitySettings.activeColorSpace == ColorSpace.Gamma ? false : true)
                {
                        filterMode = FilterMode.Trilinear,
                        anisoLevel = 4
                };
        texture.LoadRawTextureData(textureData, textureDataSize);
        texture.Apply(true, false);
    }
}
