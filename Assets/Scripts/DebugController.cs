using UnityEngine;

public class DebugController : MonoBehaviour
{
    public CombatStatistics combatStats;

    private bool showDebug;

    // Strings are needed because TextField works with strings
    private string playerSpeedInput;
    private string tongueSpeedInput;

    private void Start()
    {
        // Initialize input fields with current values
        playerSpeedInput = combatStats.playerSpeed.ToString();
        tongueSpeedInput = combatStats.tongueExtendSpeed.ToString();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            showDebug = !showDebug;
        }
    }

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Box(new Rect(10, 10, 400, 180), "Debug Menu");

        GUILayout.BeginArea(new Rect(20, 40, 380, 140));

        GUILayout.Label("Player Speed:");
        playerSpeedInput = GUILayout.TextField(playerSpeedInput);

        GUILayout.Label("Tongue Extend Speed:");
        tongueSpeedInput = GUILayout.TextField(tongueSpeedInput);

        GUILayout.Space(10);

        if (GUILayout.Button("Apply Changes"))
        {
            ApplyChanges();
        }

        GUILayout.EndArea();
    }

    private void ApplyChanges()
    {
        float parsedValue;

        if (float.TryParse(playerSpeedInput, out parsedValue))
        {
            combatStats.playerSpeed = parsedValue;
        }

        if (float.TryParse(tongueSpeedInput, out parsedValue))
        {
            combatStats.tongueExtendSpeed = parsedValue;
        }
    }
}
