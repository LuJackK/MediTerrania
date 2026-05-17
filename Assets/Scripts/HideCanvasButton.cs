using UnityEngine;

public class HideCanvasButton : MonoBehaviour
{
    public void HideCanvas()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        GameObject target = parentCanvas != null ? parentCanvas.gameObject : transform.parent?.gameObject;

        if (target != null)
        {
            target.SetActive(false);
        }
    }
}
