﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Class to generation a map from maptiles in the editor.
/// </summary>
public class MapTileGeneratorEditor : MonoBehaviour
{
    [SerializeField]
    private string m_levelToEdit;

    [SerializeField]
    private Vector2 m_levelSize;

    [SerializeField]
    private float m_tileMargin;

    [SerializeField]
    private GameObject m_tilePrefab;

    [SerializeField]
    private Transform m_levelRoot;

    [Serializable]
    private class MapTileTypeAssignment
    {
#pragma warning disable 649
        public MapTileType m_MapTileType;
        public GameObject m_MapTilePrefab;
#pragma warning restore 649
    }

    [SerializeField]
#pragma warning disable 649
    private List<MapTileTypeAssignment> m_mapTileTypeAssignmentList;
#pragma warning restore 649

    private MapGenerationData m_currentlyVisibleMap;

    private void Awake()
    {
        ControllerContainer.MonoBehaviourRegistry.Register(this);
    }

    /// <summary>
    /// Generates the map.
    /// </summary>
    public void GenerateMap()
    {
        ClearMap();

        m_currentlyVisibleMap = ControllerContainer.MapTileGenerationService.GenerateMapGroups(m_levelSize, m_tileMargin, 2);

        ControllerContainer.MapTileGenerationService.LoadGeneratedMap(m_currentlyVisibleMap, m_tilePrefab, m_levelRoot);
    }

    /// <summary>
    /// Loads an existing map.
    /// </summary>
    public void LoadExistingMap(string levelNameToLoad = "")
    {
        string levelToLoad = Application.isPlaying ? levelNameToLoad : m_levelToEdit;

        string assetPath = string.Format("Levels/{0}", levelToLoad);

        MapGenerationData mapGenerationData = GetMapGenerationDataAtPath(assetPath);

        if (mapGenerationData == null)
        {
            Debug.LogErrorFormat("There is no existing map with name: '{0}' in the path: '{1}'", levelToLoad, assetPath);
        }
        else
        {
            ClearMap();
            ControllerContainer.MapTileGenerationService.LoadGeneratedMap(mapGenerationData, m_tilePrefab, m_levelRoot);
            m_currentlyVisibleMap = mapGenerationData;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Gets the map generation data at path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    private MapGenerationData GetMapGenerationDataAtPath(string path)
    {
        MapGenerationData mapGenerationDataToReturn;

        if (Application.isPlaying)
        {
            mapGenerationDataToReturn = (MapGenerationData)Resources.Load(path, typeof(MapGenerationData));
        }
        else
        {
            string assetPath = string.Format("Assets/Resources/{0}.asset", path);

            Debug.LogFormat("Loading asset from path: '{0}'", assetPath);

            mapGenerationDataToReturn = (MapGenerationData)AssetDatabase.LoadAssetAtPath(assetPath, typeof(MapGenerationData));
        }

        return mapGenerationDataToReturn;
    }

#else

    /// <summary>
    /// Gets the map generation data at path.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns></returns>
    private MapGenerationData GetMapGenerationDataAtPath(string path)
    {
        return (MapGenerationData)Resources.Load(path, typeof(MapGenerationData));
    }

#endif

#if UNITY_EDITOR

    /// <summary>
    /// Gets or sets the name of the save map under level.
    /// </summary>
    /// <value>
    /// The name of the save map under level.
    /// </value>
    public void SaveMapUnderLevelName()
    {
        if (m_currentlyVisibleMap == null)
        {
            Debug.LogError("No map is currently generated! Cannot save it!");
            return;
        }

        if (m_levelToEdit == string.Empty)
        {
            Debug.LogError("Please enter a name this map should be saved under!");
            return;
        }

        m_currentlyVisibleMap.m_LevelName = m_levelToEdit;

        string pathToAsset = string.Format("Assets/Levels/{0}.asset", m_currentlyVisibleMap.m_LevelName);

        AssetDatabase.CreateAsset(m_currentlyVisibleMap, pathToAsset);
    }

#endif

    /// <summary>
    /// Clears the previous generation.
    /// </summary>
    public void ClearMap()
    {
        List<GameObject> gameObjectsToKill = new List<GameObject>();

        foreach (Transform child in m_levelRoot)
        {
            gameObjectsToKill.Add(child.gameObject);
        }

        for (int i = 0; i < gameObjectsToKill.Count; i++)
        {
            DestroyImmediate(gameObjectsToKill[i]);
        }

#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(SceneManager.GetSceneByName("Battleground"));
        }
#endif
    }

    /// <summary>
    /// Returns a prefab from the MapTileTypeAssignment based on the given MapTileType.
    /// </summary>
    /// <param name="mapTileType">Type of the map tile.</param>
    /// <returns></returns>
    public GameObject GetPrefabOfMapTileType(MapTileType mapTileType)
    {
        MapTileTypeAssignment mapTileTypeAssignment = m_mapTileTypeAssignmentList.Find(prefab => prefab.m_MapTileType == mapTileType);

        return mapTileTypeAssignment == null ? null : mapTileTypeAssignment.m_MapTilePrefab;
    }
}
