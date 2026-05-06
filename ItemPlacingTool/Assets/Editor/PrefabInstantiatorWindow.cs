using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class PrefabInstantiatorWindow : EditorWindow
{
    private List<GameObject> prefabs = new List<GameObject>();//llista dels pref
    private int activePrefabIndex = -1;
    private GameObject parentObject;

    private GameObject lastInstantiated;
    private Vector2 scrollPos;

    [MenuItem("Tools/Instanciador")]//ens permetra posar la nostar tool es com la creacio del scriptables
    public static void ShowWindow()
    {
        GetWindow<PrefabInstantiatorWindow>("Prefab Instantiator");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefabs disponibles", EditorStyles.boldLabel);

        //fem que hagi un boto per afegir prefabs
        if (GUILayout.Button("Afegir Prefab"))
            prefabs.Add(null);//creem un slot de prefab en null

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        for (int i = 0; i < prefabs.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            prefabs[i] = (GameObject)EditorGUILayout.ObjectField(prefabs[i], typeof(GameObject), false);

            //per si esta actiu o no (boto q desactiav o desactiva)
            bool isActive = (activePrefabIndex == i);
            if (GUILayout.Toggle(isActive, "Actiu", GUILayout.Width(55)) != isActive)
                activePrefabIndex = isActive ? -1 : i;

            //Boto per a eliminar el prefab
            if (GUILayout.Button("X", GUILayout.Width(20)))
            {
                prefabs.RemoveAt(i);
                if (activePrefabIndex == i) activePrefabIndex = -1;
                break;
            }

            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        //objecte pare
        GUILayout.Label("Objecte pare de l'escena:", EditorStyles.boldLabel);
        parentObject = (GameObject)EditorGUILayout.ObjectField(parentObject, typeof(GameObject), true);

        GUILayout.Space(10);

        //info del pref q esta actiu
        if (activePrefabIndex >= 0 && activePrefabIndex < prefabs.Count && prefabs[activePrefabIndex] != null)
            EditorGUILayout.HelpBox($"Prefab actiu: {prefabs[activePrefabIndex].name}\nFes clic a l'escena per instanciar.", MessageType.Info);
        else
            EditorGUILayout.HelpBox("Selecciona un prefab actiu.", MessageType.Warning);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (activePrefabIndex < 0 || activePrefabIndex >= prefabs.Count) return;
        GameObject prefab = prefabs[activePrefabIndex];
        if (prefab == null) return;

        Event e = Event.current;

        //recordar q es necesari fer clic per instanciar
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            Vector3 worldPos = GetWorldPosition(e.mousePosition, sceneView);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = worldPos;

            if (parentObject != null)
                instance.transform.SetParent(parentObject.transform, true);

            Undo.RegisterCreatedObjectUndo(instance, "Instanciar Prefab");

            lastInstantiated = instance;

            e.Use();
        }

        //Drag
        if (e.type == EventType.MouseDrag && e.button == 0 && lastInstantiated != null)
        {
            Vector3 dragPos = GetWorldPosition(e.mousePosition, sceneView);
            Vector3 direction = dragPos - lastInstantiated.transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                Undo.RecordObject(lastInstantiated.transform, "Orientar Prefab");
                lastInstantiated.transform.rotation = Quaternion.LookRotation(direction);
            }

            e.Use();
        }

        //MouseUp per a deixar d'orientar
        if (e.type == EventType.MouseUp && e.button == 0)
        {
            lastInstantiated = null;
            e.Use();
        }

        sceneView.Repaint();
    }

    private Vector3 GetWorldPosition(Vector2 mousePos, SceneView sceneView)
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit))
            return hit.point;

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (groundPlane.Raycast(ray, out float distance))
            return ray.GetPoint(distance);

        return Vector3.zero;
    }
}
