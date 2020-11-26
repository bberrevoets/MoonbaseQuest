// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEngine.SceneManagement;

#endif

public class DebugUIBuilder : MonoBehaviour
{
    public delegate bool ActiveUpdate();

    public delegate void OnClick();

    public delegate void OnSlider(float f);

    public delegate void OnToggleValueChange(Toggle t);
    // room for extension:
    // support update funcs
    // fix bug where it seems to appear at a random offset
    // support remove

    // Convenience consts for clarity when using multiple debug panes. 
    // But note that you can an arbitrary number of panes if you add them in the inspector.
    public const int DEBUG_PANE_CENTER = 0;
    public const int DEBUG_PANE_RIGHT  = 1;
    public const int DEBUG_PANE_LEFT   = 2;

    private const float elementSpacing = 16.0f;
    private const float marginH        = 16.0f;
    private const float marginV        = 16.0f;

    public static DebugUIBuilder instance;

    [SerializeField]
    private RectTransform buttonPrefab = null;

    [SerializeField]
    private RectTransform labelPrefab = null;

    [SerializeField]
    private RectTransform sliderPrefab = null;

    [SerializeField]
    private RectTransform dividerPrefab = null;

    [SerializeField]
    private RectTransform togglePrefab = null;

    [SerializeField]
    private RectTransform radioPrefab = null;

    [SerializeField]
    private GameObject uiHelpersToInstantiate = null;

    [SerializeField]
    private Transform[] targetContentPanels = null;

    [SerializeField]
    private List<GameObject> toEnable = null;

    [SerializeField]
    private List<GameObject> toDisable = null;

    public  LaserPointer.LaserBeamBehavior  laserBeamBehavior;
    private List<RectTransform>[]           insertedElements;
    private Vector2[]                       insertPositions;
    private LaserPointer                    lp;
    private LineRenderer                    lr;
    private Vector3                         menuOffset;
    private Dictionary<string, ToggleGroup> radioGroups = new Dictionary<string, ToggleGroup>();

    private bool[]       reEnable;
    private OVRCameraRig rig;

    public void Awake()
    {
        Debug.Assert(instance == null);
        instance   = this;
        menuOffset = transform.position; // TODO: this is unpredictable/busted
        gameObject.SetActive(false);
        rig = FindObjectOfType<OVRCameraRig>();
        for (var i = 0; i < toEnable.Count; ++i)
        {
            toEnable[i].SetActive(false);
        }

        insertPositions = new Vector2[targetContentPanels.Length];
        for (var i = 0; i < insertPositions.Length; ++i)
        {
            insertPositions[i].x = marginH;
            insertPositions[i].y = -marginV;
        }

        insertedElements = new List<RectTransform>[targetContentPanels.Length];
        for (var i = 0; i < insertedElements.Length; ++i)
        {
            insertedElements[i] = new List<RectTransform>();
        }

        if (uiHelpersToInstantiate)
        {
            Instantiate(uiHelpersToInstantiate);
        }

        lp = FindObjectOfType<LaserPointer>();
        if (!lp)
        {
            Debug.LogError("Debug UI requires use of a LaserPointer and will not function without it. Add one to your scene, or assign the UIHelpers prefab to the DebugUIBuilder in the inspector.");
            return;
        }

        lp.laserBeamBehavior = laserBeamBehavior;

        if (!toEnable.Contains(lp.gameObject))
        {
            toEnable.Add(lp.gameObject);
        }

        GetComponent<OVRRaycaster>().pointer = lp.gameObject;
        lp.gameObject.SetActive(false);
        #if UNITY_EDITOR
        var scene = SceneManager.GetActiveScene().name;
        OVRPlugin.SendEvent("debug_ui_builder",
                ((scene == "DebugUI") ||
                 (scene == "DistanceGrab") ||
                 (scene == "OVROverlay") ||
                 (scene == "Locomotion")).ToString(),
                "sample_framework");
        #endif
    }

    public void Show()
    {
        Relayout();
        gameObject.SetActive(true);
        transform.position = rig.transform.TransformPoint(menuOffset);
        var newEulerRot = rig.transform.rotation.eulerAngles;
        newEulerRot.x         = 0.0f;
        newEulerRot.z         = 0.0f;
        transform.eulerAngles = newEulerRot;

        if (reEnable == null || reEnable.Length < toDisable.Count)
        {
            reEnable = new bool[toDisable.Count];
        }

        reEnable.Initialize();
        var len = toDisable.Count;
        for (var i = 0; i < len; ++i)
        {
            if (toDisable[i])
            {
                reEnable[i] = toDisable[i].activeSelf;
                toDisable[i].SetActive(false);
            }
        }

        len = toEnable.Count;
        for (var i = 0; i < len; ++i)
        {
            toEnable[i].SetActive(true);
        }

        var numPanels = targetContentPanels.Length;
        for (var i = 0; i < numPanels; ++i)
        {
            targetContentPanels[i].gameObject.SetActive(insertedElements[i].Count > 0);
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);

        for (var i = 0; i < reEnable.Length; ++i)
        {
            if (toDisable[i] && reEnable[i])
            {
                toDisable[i].SetActive(true);
            }
        }

        var len = toEnable.Count;
        for (var i = 0; i < len; ++i)
        {
            toEnable[i].SetActive(false);
        }
    }

