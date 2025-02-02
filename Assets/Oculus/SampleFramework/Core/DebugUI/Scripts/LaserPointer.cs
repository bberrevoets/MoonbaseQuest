// ==========================================================================
//  Author: B.N. Berrevoets (bert)
//  Created: 20/11/2020
// ==========================================================================

using UnityEngine;

public class LaserPointer : OVRCursor
{
    public enum LaserBeamBehavior
    {
        On,             // laser beam always on
        Off,            // laser beam always off
        OnWhenHitTarget // laser beam only activates when hit valid target
    }

    public  GameObject cursorVisual;
    public  float      maxLength = 10.0f;
    private Vector3    _endPoint;
    private Vector3    _forward;
    private bool       _hitTarget;

    private LaserBeamBehavior _laserBeamBehavior;
    private Vector3           _startPoint;
    private LineRenderer      lineRenderer;
    private bool              m_restoreOnInputAcquired = false;

    public LaserBeamBehavior laserBeamBehavior
    {
        set
        {
            _laserBeamBehavior = value;
            if (laserBeamBehavior == LaserBeamBehavior.Off || laserBeamBehavior == LaserBeamBehavior.OnWhenHitTarget)
            {
                lineRenderer.enabled = false;
            }
            else
            {
                lineRenderer.enabled = true;
            }
        }
        get => _laserBeamBehavior;
    }

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    private void Start()
    {
        if (cursorVisual)
        {
            cursorVisual.SetActive(false);
        }

        OVRManager.InputFocusAcquired += OnInputFocusAcquired;
        OVRManager.InputFocusLost     += OnInputFocusLost;
    }

    private void LateUpdate()
    {
        lineRenderer.SetPosition(0, _startPoint);
        if (_hitTarget)
        {
            lineRenderer.SetPosition(1, _endPoint);
            UpdateLaserBeam(_startPoint, _endPoint);
            if (cursorVisual)
            {
                cursorVisual.transform.position = _endPoint;
                cursorVisual.SetActive(true);
            }
        }
        else
        {
            UpdateLaserBeam(_startPoint, _startPoint + maxLength * _forward);
            lineRenderer.SetPosition(1, _startPoint + maxLength * _forward);
            if (cursorVisual)
            {
                cursorVisual.SetActive(false);
            }
        }
    }

    private void OnDisable()
    {
        if (cursorVisual)
        {
            cursorVisual.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        OVRManager.InputFocusAcquired -= OnInputFocusAcquired;
        OVRManager.InputFocusLost     -= OnInputFocusLost;
    }

    public override void SetCursorStartDest(Vector3 start, Vector3 dest, Vector3 normal)
    {
        _startPoint = start;
        _endPoint   = dest;
        _hitTarget  = true;
    }

    public override void SetCursorRay(Transform t)
    {
        _startPoint = t.position;
        _forward    = t.forward;
        _hitTarget  = false;
    }

    // make laser beam a behavior with a prop that enables or disables
    private void UpdateLaserBeam(Vector3 start, Vector3 end)
    {
        if (laserBeamBehavior == LaserBeamBehavior.Off) { }
        else if (laserBeamBehavior == LaserBeamBehavior.On)
        {
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
        else if (laserBeamBehavior == LaserBeamBehavior.OnWhenHitTarget)
        {
            if (_hitTarget)
            {
                if (!lineRenderer.enabled)
                {
                    lineRenderer.enabled = true;
                    lineRenderer.SetPosition(0, start);
                    lineRenderer.SetPosition(1, end);
                }
            }
            else
            {
                if (lineRenderer.enabled)
                {
                    lineRenderer.enabled = false;
                }
            }
        }
    }

    public void OnInputFocusLost()
    {
        if (gameObject && gameObject.activeInHierarchy)
        {
            m_restoreOnInputAcquired = true;
            gameObject.SetActive(false);
        }
    }

    public void OnInputFocusAcquired()
    {
        if (m_restoreOnInputAcquired && gameObject)
        {
            m_restoreOnInputAcquired = false;
            gameObject.SetActive(true);
        }
    }
}
