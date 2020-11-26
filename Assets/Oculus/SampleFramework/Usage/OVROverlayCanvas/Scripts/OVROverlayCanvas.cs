// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

//#define DEBUG_OVERLAY_CANVAS

using UnityEngine;

[RequireComponent(typeof(Canvas))]
public class OVROverlayCanvas : MonoBehaviour
{
    public enum DrawMode
    {
        Opaque,
        OpaqueWithClip,
        TransparentDefaultAlpha,
        TransparentCorrectAlpha
    }

    private static readonly Plane[] _FrustumPlanes = new Plane[6];

    [SerializeField] [HideInInspector]
    private Shader _transparentShader = null;

    [SerializeField] [HideInInspector]
    private Shader _opaqueShader = null;

    public int   MaxTextureSize  = 1600;
    public int   MinTextureSize  = 200;
    public float PixelsPerUnit   = 1f;
    public int   DrawRate        = 1;
    public int   DrawFrameOffset = 0;
    public bool  Expensive       = false;
    public int   Layer           = 0;

    public  DrawMode     Opacity = DrawMode.OpaqueWithClip;
    private Camera       _camera;
    private Canvas       _canvas;
    private Material     _defaultMat;
    private MeshRenderer _meshRenderer;
    private OVROverlay   _overlay;

    private Mesh _quad;

    private RectTransform _rectTransform;
    private RenderTexture _renderTexture;

    private bool ScaleViewport = Application.isMobilePlatform;

    public bool overlayEnabled
    {
        get => _overlay && _overlay.enabled;
        set
        {
            if (_overlay)
            {
                _overlay.enabled  = value;
                _defaultMat.color = value ? Color.black : Color.white;
            }
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        _canvas = GetComponent<Canvas>();

        _rectTransform = _canvas.GetComponent<RectTransform>();

        var rectWidth  = _rectTransform.rect.width;
        var rectHeight = _rectTransform.rect.height;

        var aspectX = rectWidth >= rectHeight ? 1 : rectWidth / rectHeight;
        var aspectY = rectHeight >= rectWidth ? 1 : rectHeight / rectWidth;

        // if we are scaling the viewport we don't need to add a border
        var pixelBorder = ScaleViewport ? 0 : 8;
        var innerWidth  = Mathf.CeilToInt(aspectX * (MaxTextureSize - pixelBorder * 2));
        var innerHeight = Mathf.CeilToInt(aspectY * (MaxTextureSize - pixelBorder * 2));
        var width       = innerWidth + pixelBorder * 2;
        var height      = innerHeight + pixelBorder * 2;

        var paddedWidth  = rectWidth * (width / (float) innerWidth);
        var paddedHeight = rectHeight * (height / (float) innerHeight);

        var insetRectWidth  = innerWidth / (float) width;
        var insetRectHeight = innerHeight / (float) height;

        // ever so slightly shrink our opaque mesh to avoid black borders
        var opaqueTrim = Opacity == DrawMode.Opaque ? new Vector2(0.005f / _rectTransform.lossyScale.x, 0.005f / _rectTransform.lossyScale.y) : Vector2.zero;

        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        // if we can't scale the viewport, generate mipmaps instead
        _renderTexture.useMipMap = !ScaleViewport;

        var overlayCamera = new GameObject(name + " Overlay Camera")
        {
                #if !DEBUG_OVERLAY_CANVAS
                hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable
                #endif
        };
        overlayCamera.transform.SetParent(transform, false);

        _camera                    = overlayCamera.AddComponent<Camera>();
        _camera.stereoTargetEye    = StereoTargetEyeMask.None;
        _camera.transform.position = transform.position - transform.forward;
        _camera.orthographic       = true;
        _camera.enabled            = false;
        _camera.targetTexture      = _renderTexture;
        _camera.cullingMask        = 1 << gameObject.layer;
        _camera.clearFlags         = CameraClearFlags.SolidColor;
        _camera.backgroundColor    = Color.clear;
        _camera.orthographicSize   = 0.5f * paddedHeight * _rectTransform.localScale.y;
        _camera.nearClipPlane      = 0.99f;
        _camera.farClipPlane       = 1.01f;

        _quad = new Mesh
        {
                name      = name + " Overlay Quad",
                hideFlags = HideFlags.HideAndDontSave
        };

        _quad.vertices  = new[] {new Vector3(-0.5f, -0.5f), new Vector3(-0.5f, 0.5f), new Vector3(0.5f, 0.5f), new Vector3(0.5f, -0.5f)};
        _quad.uv        = new[] {new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0)};
        _quad.triangles = new[] {0, 1, 2, 2, 3, 0};
        _quad.bounds    = new Bounds(Vector3.zero, Vector3.one);
        _quad.UploadMeshData(true);

        switch (Opacity)
        {
            case DrawMode.Opaque:
                _defaultMat = new Material(_opaqueShader);
                break;
            case DrawMode.OpaqueWithClip:
                _defaultMat = new Material(_opaqueShader);
                _defaultMat.EnableKeyword("WITH_CLIP");
                break;
            case DrawMode.TransparentDefaultAlpha:
                _defaultMat = new Material(_transparentShader);
                _defaultMat.EnableKeyword("ALPHA_SQUARED");
                break;
            case DrawMode.TransparentCorrectAlpha:
                _defaultMat = new Material(_transparentShader);
                break;
        }

        _defaultMat.mainTexture       = _renderTexture;
        _defaultMat.color             = Color.black;
        _defaultMat.mainTextureOffset = new Vector2(0.5f - 0.5f * insetRectWidth, 0.5f - 0.5f * insetRectHeight);
        _defaultMat.mainTextureScale  = new Vector2(insetRectWidth,               insetRectHeight);

        var meshRenderer = new GameObject(name + " MeshRenderer")
        {
                #if !DEBUG_OVERLAY_CANVAS
                hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable
                #endif
        };

        meshRenderer.transform.SetParent(transform, false);
        meshRenderer.AddComponent<MeshFilter>().sharedMesh = _quad;
        _meshRenderer                                      = meshRenderer.AddComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial                       = _defaultMat;
        meshRenderer.layer                                 = Layer;
        meshRenderer.transform.localScale                  = new Vector3(rectWidth - opaqueTrim.x, rectHeight - opaqueTrim.y, 1);

        var overlay = new GameObject(name + " Overlay")
        {
                #if !DEBUG_OVERLAY_CANVAS
                hideFlags = HideFlags.HideInHierarchy | HideFlags.NotEditable
                #endif
        };
        overlay.transform.SetParent(transform, false);
        _overlay                         = overlay.AddComponent<OVROverlay>();
        _overlay.isDynamic               = true;
        _overlay.noDepthBufferTesting    = true;
        _overlay.isAlphaPremultiplied    = !Application.isMobilePlatform;
        _overlay.textures[0]             = _renderTexture;
        _overlay.currentOverlayType      = OVROverlay.OverlayType.Underlay;
        _overlay.transform.localScale    = new Vector3(paddedWidth, paddedHeight, 1);
        _overlay.useExpensiveSuperSample = Expensive;
    }

