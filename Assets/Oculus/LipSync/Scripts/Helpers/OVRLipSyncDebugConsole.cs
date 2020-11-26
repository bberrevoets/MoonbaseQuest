// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections;

using UnityEngine;
using UnityEngine.UI;

public class OVRLipSyncDebugConsole : MonoBehaviour
{
    // Our instance to allow this script to be called without a direct connection.
    private static OVRLipSyncDebugConsole s_Instance  = null;
    public         ArrayList              messages    = new ArrayList();
    public         int                    maxMessages = 15; // The max number of messages displayed
    public         Text                   textMsg;          // text string to display
    private        float                  clearTimeout = 0.0f;

    // Clear timeout
    private bool clearTimeoutOn = false;

    /// <summary>
    ///     Gets the instance.
    /// </summary>
    /// <value>The instance.</value>
    public static OVRLipSyncDebugConsole instance
    {
        get
        {
            if (s_Instance == null)
            {
                s_Instance = FindObjectOfType(typeof(OVRLipSyncDebugConsole)) as OVRLipSyncDebugConsole;

                if (s_Instance == null)
                {
                    var console = new GameObject();
                    console.AddComponent<OVRLipSyncDebugConsole>();
                    console.name = "OVRLipSyncDebugConsole";
                    s_Instance   = FindObjectOfType(typeof(OVRLipSyncDebugConsole)) as OVRLipSyncDebugConsole;
                }
            }

            return s_Instance;
        }
    }

    /// <summary>
    ///     Awake this instance.
    /// </summary>
    private void Awake()
    {
        s_Instance = this;
        Init();
    }

    /// <summary>
    ///     Update this instance.
    /// </summary>
    private void Update()
    {
        if (clearTimeoutOn)
        {
            clearTimeout -= Time.deltaTime;
            if (clearTimeout < 0.0f)
            {
                Clear();
                clearTimeout   = 0.0f;
                clearTimeoutOn = false;
            }
        }
    }

    /// <summary>
    ///     Init this instance.
    /// </summary>
    public void Init()
    {
        if (textMsg == null)
        {
            Debug.LogWarning("DebugConsole Init WARNING::UI text not set. Will not be able to display anything.");
        }

        Clear();
    }

    //+++++++++ INTERFACE FUNCTIONS ++++++++++++++++++++++++++++++++

    /// <summary>
    ///     Log the specified message.
    /// </summary>
    /// <param name="message">Message.</param>
    public static void Log(string message)
    {
        instance.AddMessage(message, Color.white);
    }

    /// <summary>
    ///     Log the specified message and color.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="color">Color.</param>
    public static void Log(string message, Color color)
    {
        instance.AddMessage(message, color);
    }

    /// <summary>
    ///     Clear this instance.
    /// </summary>
    public static void Clear()
    {
        instance.ClearMessages();
    }

    /// <summary>
    ///     Calls clear after a certain time.
    /// </summary>
    /// <param name="timeToClear">Time to clear.</param>
    public static void ClearTimeout(float timeToClear)
    {
        instance.SetClearTimeout(timeToClear);
    }

    //++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++

    /// <summary>
    ///     Adds the message.
    /// </summary>
    /// <param name="message">Message.</param>
    /// <param name="color">Color.</param>
    public void AddMessage(string message, Color color)
    {
        messages.Add(message);

        if (textMsg != null)
        {
            textMsg.color = color;
        }

        Display();
    }

    /// <summary>
    ///     Clears the messages.
    /// </summary>
    public void ClearMessages()
    {
        messages.Clear();
        Display();
    }

    /// <summary>
    ///     Sets the clear timeout.
    /// </summary>
    /// <param name="timeout">Timeout.</param>
    public void SetClearTimeout(float timeout)
    {
        clearTimeout   = timeout;
        clearTimeoutOn = true;
    }

    /// <summary>
    // Prunes the array to fit within the maxMessages limit
    /// </summary>
    private void Prune()
    {
        int diff;
        if (messages.Count > maxMessages)
        {
            if (messages.Count <= 0)
            {
                diff = 0;
            }
            else
            {
                diff = messages.Count - maxMessages;
            }

            messages.RemoveRange(0, diff);
        }
    }

    /// <summary>
    ///     Display this instance.
    /// </summary>
    private void Display()
    {
        if (messages.Count > maxMessages)
        {
            Prune();
        }

        if (textMsg != null)
        {
            textMsg.text = ""; // Clear text out
            var x = 0;

            while (x < messages.Count)
            {
                textMsg.text += (string) messages[x];
                textMsg.text += '\n';
                x            += 1;
            }
        }
    }
}
