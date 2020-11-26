// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;
using System.Collections;
using System.IO;

using UnityEditor;

using UnityEngine;
using UnityEngine.Video;

public class MoviePlayerSample : MonoBehaviour
{
    public enum VideoShape
    {
        _360,
        _180,
        Quad
    }

    public enum VideoStereo
    {
        Mono,
        TopBottom,
        LeftRight,
        BottomTop
    }

    public  string      MovieName;
    public  string      DrmLicenseUrl;
    public  bool        LoopVideo;
    public  VideoShape  Shape;
    public  VideoStereo Stereo;
    public  bool        DisplayMono;
    private bool        _LastDisplayMono = false;

    // keep track of last state so we know when to update our display
    private VideoShape  _LastShape  = (VideoShape) (-1);
    private VideoStereo _LastStereo = (VideoStereo) (-1);

    private RenderTexture copyTexture;
    private Material      externalTex2DMaterial;
    private Renderer      mediaRenderer             = null;
    private OVROverlay    overlay                   = null;
    private bool          videoPausedBeforeAppPause = false;

    private VideoPlayer videoPlayer = null;

    public bool IsPlaying        { get; private set; }
    public long Duration         { get; private set; }
    public long PlaybackPosition { get; private set; }

    /// <summary>
    ///     Initialization of the movie surface
    /// </summary>
    private void Awake()
    {
        Debug.Log("MovieSample Awake");

        mediaRenderer = GetComponent<Renderer>();

        videoPlayer = GetComponent<VideoPlayer>();
        if (videoPlayer == null)
        {
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
        }

        videoPlayer.isLooping = LoopVideo;

        overlay = GetComponent<OVROverlay>();
        if (overlay == null)
        {
            overlay = gameObject.AddComponent<OVROverlay>();
        }

        // disable it to reset it.
        overlay.enabled = false;
        // only can use external surface with native plugin
        overlay.isExternalSurface = NativeVideoPlayer.IsAvailable;
        // only mobile has Equirect shape
        overlay.enabled = (overlay.currentOverlayShape != OVROverlay.OverlayShape.Equirect || Application.platform == RuntimePlatform.Android);

        #if UNITY_EDITOR
        overlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
        overlay.enabled             = true;
        #endif
    }