    private void Update()
    {
        if (ShouldRender())
        {
            if (ScaleViewport)
            {
                if (Camera.main != null)
                {
                    var d = (Camera.main.transform.position - transform.position).magnitude;

                    var size = PixelsPerUnit * Mathf.Max(_rectTransform.rect.width * transform.lossyScale.x, _rectTransform.rect.height * transform.lossyScale.y) / d;

                    // quantize to even pixel sizes
                    const float quantize    = 8;
                    var         pixelHeight = Mathf.Ceil(size / quantize * _renderTexture.height) * quantize;

                    // clamp between or min size and our max size
                    pixelHeight = Mathf.Clamp(pixelHeight, MinTextureSize, _renderTexture.height);

                    var innerPixelHeight = pixelHeight - 2;

                    _camera.orthographicSize = 0.5f * _rectTransform.rect.height * _rectTransform.localScale.y * pixelHeight / innerPixelHeight;

                    var aspect = (_rectTransform.rect.width / _rectTransform.rect.height);

                    var innerPixelWidth = innerPixelHeight * aspect;
                    var pixelWidth      = Mathf.Ceil((innerPixelWidth + 2) * 0.5f) * 2;

                    var sizeX = pixelWidth / _renderTexture.width;
                    var sizeY = pixelHeight / _renderTexture.height;

                    // trim a half pixel off each size if this is opaque (transparent should fade)
                    var inset = Opacity == DrawMode.Opaque ? 1.001f : 0;

                    var innerSizeX = (innerPixelWidth - inset) / _renderTexture.width;
                    var innerSizeY = (innerPixelHeight - inset) / _renderTexture.height;

                    // scale the camera rect
                    _camera.rect = new Rect((1 - sizeX) / 2, (1 - sizeY) / 2, sizeX, sizeY);

                    var src = new Rect(0.5f - (0.5f * innerSizeX), 0.5f - (0.5f * innerSizeY), innerSizeX, innerSizeY);

                    _defaultMat.mainTextureOffset = src.min;
                    _defaultMat.mainTextureScale  = src.size;

                    // update the overlay to use this same size
                    _overlay.overrideTextureRectMatrix = true;
                    src.y                              = 1 - src.height - src.y;
                    var dst = new Rect(0, 0, 1, 1);
                    _overlay.SetSrcDestRects(src, src, dst, dst);
                }
            }

            _camera.Render();
        }
    }

    private void OnEnable()
    {
        if (_overlay)
        {
            _meshRenderer.enabled = true;
            _overlay.enabled      = true;
        }

        if (_camera)
        {
            _camera.enabled = true;
        }
    }

    private void OnDisable()
    {
        if (_overlay)
        {
            _overlay.enabled      = false;
            _meshRenderer.enabled = false;
        }

        if (_camera)
        {
            _camera.enabled = false;
        }
    }

    private void OnDestroy()
    {
        Destroy(_defaultMat);
        Destroy(_quad);
        Destroy(_renderTexture);
    }

    protected virtual bool ShouldRender()
    {
        if (DrawRate > 1)
        {
            if (Time.frameCount % DrawRate != DrawFrameOffset % DrawRate)
            {
                return false;
            }
        }

        if (Camera.main != null)
        {
            // Perform Frustum culling
            for (var i = 0; i < 2; i++)
            {
                var eye = (Camera.StereoscopicEye) i;
                var mat = Camera.main.GetStereoProjectionMatrix(eye) * Camera.main.GetStereoViewMatrix(eye);
                GeometryUtility.CalculateFrustumPlanes(mat, _FrustumPlanes);
                if (GeometryUtility.TestPlanesAABB(_FrustumPlanes, _meshRenderer.bounds))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }
}
