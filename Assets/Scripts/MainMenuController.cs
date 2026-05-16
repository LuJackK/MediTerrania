using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "Test1";
    [SerializeField] private Font menuFont;

    private GUIStyle titleStyle;
    private GUIStyle buttonStyle;

    private void Awake()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.clearFlags = CameraClearFlags.SolidColor;
            mainCamera.backgroundColor = Color.black;
        }
    }

    private void OnGUI()
    {
        EnsureStyles();

        GUI.color = new Color(0f, 0f, 0f, 0.45f);
        GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        float titleHeight = 96f;
        float buttonWidth = 260f;
        float buttonHeight = 64f;
        float centerX = Screen.width * 0.5f;
        float centerY = Screen.height * 0.5f;

        Rect titleRect = new Rect(0f, centerY - 125f, Screen.width, titleHeight);
        Rect buttonRect = new Rect(centerX - buttonWidth * 0.5f, centerY + 20f, buttonWidth, buttonHeight);

        GUI.Label(titleRect, "Medi<color=#37D7B2>Terra</color>na", titleStyle);

        if (GUI.Button(buttonRect, "Play Game", buttonStyle))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null && buttonStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            font = menuFont,
            fontSize = 72,
            fontStyle = FontStyle.Bold,
            richText = true,
            normal =
            {
                textColor = Color.white
            }
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            font = menuFont,
            fontSize = 28,
            fontStyle = FontStyle.Bold
        };
    }
}
