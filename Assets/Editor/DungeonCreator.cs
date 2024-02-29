using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Codice.Client.BaseCommands;


public class DungeonCreator : EditorWindow
{
    [MenuItem("Tools/DungeonCreator")]
    public static void OpenWindow() => GetWindow(typeof(DungeonCreator));

    //SerializedThings
    private SerializedObject so;
    const int gridSize = 10;
    const float gridExtent = 32;

    //Variables
    private GameObject[] rooms;
    private void OnEnable()
    {
        //Events
        SceneView.duringSceneGui += DuringSceneGUI;
        Selection.selectionChanged += Repaint;

        //Serialize
        so = new SerializedObject(this);

        //Prendiamo il percorso in cui sono messi i prefab e li salviamo in un array.
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/Rooms" }); //Cerchiamo la cartella e il tipo dei file tramite il filtro t:prefab
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        rooms = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        //Prendiamo i valori salvati se ce ne sono.
        //FIXME: Tengo come esempio toglila dopo
        //gridSize = EditorPrefs.GetInt("dungeonCreator_gridSize", 10);


    }

    private void OnDisable()
    {
        //Events
        SceneView.duringSceneGui -= DuringSceneGUI;
        Selection.selectionChanged -= Repaint;


        //Salviamo le impostazioni così che quando la window verrà aperta avremo tutto come prima
        EditorPrefs.SetInt("dungeonCreator_gridSize", gridSize);
    }


    //Cosa succede sulla finestra.
    private void OnGUI()
    {
        so.Update();

        so.ApplyModifiedProperties();

        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {
            GUI.FocusControl(null);
            Repaint();
        }
    }

    //Cosa succede nella scena
    private void DuringSceneGUI(SceneView sceneView)
    {
        GameObject selectedRoom = DrawRoomSelectionGUI();
        if (selectedRoom != null)
        {
            if (Event.current.type == EventType.MouseDrag)
            {
                Vector3 pos = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                selectedRoom.transform.SetPositionAndRotation(pos, Quaternion.identity);
            }
        }

        //Qualsiasi cosa che deve accadere ad ogni repaint.
        if (Event.current.type == EventType.Repaint)
        {
            DrawGrid(sceneView);
        }

        if (Event.current.type == EventType.MouseMove)
        {
            sceneView.Repaint();
        }
    }

    private GameObject DrawRoomSelectionGUI()
    {
        GameObject spawnedRoom = null;

        Handles.BeginGUI(); //Start 2d block GUI in scene view

        Vector2 rectSize = SceneView.lastActiveSceneView.camera.pixelRect.center;
        Rect rect = new Rect(rectSize.x - rooms.Length * 50, 10, 60, 60);

        for (int i = 0; i < rooms.Length; i++)
        {
            GameObject prefab = rooms[i];
            EditorGUILayout.BeginVertical();
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            if (GUI.Button(rect, new GUIContent(icon)))
            {
                spawnedRoom = SpawnRoom(prefab);
                //Selection.activeGameObject = spawnedRoom;
            }

            EditorGUILayout.EndVertical();

            rect.x += rect.width + 2;
        }

        Handles.EndGUI();

        return spawnedRoom;
    }

    private GameObject SpawnRoom(GameObject room)
    {
        GameObject spawnedRoom = (GameObject)PrefabUtility.InstantiatePrefab(room);
        Undo.RegisterCreatedObjectUndo(spawnedRoom, "Room Spawn");
        return spawnedRoom;
    }

    private void DrawGrid(SceneView sceneView)
    {
        //Di quante linee avremo bisogno per formare una griglia?
        int lineCount = Mathf.RoundToInt((gridExtent * 2) / gridSize);

        if (lineCount % 2 == 0)
        {
            lineCount++;
        }

        int halfLineCount = lineCount / 2;

        for (int i = 0; i < lineCount; i++)
        {
            int offsetIndex = i - halfLineCount;

            float xCoord = offsetIndex * gridSize;
            float zCoord0 = halfLineCount * gridSize;
            float zCoord1 = -halfLineCount * gridSize;

            Vector3 p0 = new Vector3(xCoord, 0f, zCoord0);
            Vector3 p1 = new Vector3(xCoord, 0f, zCoord1);

            Handles.DrawAAPolyLine(p0, p1);

            Vector3 p2 = new Vector3(zCoord0, 0f, xCoord);
            Vector3 p3 = new Vector3(zCoord1, 0f, xCoord);
            Handles.DrawAAPolyLine(p2, p3);
        }
    }
}
