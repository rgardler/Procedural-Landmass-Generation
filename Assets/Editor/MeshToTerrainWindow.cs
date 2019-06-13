﻿using UnityEngine;
using UnityEditor;
using System;
using System.Linq;

/// <summary>
/// Convert an object to a terrain. 
/// 
/// Based on code from the Unity Wiki at http://wiki.unity3d.com/index.php?title=Object2Terrain
/// 
/// Licensed under the Creative Commons Share Alike http://creativecommons.org/licenses/by-sa/3.0/
/// 
/// -Resolution: the resolution of the generated terrain.
/// -Add terrain: add blank terrain to the front/back, left/right and above.Handy if you want to create additional content.Increase this if the edge of the terrain is cut off.
/// -Shift height: move the terrain up or down. The terrain GameObject will stay in the same position.Range limited to the vertical terrain size.
/// -Bottom up: terrain is generated from the bottom up.This will ensure a 1 to 1 resemblance of the source object.
/// -Top down: terrain is generated from top to bottom.This will stretch the terrain if a larger y value of "Add terrain" is used.This is the original mode but gives somewhat odd results.
/// -Create terrain: start generating the terrain. Depending on the resolution, this might take a while.
/// 
/// </summary>
public class MeshToTerrainWindow : EditorWindow
{

    [MenuItem("Digital Painting/Terrain/Convert Preview to Terrain", false, 2000)]
    static void OpenWindow()
    {
        EditorWindow.GetWindow<MeshToTerrainWindow>(false);
    }

    private string terrainName = "Generated Terrain";
    private int spacing = 10;
    private int resolution = 512;
    private Vector3 addTerrain;
    int bottomTopRadioSelected = 0;
    static string[] bottomTopRadio = new string[] { "Bottom Up", "Top Down" };
    private float shiftHeight = 0f;
    
    void OnGUI()
    {
        GUILayout.Space(spacing);
        
        GUILayout.BeginVertical("box");
        GUILayout.Label("Advanced Settings");
        resolution = EditorGUILayout.IntField("Resolution", resolution);
        addTerrain = EditorGUILayout.Vector3Field("Add terrain", addTerrain);
        shiftHeight = EditorGUILayout.Slider("Shift height", shiftHeight, -1f, 1f);
        bottomTopRadioSelected = GUILayout.SelectionGrid(bottomTopRadioSelected, bottomTopRadio, bottomTopRadio.Length, EditorStyles.radioButton);
        GUILayout.EndHorizontal();

        if (Terrain.activeTerrain)
        {   
            if (GUILayout.Button("Delete Terrain"))
            {
                DestroyImmediate(Terrain.activeTerrain.gameObject);
            }
        }
        else
        {
            if (GUILayout.Button("Create Terrain"))
            {
                CreateTerrain();
            }
        }
    }

    delegate void CleanUp();

    void CreateTerrain()
    {
        //fire up the progress bar
        ShowProgressBar(1, 100);

        TerrainData terrainData = new TerrainData();

        terrainData.heightmapResolution = resolution;
        GameObject terrainObject = Terrain.CreateTerrainGameObject(terrainData);

        Undo.RegisterCreatedObjectUndo(terrainObject, "Preview to Terrain");

        GameObject preview = GameObject.Find("Preview Mesh");
        MeshCollider collider = preview.GetComponent<MeshCollider>();
        CleanUp cleanUp = null;

        //Add a collider to our source object if it does not exist.
        //Otherwise raycasting doesn't work.
        if (!collider)
        {
            collider = preview.AddComponent<MeshCollider>();
            cleanUp = () => DestroyImmediate(collider);
        }

        Bounds bounds = collider.bounds;
        float sizeFactor = collider.bounds.size.y / (collider.bounds.size.y + addTerrain.y);
        terrainData.size = collider.bounds.size + addTerrain;
        bounds.size = new Vector3(terrainData.size.x, collider.bounds.size.y, terrainData.size.z);

        // Do raycasting samples over the object to see what terrain heights should be
        float[,] heights = new float[terrainData.heightmapWidth, terrainData.heightmapHeight];
        Ray ray = new Ray(new Vector3(bounds.min.x, bounds.max.y + bounds.size.y, bounds.min.z), -Vector3.up);
        RaycastHit hit = new RaycastHit();
        float meshHeightInverse = 1 / bounds.size.y;
        Vector3 rayOrigin = ray.origin;

        int maxHeight = heights.GetLength(0);
        int maxLength = heights.GetLength(1);

        Vector2 stepXZ = new Vector2(bounds.size.x / maxLength, bounds.size.z / maxHeight);

        for (int zCount = 0; zCount < maxHeight; zCount++)
        {

            ShowProgressBar(zCount, maxHeight);

            for (int xCount = 0; xCount < maxLength; xCount++)
            {

                float height = 0.0f;

                if (collider.Raycast(ray, out hit, bounds.size.y * 3))
                {

                    height = (hit.point.y - bounds.min.y) * meshHeightInverse;
                    height += shiftHeight;

                    //bottom up
                    if (bottomTopRadioSelected == 0)
                    {

                        height *= sizeFactor;
                    }

                    //clamp
                    if (height < 0)
                    {

                        height = 0;
                    }
                }

                heights[zCount, xCount] = height;
                rayOrigin.x += stepXZ[0];
                ray.origin = rayOrigin;
            }

            rayOrigin.z += stepXZ[1];
            rayOrigin.x = bounds.min.x;
            ray.origin = rayOrigin;
        }

        terrainData.SetHeights(0, 0, heights);

        AssetDatabase.CreateAsset(terrainData, "Assets/Terrain Assets/" + terrainName + ".asset");

        GameObject terrain = GameObject.Find("Terrain");
        terrain.name = terrainName;

        //preview.transform.root.gameObject.SetActive(false);

        EditorUtility.ClearProgressBar();

        if (cleanUp != null)
        {
            cleanUp();
        }
    }

    void ShowProgressBar(float progress, float maxProgress)
    {
        float p = progress / maxProgress;
        EditorUtility.DisplayProgressBar("Creating Terrain...", Mathf.RoundToInt(p * 100f) + " %", p);
    }
}