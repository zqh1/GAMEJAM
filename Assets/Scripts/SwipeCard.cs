using UnityEngine;
using UnityEngine.EventSystems;

// Handles dragging one card panel left or right.
// Attach this to the CardPanel RectTransform.
public class SwipeCard : MonoBehaviour, IDragHandler, IEndDragHandler
{
    public RaisingManager raisingManager;

    [Header("Swipe Settings")]
    public float swipeThreshold = 85f;
    public float previewDeadZone = 85f;

    // The card cannot be dragged farther than this distance from the center.
    public float maxDragDistance = 130f;

    public float maxRotation = 15f;

    private RectTransform rectTransform;
    private Vector2 startPosition;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
        {
            Debug.LogWarning("SwipeCard needs to be attached to a UI object with a RectTransform.");
            return;
        }

        startPosition = rectTransform.anchoredPosition;
    }

    // Moves and tilts the card while the player drags it.
    public void OnDrag(PointerEventData eventData)
    {
        if (!CanSwipe())
        {
            return;
        }

        float currentOffset = rectTransform.anchoredPosition.x - startPosition.x;
        float newOffset = currentOffset + eventData.delta.x;

        // Clamp the card so it cannot be dragged out of the screen.
        newOffset = Mathf.Clamp(newOffset, -maxDragDistance, maxDragDistance);

        rectTransform.anchoredPosition = new Vector2(
            startPosition.x + newOffset,
            startPosition.y
        );

        float rotationPercent = Mathf.Clamp(newOffset / swipeThreshold, -1f, 1f);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, -rotationPercent * maxRotation);

        if (newOffset < -previewDeadZone)
        {
            raisingManager.PreviewChoice(-1);
        }
        else if (newOffset > previewDeadZone)
        {
            raisingManager.PreviewChoice(1);
        }
        else
        {
            raisingManager.PreviewChoice(0);
        }
    }

    // Chooses a side if the card was dragged far enough, otherwise returns it to center.
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!CanSwipe())
        {
            ReturnToCenter();
            return;
        }

        float horizontalOffset = rectTransform.anchoredPosition.x - startPosition.x;

        if (horizontalOffset < -swipeThreshold)
        {
            raisingManager.ChooseLeft();
            ReturnToCenter();
        }
        else if (horizontalOffset > swipeThreshold)
        {
            raisingManager.ChooseRight();
            ReturnToCenter();
        }
        else
        {
            ReturnToCenter();
            raisingManager.ResetChoicePreview();
        }
    }

    private bool CanSwipe()
    {
        if (rectTransform == null)
        {
            return false;
        }

        if (raisingManager == null)
        {
            Debug.LogWarning("SwipeCard is missing a RaisingManager reference.");
            return false;
        }

        return !raisingManager.IsWaitingForNextCard;
    }

    private void ReturnToCenter()
    {
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchoredPosition = startPosition;
        rectTransform.localRotation = Quaternion.identity;
    }
}