    private IEnumerator Start()
    {
        if (mediaRenderer.material == null)
        {
            Debug.LogError("No material for movie surface");
            yield break;
        }

        // wait 1 second to start (there is a bug in Unity where starting
        // the video too soon will cause it to fail to load)
        yield return new WaitForSeconds(1.0f);

        if (!string.IsNullOrEmpty(MovieName))
        {
            if (IsLocalVideo(MovieName))
            {
                #if UNITY_EDITOR
                // in editor, just pull in the movie file from wherever it lives (to test without putting in streaming assets)
                var guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(MovieName));

                if (guids.Length > 0)
                {
                    var video = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Play(video, null);
                }
                #else
                Play(Application.streamingAssetsPath +"/" + MovieName, null);
                #endif
            }
            else
            {
                Play(MovieName, DrmLicenseUrl);
            }
        }
    }

    private void Update()
    {
        UpdateShapeAndStereo();
        if (!overlay.isExternalSurface)
        {
            var displayTexture = videoPlayer.texture != null ? videoPlayer.texture : Texture2D.blackTexture;
            if (overlay.enabled)
            {
                if (overlay.textures[0] != displayTexture)
                {
                    // OVROverlay won't check if the texture changed, so disable to clear old texture
                    overlay.enabled     = false;
                    overlay.textures[0] = displayTexture;
                    overlay.enabled     = true;
                }
            }
            else
            {
                mediaRenderer.material.mainTexture = displayTexture;
                mediaRenderer.material.SetVector("_SrcRectLeft",  overlay.srcRectLeft.ToVector());
                mediaRenderer.material.SetVector("_SrcRectRight", overlay.srcRectRight.ToVector());
            }

            IsPlaying        = videoPlayer.isPlaying;
            PlaybackPosition = (long) (videoPlayer.time * 1000L);

            #if UNITY_2019_1_OR_NEWER
            Duration = (long) (videoPlayer.length * 1000L);
            #else
            Duration = videoPlayer.frameRate > 0 ? (long)(videoPlayer.frameCount / videoPlayer.frameRate * 1000L) : 0L;
            #endif
        }
        else
        {
            NativeVideoPlayer.SetListenerRotation(Camera.main.transform.rotation);
            IsPlaying        = NativeVideoPlayer.IsPlaying;
            PlaybackPosition = NativeVideoPlayer.PlaybackPosition;
            Duration         = NativeVideoPlayer.Duration;
            if (IsPlaying && (int) OVRManager.display.displayFrequency != 60)
            {
                OVRManager.display.displayFrequency = 60.0f;
            }
            else if (!IsPlaying && (int) OVRManager.display.displayFrequency != 72)
            {
                OVRManager.display.displayFrequency = 72.0f;
            }
        }
    }

    /// <summary>
    ///     Pauses video playback when the app loses or gains focus
    /// </summary>
    private void OnApplicationPause(bool appWasPaused)
    {
        Debug.Log("OnApplicationPause: " + appWasPaused);
        if (appWasPaused)
        {
            videoPausedBeforeAppPause = !IsPlaying;
        }

        // Pause/unpause the video only if it had been playing prior to app pause
        if (!videoPausedBeforeAppPause)
        {
            if (appWasPaused)
            {
                Pause();
            }
            else
            {
                Play();
            }
        }
    }

    private bool IsLocalVideo(string movieName) =>
            // if the path contains any url scheme, it is not local
            !movieName.Contains("://");

    private void UpdateShapeAndStereo()
    {
        if (Shape != _LastShape || Stereo != _LastStereo || DisplayMono != _LastDisplayMono)
        {
            var destRect = new Rect(0, 0, 1, 1);
            switch (Shape)
            {
                case VideoShape._360:
                    // set shape to Equirect
                    overlay.currentOverlayShape = OVROverlay.OverlayShape.Equirect;
                    break;
                case VideoShape._180:
                    overlay.currentOverlayShape = OVROverlay.OverlayShape.Equirect;
                    destRect                    = new Rect(0.25f, 0, 0.5f, 1.0f);
                    break;
                case VideoShape.Quad:
                default:
                    overlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
                    break;
            }

            overlay.overrideTextureRectMatrix = true;

            var sourceLeft  = new Rect(0, 0, 1, 1);
            var sourceRight = new Rect(0, 0, 1, 1);
            switch (Stereo)
            {
                case VideoStereo.LeftRight:
                    // set source matrices for left/right
                    sourceLeft  = new Rect(0.0f, 0.0f, 0.5f, 1.0f);
                    sourceRight = new Rect(0.5f, 0.0f, 0.5f, 1.0f);
                    break;
                case VideoStereo.TopBottom:
                    // set source matrices for top/bottom
                    sourceLeft  = new Rect(0.0f, 0.5f, 1.0f, 0.5f);
                    sourceRight = new Rect(0.0f, 0.0f, 1.0f, 0.5f);
                    break;
                case VideoStereo.BottomTop:
                    // set source matrices for top/bottom
                    sourceLeft  = new Rect(0.0f, 0.0f, 1.0f, 0.5f);
                    sourceRight = new Rect(0.0f, 0.5f, 1.0f, 0.5f);
                    break;
            }

            overlay.invertTextureRects = false;
            overlay.SetSrcDestRects(sourceLeft, DisplayMono ? sourceLeft : sourceRight, destRect, destRect);

            _LastDisplayMono = DisplayMono;
            _LastStereo      = Stereo;
            _LastShape       = Shape;
        }
    }

    public void Play(string moviePath, string drmLicencesUrl)
    {
        if (moviePath != string.Empty)
        {
            Debug.Log("Playing Video: " + moviePath);
            if (overlay.isExternalSurface)
            {
                OVROverlay.ExternalSurfaceObjectCreated surfaceCreatedCallback = () =>
                {
                    Debug.Log("Playing ExoPlayer with SurfaceObject");
                    NativeVideoPlayer.PlayVideo(moviePath, drmLicencesUrl, overlay.externalSurfaceObject);
                    NativeVideoPlayer.SetLooping(LoopVideo);
                };

                if (overlay.externalSurfaceObject == IntPtr.Zero)
                {
                    overlay.externalSurfaceObjectCreated = surfaceCreatedCallback;
                }
                else
                {
                    surfaceCreatedCallback.Invoke();
                }
            }
            else
            {
                Debug.Log("Playing Unity VideoPlayer");
                videoPlayer.url = moviePath;
                videoPlayer.Prepare();
                videoPlayer.Play();
            }

            Debug.Log("MovieSample Start");
            IsPlaying = true;
        }
        else
        {
            Debug.LogError("No media file name provided");
        }
    }

    public void Play()
    {
        if (overlay.isExternalSurface)
        {
            NativeVideoPlayer.Play();
        }
        else
        {
            videoPlayer.Play();
        }

        IsPlaying = true;
    }

    public void Pause()
    {
        if (overlay.isExternalSurface)
        {
            NativeVideoPlayer.Pause();
        }
        else
        {
            videoPlayer.Pause();
        }

        IsPlaying = false;
    }

    public void SeekTo(long position)
    {
        var seekPos = Math.Max(0, Math.Min(Duration, position));
        if (overlay.isExternalSurface)
        {
            NativeVideoPlayer.PlaybackPosition = seekPos;
        }
        else
        {
            videoPlayer.time = seekPos / 1000.0;
        }
    }

    public void SetPlaybackSpeed(float speed)
    {
        // clamp at 0
        speed = Mathf.Max(0, speed);
        if (overlay.isExternalSurface)
        {
            NativeVideoPlayer.SetPlaybackSpeed(speed);
        }
        else
        {
            videoPlayer.playbackSpeed = speed;
        }
    }

    public void Stop()
    {
        if (overlay.isExternalSurface)
        {
            NativeVideoPlayer.Stop();
        }
        else
        {
            videoPlayer.Stop();
        }

        IsPlaying = false;
    }
}
