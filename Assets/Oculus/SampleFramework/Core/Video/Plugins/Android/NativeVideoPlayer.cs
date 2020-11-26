// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System;

using UnityEngine;

public static class NativeVideoPlayer
{
    public enum PlabackState
    {
        Idle      = 1,
        Preparing = 2,
        Buffering = 3,
        Ready     = 4,
        Ended     = 5
    }

    private static IntPtr? _Activity;
    private static IntPtr? _VideoPlayerClass;

    private static readonly jvalue[] EmptyParams = new jvalue[0];

    private static IntPtr   getIsPlayingMethodId;
    private static IntPtr   getCurrentPlaybackStateMethodId;
    private static IntPtr   getDurationMethodId;
    private static IntPtr   getPlaybackPositionMethodId;
    private static IntPtr   setPlaybackPositionMethodId;
    private static jvalue[] setPlaybackPositionParams;
    private static IntPtr   playVideoMethodId;
    private static jvalue[] playVideoParams;
    private static IntPtr   stopMethodId;
    private static IntPtr   resumeMethodId;
    private static IntPtr   pauseMethodId;
    private static IntPtr   setPlaybackSpeedMethodId;
    private static jvalue[] setPlaybackSpeedParams;
    private static IntPtr   setLoopingMethodId;
    private static jvalue[] setLoopingParams;
    private static IntPtr   setListenerRotationQuaternionMethodId;
    private static jvalue[] setListenerRotationQuaternionParams;

    private static IntPtr VideoPlayerClass
    {
        get
        {
            if (!_VideoPlayerClass.HasValue)
            {
                try
                {
                    var myVideoPlayerClass = AndroidJNI.FindClass("com/oculus/videoplayer/NativeVideoPlayer");

                    if (myVideoPlayerClass != IntPtr.Zero)
                    {
                        _VideoPlayerClass = AndroidJNI.NewGlobalRef(myVideoPlayerClass);

                        AndroidJNI.DeleteLocalRef(myVideoPlayerClass);
                    }
                    else
                    {
                        Debug.LogError("Failed to find NativeVideoPlayer class");
                        _VideoPlayerClass = IntPtr.Zero;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError("Failed to find NativeVideoPlayer class");
                    Debug.LogException(ex);
                    _VideoPlayerClass = IntPtr.Zero;
                }
            }

            return _VideoPlayerClass.GetValueOrDefault();
        }
    }

    private static IntPtr Activity
    {
        get
        {
            if (!_Activity.HasValue)
            {
                try
                {
                    var unityPlayerClass     = AndroidJNI.FindClass("com/unity3d/player/UnityPlayer");
                    var currentActivityField = AndroidJNI.GetStaticFieldID(unityPlayerClass, "currentActivity", "Landroid/app/Activity;");
                    var activity             = AndroidJNI.GetStaticObjectField(unityPlayerClass, currentActivityField);

                    _Activity = AndroidJNI.NewGlobalRef(activity);

                    AndroidJNI.DeleteLocalRef(activity);
                    AndroidJNI.DeleteLocalRef(unityPlayerClass);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    _Activity = IntPtr.Zero;
                }
            }

            return _Activity.GetValueOrDefault();
        }
    }

    public static bool IsAvailable
    {
        get
        {
            #if UNITY_ANDROID && !UNITY_EDITOR
            return VideoPlayerClass != System.IntPtr.Zero;
            #else
            return false;
            #endif
        }
    }

    public static bool IsPlaying
    {
        get
        {
            if (getIsPlayingMethodId == IntPtr.Zero)
            {
                getIsPlayingMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "getIsPlaying", "()Z");
            }

            return AndroidJNI.CallStaticBooleanMethod(VideoPlayerClass, getIsPlayingMethodId, EmptyParams);
        }
    }

