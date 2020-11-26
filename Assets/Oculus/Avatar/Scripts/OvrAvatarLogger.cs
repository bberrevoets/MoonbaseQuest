// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Diagnostics;

using UnityEngine;

using Debug = UnityEngine.Debug;

namespace Oculus.Avatar
{
    public static class AvatarLogger
    {
        public const string LogAvatar = "[Avatars] - ";
        public const string Tab       = "    ";

        [Conditional("ENABLE_AVATAR_LOGS")] [Conditional("ENABLE_AVATAR_LOG_BASIC")]
        public static void Log(string logMsg)
        {
            Debug.Log(LogAvatar + logMsg);
        }

        [Conditional("ENABLE_AVATAR_LOGS")] [Conditional("ENABLE_AVATAR_LOG_BASIC")]
        public static void Log(string logMsg, Object context)
        {
            Debug.Log(LogAvatar + logMsg, context);
        }

        [Conditional("ENABLE_AVATAR_LOGS")] [Conditional("ENABLE_AVATAR_LOG_WARNING")]
        public static void LogWarning(string logMsg)
        {
            Debug.LogWarning(LogAvatar + logMsg);
        }

        [Conditional("ENABLE_AVATAR_LOGS")] [Conditional("ENABLE_AVATAR_LOG_ERROR")]
        public static void LogError(string logMsg)
        {
            Debug.LogError(LogAvatar + logMsg);
        }

        [Conditional("ENABLE_AVATAR_LOGS")] [Conditional("ENABLE_AVATAR_LOG_ERROR")]
        public static void LogError(string logMsg, Object context)
        {
            Debug.LogError(LogAvatar + logMsg, context);
        }
    }
}
