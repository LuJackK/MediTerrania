using UnityEngine;
using UnityEngine.InputSystem;

public class ModeHandler : MonoBehaviour
{
    public SwimTryOutMovementController swimTryOutMovementController;
    
    private Test1CohesiveUiBootstrap ui;
    private bool qToggle = false;

    void Start()
    {
        ui = FindFirstObjectByType<Test1CohesiveUiBootstrap>();
    }

    void Update()
    {
        UpdateToggles();
    }

    private void UpdateToggles()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.qKey.wasPressedThisFrame)
        {
            qToggle = !qToggle;
            Debug.Log($"Q toggled: {qToggle}");
        }

        if (qToggle)
        {
            ui?.DisableUi();
            swimTryOutMovementController.enabled = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            ui?.EnableUi();
            swimTryOutMovementController.enabled = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}