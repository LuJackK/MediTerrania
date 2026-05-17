using UnityEngine;

public class HideCanvasButton : MonoBehaviour
{
    public ForwardRaycast raycast;
    public SwimTryOutMovementController swimTryOutMovementController;
    public void HideCanvas()
    {
        Debug.Log("YOU CLICKED IT YOUR NOT CRAZY");
      
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        GameObject target = parentCanvas != null ? parentCanvas.gameObject : transform.parent?.gameObject;

        if (target != null)
        {
            target.SetActive(false);
            raycast.enabled = true;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            swimTryOutMovementController.enabled = true;
        }
    }
}