    // Currently a slow brute-force method that lays out every element. 
    // As this is intended as a debug UI, it might be fine, but there are many simple optimizations we can make.
    private void Relayout()
    {
        for (var panelIdx = 0; panelIdx < targetContentPanels.Length; ++panelIdx)
        {
            var canvasRect = targetContentPanels[panelIdx].GetComponent<RectTransform>();
            var elems      = insertedElements[panelIdx];
            var elemCount  = elems.Count;
            var x          = marginH;
            var y          = -marginV;
            var maxWidth   = 0.0f;
            for (var elemIdx = 0; elemIdx < elemCount; ++elemIdx)
            {
                var r = elems[elemIdx];
                r.anchoredPosition =  new Vector2(x, y);
                y                  -= (r.rect.height + elementSpacing);
                maxWidth           =  Mathf.Max(r.rect.width + 2 * marginH, maxWidth);
            }

            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, maxWidth);
            canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   -y + marginV);
        }
    }

    private void AddRect(RectTransform r, int targetCanvas)
    {
        if (targetCanvas > targetContentPanels.Length)
        {
            Debug.LogError("Attempted to add debug panel to canvas " + targetCanvas + ", but only " + targetContentPanels.Length + " panels were provided. Fix in the inspector or pass a lower value for target canvas.");
            return;
        }

        r.transform.SetParent(targetContentPanels[targetCanvas], false);
        insertedElements[targetCanvas].Add(r);
        if (gameObject.activeInHierarchy)
        {
            Relayout();
        }
    }

    public RectTransform AddButton(string label, OnClick handler, int targetCanvas = 0)
    {
        var buttonRT = Instantiate(buttonPrefab).GetComponent<RectTransform>();
        var button   = buttonRT.GetComponentInChildren<Button>();
        button.onClick.AddListener(delegate { handler(); });
        ((Text) (buttonRT.GetComponentsInChildren(typeof(Text), true)[0])).text = label;
        AddRect(buttonRT, targetCanvas);
        return buttonRT;
    }

    public RectTransform AddLabel(string label, int targetCanvas = 0)
    {
        var rt = Instantiate(labelPrefab).GetComponent<RectTransform>();
        rt.GetComponent<Text>().text = label;
        AddRect(rt, targetCanvas);
        return rt;
    }

    public RectTransform AddSlider(string label, float min, float max, OnSlider onValueChanged, bool wholeNumbersOnly = false, int targetCanvas = 0)
    {
        var rt = Instantiate(sliderPrefab);
        var s  = rt.GetComponentInChildren<Slider>();
        s.minValue = min;
        s.maxValue = max;
        s.onValueChanged.AddListener(delegate(float f) { onValueChanged(f); });
        s.wholeNumbers = wholeNumbersOnly;
        AddRect(rt, targetCanvas);
        return rt;
    }

    public RectTransform AddDivider(int targetCanvas = 0)
    {
        var rt = Instantiate(dividerPrefab);
        AddRect(rt, targetCanvas);
        return rt;
    }

    public RectTransform AddToggle(string label, OnToggleValueChange onValueChanged, int targetCanvas = 0)
    {
        var rt = Instantiate(togglePrefab);
        AddRect(rt, targetCanvas);
        var buttonText = rt.GetComponentInChildren<Text>();
        buttonText.text = label;
        var t = rt.GetComponentInChildren<Toggle>();
        t.onValueChanged.AddListener(delegate { onValueChanged(t); });
        return rt;
    }

    public RectTransform AddToggle(string label, OnToggleValueChange onValueChanged, bool defaultValue, int targetCanvas = 0)
    {
        var rt = Instantiate(togglePrefab);
        AddRect(rt, targetCanvas);
        var buttonText = rt.GetComponentInChildren<Text>();
        buttonText.text = label;
        var t = rt.GetComponentInChildren<Toggle>();
        t.isOn = defaultValue;
        t.onValueChanged.AddListener(delegate { onValueChanged(t); });
        return rt;
    }

    public RectTransform AddRadio(string label, string group, OnToggleValueChange handler, int targetCanvas = 0)
    {
        var rt = Instantiate(radioPrefab);
        AddRect(rt, targetCanvas);
        var buttonText = rt.GetComponentInChildren<Text>();
        buttonText.text = label;
        var tb = rt.GetComponentInChildren<Toggle>();
        if (group == null)
        {
            @group = "default";
        }

        ToggleGroup tg      = null;
        var         isFirst = false;
        if (!radioGroups.ContainsKey(group))
        {
            tg                 = tb.gameObject.AddComponent<ToggleGroup>();
            radioGroups[group] = tg;
            isFirst            = true;
        }
        else
        {
            tg = radioGroups[group];
        }

        tb.group = tg;
        tb.isOn  = isFirst;
        tb.onValueChanged.AddListener(delegate { handler(tb); });
        return rt;
    }

    public void ToggleLaserPointer(bool isOn)
    {
        if (lp)
        {
            if (isOn)
            {
                lp.enabled = true;
            }
            else
            {
                lp.enabled = false;
            }
        }
    }
}
