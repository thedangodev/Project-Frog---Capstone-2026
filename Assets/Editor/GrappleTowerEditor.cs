using UnityEditor;

[CustomEditor(typeof(GrappleTower))]
[CanEditMultipleObjects]   // <-- Add this
public class GrappleTowerEditor : Editor
{
    SerializedProperty towerTypeProp;
    SerializedProperty fireFieldsProp;
    SerializedProperty iceFieldsProp;
    SerializedProperty windFieldsProp;

    private void OnEnable()
    {
        towerTypeProp = serializedObject.FindProperty("towerType");
        fireFieldsProp = serializedObject.FindProperty("fireFields");
        iceFieldsProp = serializedObject.FindProperty("iceFields");
        windFieldsProp = serializedObject.FindProperty("windFields");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(towerTypeProp);
        EditorGUILayout.Space();

        TowerType type = (TowerType)towerTypeProp.enumValueIndex;

        switch (type)
        {
            case TowerType.Fire:
                DrawFireFields();
                break;

            case TowerType.Ice:
                DrawIceFields();
                break;

            case TowerType.Wind:
                DrawWindFields();
                break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawFireFields()
    {
        EditorGUILayout.LabelField("Fire Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(fireFieldsProp.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(fireFieldsProp.FindPropertyRelative("burnDuration"));
        EditorGUILayout.PropertyField(fireFieldsProp.FindPropertyRelative("burnTickRate"));
    }

    private void DrawIceFields()
    {
        EditorGUILayout.LabelField("Ice Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(iceFieldsProp.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(iceFieldsProp.FindPropertyRelative("damageMultiplier"));
        EditorGUILayout.PropertyField(iceFieldsProp.FindPropertyRelative("slowMultiplier"));
        EditorGUILayout.PropertyField(iceFieldsProp.FindPropertyRelative("slowDuration"));
    }

    private void DrawWindFields()
    {
        EditorGUILayout.LabelField("Wind Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(windFieldsProp.FindPropertyRelative("damage"));
        EditorGUILayout.PropertyField(windFieldsProp.FindPropertyRelative("damageMultiplier"));
    }
}