    public static PlabackState CurrentPlaybackState
    {
        get
        {
            if (getCurrentPlaybackStateMethodId == IntPtr.Zero)
            {
                getCurrentPlaybackStateMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "getCurrentPlaybackState", "()I");
            }

            return (PlabackState) AndroidJNI.CallStaticIntMethod(VideoPlayerClass, getCurrentPlaybackStateMethodId, EmptyParams);
        }
    }

    public static long Duration
    {
        get
        {
            if (getDurationMethodId == IntPtr.Zero)
            {
                getDurationMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "getDuration", "()J");
            }

            return AndroidJNI.CallStaticLongMethod(VideoPlayerClass, getDurationMethodId, EmptyParams);
        }
    }

    public static long PlaybackPosition
    {
        get
        {
            if (getPlaybackPositionMethodId == IntPtr.Zero)
            {
                getPlaybackPositionMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "getPlaybackPosition", "()J");
            }

            return AndroidJNI.CallStaticLongMethod(VideoPlayerClass, getPlaybackPositionMethodId, EmptyParams);
        }
        set
        {
            if (setPlaybackPositionMethodId == IntPtr.Zero)
            {
                setPlaybackPositionMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "setPlaybackPosition", "(J)V");
                setPlaybackPositionParams   = new jvalue[1];
            }

            setPlaybackPositionParams[0].j = value;

            AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, setPlaybackPositionMethodId, setPlaybackPositionParams);
        }
    }

    public static void PlayVideo(string path, string drmLicenseUrl, IntPtr surfaceObj)
    {
        if (playVideoMethodId == IntPtr.Zero)
        {
            playVideoMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "playVideo", "(Landroid/content/Context;Ljava/lang/String;Ljava/lang/String;Landroid/view/Surface;)V");
            playVideoParams   = new jvalue[4];
        }

        var filePathJString      = AndroidJNI.NewStringUTF(path);
        var drmLicenseUrlJString = AndroidJNI.NewStringUTF(drmLicenseUrl);

        playVideoParams[0].l = Activity;
        playVideoParams[1].l = filePathJString;
        playVideoParams[2].l = drmLicenseUrlJString;
        playVideoParams[3].l = surfaceObj;
        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, playVideoMethodId, playVideoParams);

        AndroidJNI.DeleteLocalRef(filePathJString);
        AndroidJNI.DeleteLocalRef(drmLicenseUrlJString);
    }

    public static void Stop()
    {
        if (stopMethodId == IntPtr.Zero)
        {
            stopMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "stop", "()V");
        }

        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, stopMethodId, EmptyParams);
    }

    public static void Play()
    {
        if (resumeMethodId == IntPtr.Zero)
        {
            resumeMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "resume", "()V");
        }

        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, resumeMethodId, EmptyParams);
    }

    public static void Pause()
    {
        if (pauseMethodId == IntPtr.Zero)
        {
            pauseMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "pause", "()V");
        }

        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, pauseMethodId, EmptyParams);
    }

    public static void SetPlaybackSpeed(float speed)
    {
        if (setPlaybackSpeedMethodId == IntPtr.Zero)
        {
            setPlaybackSpeedMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "setPlaybackSpeed", "(F)V");
            setPlaybackSpeedParams   = new jvalue[1];
        }

        setPlaybackSpeedParams[0].f = speed;
        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, setPlaybackSpeedMethodId, setPlaybackSpeedParams);
    }

    public static void SetLooping(bool looping)
    {
        if (setLoopingMethodId == IntPtr.Zero)
        {
            setLoopingMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "setLooping", "(Z)V");
            setLoopingParams   = new jvalue[1];
        }

        setLoopingParams[0].z = looping;
        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, setLoopingMethodId, setLoopingParams);
    }

    public static void SetListenerRotation(Quaternion rotation)
    {
        if (setListenerRotationQuaternionMethodId == IntPtr.Zero)
        {
            setListenerRotationQuaternionMethodId = AndroidJNI.GetStaticMethodID(VideoPlayerClass, "setListenerRotationQuaternion", "(FFFF)V");
            setListenerRotationQuaternionParams   = new jvalue[4];
        }

        setListenerRotationQuaternionParams[0].f = rotation.x;
        setListenerRotationQuaternionParams[1].f = rotation.y;
        setListenerRotationQuaternionParams[2].f = rotation.z;
        setListenerRotationQuaternionParams[3].f = rotation.w;
        AndroidJNI.CallStaticVoidMethod(VideoPlayerClass, setListenerRotationQuaternionMethodId, setListenerRotationQuaternionParams);
    }
}
