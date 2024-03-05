using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using Codice.Client.BaseCommands;
using Unity.VisualScripting;
using System.Security.Policy;
using UnityEditor.EditorTools;


public class DungeonCreator : EditorWindow
{
    [MenuItem("Tools/DungeonCreator")]
    public static void OpenWindow() => GetWindow(typeof(DungeonCreator));

    private List<GameObject> placedRooms = new();
    //The room we are placing
    private GameObject _roomToPlace;
    public GameObject RoomToPlace
    {
        set
        {
            if (value != null && !value.CompareTag("Room")) return;

            _roomToPlace = value;
        }
        get
        {
            return _roomToPlace;
        }
    }


    //The position where a room is placed on spawn
    private readonly Vector3 spawnPosition = new(0, 0, 40);

    //SerializedThings
    private SerializedObject so;
    const int gridSize = 10;
    const int cellOffset = gridSize / 2;
    const float gridExtent = 32;

    //Variables
    private GameObject[] roomPrefabs;
    private void OnEnable()
    {
        //Events
        SceneView.duringSceneGui += DuringSceneGUI;
        Selection.selectionChanged += () => RoomToPlace = Selection.activeGameObject;
        EditorApplication.hierarchyChanged += SetupList;

        //Serialize
        so = new SerializedObject(this);

        //Prendiamo il percorso in cui sono messi i prefab e li salviamo in un array.
        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs/Rooms" }); //Cerchiamo la cartella e il tipo dei file tramite il filtro t:prefab
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        roomPrefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        //Prendiamo i valori salvati se ce ne sono.
        //FIXME: Tengo come esempio toglila dopo
        //gridSize = EditorPrefs.GetInt("dungeonCreator_gridSize", 10);

        //Ci prediamo tutte le stanze già piazzate
        SetupList();
    }

    private void SetupList()
    {
        placedRooms = GameObject.FindGameObjectsWithTag("Room").ToList();
    }

    private void OnDisable()
    {
        //Events
        SceneView.duringSceneGui -= DuringSceneGUI;
        Selection.selectionChanged -= () => RoomToPlace = Selection.activeGameObject;
        EditorApplication.hierarchyChanged -= SetupList;


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
        Debug.Log(RoomToPlace);

        DrawRoomSelectionGUI();

        //Qualsiasi cosa che deve accadere ad ogni repaint.
        if (Event.current.type == EventType.Repaint)
        {
            DrawGrid(sceneView);
        }

        if (Event.current.type == EventType.MouseUp)
        {
            //FIXME: Troviamo un modo per non usare i tag? una tag class maybe?
            if (Selection.activeGameObject != null && Selection.activeGameObject.CompareTag("Room"))
            {
                //Reset dell'asse delle Y
                Vector3 roomPos = RoomToPlace.transform.position;
                //All on the same height
                roomPos.y = 0;

                //Snap
                roomPos = Round(roomPos);
                if (CanPlace(roomPos))
                {
                    RoomToPlace.transform.SetPositionAndRotation(roomPos, Quaternion.identity);
                    placedRooms.Add(RoomToPlace);
                }
                else
                {
                    RoomToPlace.transform.SetPositionAndRotation(spawnPosition, Quaternion.identity);
                }
            }
        }

        if (Event.current.type == EventType.MouseDrag)
        {
            sceneView.Repaint();
        }
    }

    private bool CanPlace(Vector3 snappedPos)
    {
        if (snappedPos.x < -30 || snappedPos.x > 30) return false;
        if (snappedPos.z < -30 || snappedPos.z > 30) return false;

        if (placedRooms.Count <= 0) return true;

        foreach (GameObject room in placedRooms)
        {
            if (room.transform.position == snappedPos)
                return false;
        }
        return true;
    }

    private void DrawRoomSelectionGUI()
    {
        Handles.BeginGUI(); //Start 2d block GUI in scene view

        Vector2 rectSize = SceneView.lastActiveSceneView.camera.pixelRect.center;
        Rect rect = new(rectSize.x - roomPrefabs.Length / 2 * 60, 10, 60, 60);

        for (int i = 0; i < roomPrefabs.Length; i++)
        {
            GameObject prefab = roomPrefabs[i];
            EditorGUILayout.BeginVertical();
            Texture icon = AssetPreview.GetAssetPreview(prefab);

            if (GUI.Button(rect, new GUIContent(icon)))
            {
                //If we're placing a new room this will be replaced with the new choice
                // if(roomToPlace) Destroy(roomToPlace);

                if (_roomToPlace != null && _roomToPlace.transform.position == spawnPosition)
                    DestroyImmediate(_roomToPlace);

                RoomToPlace = SpawnRoom(prefab);
                Selection.activeGameObject = RoomToPlace;
            }

            EditorGUILayout.EndVertical();

            rect.x += rect.width + 2;
        }

        Handles.EndGUI();
    }

    private Vector3 Round(Vector3 v)
    {
        v /= gridSize;

        v.x = Mathf.Round(v.x);
        v.y = Mathf.Round(v.y);
        v.z = Mathf.Round(v.z);

        return v * gridSize;
    }

    //FIXME: Static?
    private GameObject SpawnRoom(GameObject room)
    {
        GameObject spawnedRoom = (GameObject)PrefabUtility.InstantiatePrefab(room);
        spawnedRoom.transform.position = spawnPosition;
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

        for (int i = -1; i < lineCount; i++)
        {
            int offsetIndex = i - halfLineCount;

            float xCoord = offsetIndex * gridSize + cellOffset;
            float zCoord0 = halfLineCount * gridSize + cellOffset;
            float zCoord1 = -halfLineCount * gridSize - cellOffset;

            Vector3 p0 = new Vector3(xCoord, 0f, zCoord0);
            Vector3 p1 = new Vector3(xCoord, 0f, zCoord1);

            Handles.DrawAAPolyLine(p0, p1);

            Vector3 p2 = new Vector3(zCoord0, 0f, xCoord);
            Vector3 p3 = new Vector3(zCoord1, 0f, xCoord);

            Handles.DrawAAPolyLine(p2, p3);
        }

        /*
        // Di quante linee avremo bisogno per formare una griglia?
        int lineCount = Mathf.RoundToInt((gridExtent * 2) / gridSize);

        if (lineCount % 2 == 0)
        {
            lineCount++;
        }

        int halfLineCount = lineCount / 2;


        for (int i = -halfLineCount; i <= halfLineCount; i++)
        {
            // Calcola la coordinata x
            float xCoord = i * gridSize + gridSize / 2f;

            // Disegna la linea verticale
            Vector3 p0 = new Vector3(xCoord, 0f, -gridExtent);
            Vector3 p1 = new Vector3(xCoord, 0f, gridExtent);
            Handles.DrawAAPolyLine(p0, p1);
            
            // Disegna la linea orizzontale
            Vector3 p2 = new Vector3(-gridExtent, 0f, xCoord);
            Vector3 p3 = new Vector3(gridExtent, 0f, xCoord);
            Handles.DrawAAPolyLine(p2, p3);
        }
        */
    }
}
