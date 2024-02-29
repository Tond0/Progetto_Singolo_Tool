using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;


public class DungeonCreator : EditorWindow
{
    [MenuItem("Tools/DungeonCreator")]
    public static void OpenWindow() => GetWindow(typeof(DungeonCreator));

    //SerializedThings
    SerializedObject so;


    //Variables
    private GameObject[] rooms;
    private void OnEnable()
    {
        SceneView.duringSceneGui += DuringSceneGUI;
        so = new SerializedObject(this);

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/Rooms" }); //Cerchiamo la cartella e il tipo dei file tramite il filtro t:prefab
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        rooms = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnGUI()
    {
        so.Update();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }
    private void DuringSceneGUI(SceneView sceneView)
    {
        DrawRoomSelectionGUI();


        if (Event.current.type == EventType.MouseMove)
        {
            DrawGrid(sceneView);
            sceneView.Repaint();
        }
    }

    private void DrawRoomSelectionGUI()
    {
        Handles.BeginGUI(); //Start 2d block GUI in scene view

        Vector2 rectSize = SceneView.lastActiveSceneView.camera.pixelRect.center;
        Rect rect = new Rect(rectSize.x - rooms.Length * 45, 50, 90, 90);

        for (int i = 0; i < rooms.Length; i++)
        {
            GameObject prefab = rooms[i];
            EditorGUILayout.BeginVertical();
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            if (GUI.Button(rect, new GUIContent(icon)))
            {

            }

            EditorGUILayout.EndVertical();

            rect.x += rect.width + 2;
        }

        Handles.EndGUI();
    }

    private void DrawGrid(SceneView sceneView)
    {
        sceneView.in2DMode = false;
        sceneView.orthographic = false;

        Grid grid = new();
        grid.cellSize = new(10, 10, 10);
        grid.cellSwizzle = GridLayout.CellSwizzle.XYZ;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        float gridSpacing = 1 * 10f;

        // Draw grid lines along X axis
        for (float x = -sceneView.cameraDistance; x < sceneView.cameraDistance; x += gridSpacing)
        {
            Handles.DrawLine(new Vector3(x, 0f, -sceneView.cameraDistance), new Vector3(x, 0f, sceneView.cameraDistance));
        }

        // Draw grid lines along Z axis
        for (float z = -sceneView.cameraDistance; z < sceneView.cameraDistance; z += gridSpacing)
        {
            Handles.DrawLine(new Vector3(-sceneView.cameraDistance, 0f, z), new Vector3(sceneView.cameraDistance, 0f, z));
        }
    }
}
