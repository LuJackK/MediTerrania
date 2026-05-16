using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class AnchorDrag : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Header("Depth Settings")]
    public float minDepthMeters = 5f;
    public float maxDepthMeters = 25f;

    [Header("UI")]
    public RectTransform track;
    public TMP_Text depthText;
    public float anchorXOffset = 0f;

    private RectTransform anchorRect;
    private float topY;
    private float bottomY;

    public float CurrentDepthMeters { get; private set; }

    private void Awake()
    {
        anchorRect = (RectTransform)transform;
        CacheTrackLimits();
        UpdateDepthFromPosition();
    }

    private void OnValidate()
    {
        minDepthMeters = Mathf.Max(0f, minDepthMeters);
        maxDepthMeters = Mathf.Max(minDepthMeters, maxDepthMeters);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        MoveAnchor(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveAnchor(eventData);
    }

    public void RefreshDepth()
    {
        anchorRect ??= (RectTransform)transform;
        CacheTrackLimits();
        UpdateDepthFromPosition();
    }

    private void CacheTrackLimits()
    {
        if (track == null)
        {
            topY = 0f;
            bottomY = 0f;
            return;
        }

        float halfHeight = track.rect.height * 0.5f;
        topY = track.anchoredPosition.y + halfHeight;
        bottomY = track.anchoredPosition.y - halfHeight;
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

        anchorRect.anchoredPosition = new Vector2(track.anchoredPosition.x + anchorXOffset, Mathf.Clamp(localPoint.y, bottomY, topY));
        UpdateDepthFromPosition();
    }

    private void UpdateDepthFromPosition()
    {
        if (anchorRect == null)
        {
            return;
        }

        float normalizedDepth = Mathf.InverseLerp(topY, bottomY, anchorRect.anchoredPosition.y);
        CurrentDepthMeters = Mathf.Lerp(minDepthMeters, maxDepthMeters, normalizedDepth);

        if (depthText != null)
        {
            depthText.text = Mathf.RoundToInt(CurrentDepthMeters) + " m";

            RectTransform depthTextRect = depthText.rectTransform;
            depthTextRect.anchoredPosition = new Vector2(depthTextRect.anchoredPosition.x, anchorRect.anchoredPosition.y);
        }
    }
}
