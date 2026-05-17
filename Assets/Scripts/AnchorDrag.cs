using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class AnchorDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Depth Settings")]
    public float minDepthMeters = 5f;
    public float maxDepthMeters = 25f;

    [Header("UI")]
    public RectTransform track;
    public Image trackFill;
    public TMP_Text depthText;
    public float anchorXOffset = 0f;

    [Header("Depth Environment")]
    public Camera controlledCamera;
    public Image darknessOverlay;
    public float surfaceHiddenDepthMeters = 15f;
    public float cameraDropAtHiddenSurface = 2f;
    public float surfaceRiseAtMaxDepth = 28f;
    public float surfaceRiseAcceleration = 1.7f;
    public Color shallowDarknessColor = new(0f, 0.05f, 0.09f, 0.03f);
    public Color deepDarknessColor = new(0f, 0.025f, 0.07f, 0.34f);

    private RectTransform anchorRect;
    private readonly List<DepthShiftTarget> surfaceShiftTargets = new();
    private float topY;
    private float bottomY;
    private float lockedAnchorX;
    private float lockedTextX;
    private Vector3 shallowCameraPosition;
    private bool hasShallowCameraPosition;
    private float lastNormalizedDepth;

    public float CurrentDepthMeters { get; private set; }

    private void Awake()
    {
        anchorRect = (RectTransform)transform;
        CacheLockedPositions();
        CacheCameraPosition();
        CacheTrackLimits();
        UpdateDepthFromPosition();
    }

    private void OnValidate()
    {
        minDepthMeters = Mathf.Max(0f, minDepthMeters);
        maxDepthMeters = Mathf.Max(minDepthMeters, maxDepthMeters);
        surfaceHiddenDepthMeters = Mathf.Clamp(surfaceHiddenDepthMeters, minDepthMeters, maxDepthMeters);
        cameraDropAtHiddenSurface = Mathf.Max(0f, cameraDropAtHiddenSurface);
        surfaceRiseAtMaxDepth = Mathf.Max(0f, surfaceRiseAtMaxDepth);
        surfaceRiseAcceleration = Mathf.Max(0.01f, surfaceRiseAcceleration);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);
    }

    private void LateUpdate()
    {
        ApplyDepthEnvironment(lastNormalizedDepth);
    }

    public void MoveToPointer(PointerEventData eventData)
    {
        MoveAnchor(eventData);
    }

    public void RefreshDepth()
    {
        anchorRect ??= (RectTransform)transform;
        CacheLockedPositions();
        CacheCameraPosition();
        CacheTrackLimits();
        UpdateDepthFromPosition();
    }

    public void CaptureCurrentCameraAsShallow()
    {
        controlledCamera ??= Camera.main;
        if (controlledCamera == null)
        {
            return;
        }

        shallowCameraPosition = controlledCamera.transform.position;
        hasShallowCameraPosition = true;
    }

    private void CacheLockedPositions()
    {
        if (anchorRect == null)
        {
            return;
        }

        lockedAnchorX = anchorRect.anchoredPosition.x;
        if (track != null)
        {
            lockedAnchorX = GetTrackCenterInAnchorParent().x + anchorXOffset;
        }

        if (depthText != null)
        {
            lockedTextX = depthText.rectTransform.anchoredPosition.x;
        }
    }

    private void CacheTrackLimits()
    {
        if (track == null || anchorRect == null || anchorRect.parent == null)
        {
            topY = 0f;
            bottomY = 0f;
            return;
        }

        float halfHeight = track.rect.height * 0.5f;
        RectTransform parentRect = (RectTransform)anchorRect.parent;
        Vector3 topWorld = track.TransformPoint(new Vector3(0f, halfHeight, 0f));
        Vector3 bottomWorld = track.TransformPoint(new Vector3(0f, -halfHeight, 0f));

        topY = parentRect.InverseTransformPoint(topWorld).y;
        bottomY = parentRect.InverseTransformPoint(bottomWorld).y;
    }

    private void MoveAnchor(PointerEventData eventData)
    {
        if (anchorRect == null || track == null)
        {
            return;
        }

        RectTransform parentRect = (RectTransform)anchorRect.parent;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
        {
            return;
        }

        anchorRect.anchoredPosition = new Vector2(lockedAnchorX, Mathf.Clamp(localPoint.y, bottomY, topY));
        UpdateDepthFromPosition();
    }

    private void UpdateDepthFromPosition()
    {
        if (anchorRect == null)
        {
            return;
        }

        float normalizedDepth = Mathf.InverseLerp(topY, bottomY, anchorRect.anchoredPosition.y);
        lastNormalizedDepth = normalizedDepth;
        CurrentDepthMeters = Mathf.Lerp(minDepthMeters, maxDepthMeters, normalizedDepth);

        if (trackFill != null)
        {
            trackFill.fillAmount = normalizedDepth;
        }

        if (depthText != null)
        {
            depthText.text = Mathf.RoundToInt(CurrentDepthMeters) + " m";

            RectTransform depthTextRect = depthText.rectTransform;
            depthTextRect.anchoredPosition = new Vector2(lockedTextX, anchorRect.anchoredPosition.y);
        }

        ApplyDepthEnvironment(normalizedDepth);
    }

    private Vector2 GetTrackCenterInAnchorParent()
    {
        if (track == null || anchorRect == null || anchorRect.parent == null)
        {
            return anchorRect != null ? anchorRect.anchoredPosition : Vector2.zero;
        }

        RectTransform parentRect = (RectTransform)anchorRect.parent;
        return parentRect.InverseTransformPoint(track.TransformPoint(Vector3.zero));
    }

    private void CacheCameraPosition()
    {
        controlledCamera ??= Camera.main;
        if (controlledCamera == null || hasShallowCameraPosition)
        {
            return;
        }

        shallowCameraPosition = controlledCamera.transform.position;
        hasShallowCameraPosition = true;
    }

    private void ApplyDepthEnvironment(float normalizedDepth)
    {
        if (darknessOverlay != null)
        {
            darknessOverlay.color = Color.Lerp(shallowDarknessColor, deepDarknessColor, normalizedDepth);
        }

        ApplySurfaceDepthShift(normalizedDepth);

        controlledCamera ??= Camera.main;
        if (controlledCamera == null)
        {
            return;
        }

        CacheCameraPosition();
        float cameraDepthT = Mathf.InverseLerp(minDepthMeters, surfaceHiddenDepthMeters, CurrentDepthMeters);
        controlledCamera.transform.position = shallowCameraPosition + Vector3.down * (cameraDropAtHiddenSurface * cameraDepthT);
    }

    private void ApplySurfaceDepthShift(float normalizedDepth)
    {
        CacheSurfaceShiftTargets();
        float acceleratedDepth = 1f - Mathf.Pow(1f - Mathf.Clamp01(normalizedDepth), surfaceRiseAcceleration);
        Vector3 shift = Vector3.up * (surfaceRiseAtMaxDepth * acceleratedDepth);

        for (int i = surfaceShiftTargets.Count - 1; i >= 0; i--)
        {
            DepthShiftTarget target = surfaceShiftTargets[i];
            if (target.Transform == null)
            {
                surfaceShiftTargets.RemoveAt(i);
                continue;
            }

            target.Transform.position = target.StartPosition + shift;
        }
    }

    private void CacheSurfaceShiftTargets()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        for (int i = 0; i < transforms.Length; i++)
        {
            Transform target = transforms[i];
            if (target == null || target.parent != null || !IsSurfaceShiftTarget(target.name) || HasSurfaceShiftTarget(target))
            {
                continue;
            }

            surfaceShiftTargets.Add(new DepthShiftTarget(target, target.position));
        }
    }

    private bool HasSurfaceShiftTarget(Transform transform)
    {
        for (int i = 0; i < surfaceShiftTargets.Count; i++)
        {
            if (surfaceShiftTargets[i].Transform == transform)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsSurfaceShiftTarget(string objectName)
    {
        return objectName == "SeaCeeling"
            || objectName == "SeaCeiling";
    }

    private readonly struct DepthShiftTarget
    {
        public DepthShiftTarget(Transform transform, Vector3 startPosition)
        {
            Transform = transform;
            StartPosition = startPosition;
        }

        public Transform Transform { get; }
        public Vector3 StartPosition { get; }
    }
}
