﻿using System;
using UnityEngine;
using WizardsCode.Plugin;
using WizardsCode.Plugin.Terrain;
using WizardsCode.Terrain;

namespace WizardsCode.Validation
{
    public class Terrain_ProceduralTerrain_PluginValidation : ValidationTest<Terrain_PluginManager>
    {
        public override ValidationTest<Terrain_PluginManager> Instance => new Terrain_ProceduralTerrain_PluginValidation();

        internal override Type ProfileType => typeof(Terrain_ProceduralTerrain_Profile);
        
        internal override bool PostFieldCustomValidations(AbstractPluginManager pluginManager) {
            bool isPass = base.PostFieldCustomValidations(pluginManager);
            
            if (UnityEngine.Terrain.activeTerrain)
            {
                AddOrUpdateAsPass("Terrain is required.", pluginManager, "There is a terrain in the scene.");
            }
            else
            {
                ResolutionCallback callback = new ResolutionCallback(new ProfileCallback(GenerateTerrain));
                AddOrUpdateAsError("Terrain is required.", pluginManager, "There is no terrain in the scene.", callback);
                isPass = false;
            }

            return isPass;
        }

        private void GenerateTerrain()
        {
            bool destroyPreview = false;
            string terrainName = "Generated Terrain";

            TerrainGeneratorPreview terrainPreview = GameObject.FindObjectOfType<TerrainGeneratorPreview>();
            if (terrainPreview == null)
            {
                destroyPreview = true;
                Terrain_PluginManager manager = GameObject.FindObjectOfType<Terrain_PluginManager>();
                Terrain_ProceduralTerrain_Profile profile = (Terrain_ProceduralTerrain_Profile)manager.Profile;
                profile.terrainGenerator.InstantiatePrefab();
                terrainPreview = profile.terrainGenerator.Instance.GetComponent< TerrainGeneratorPreview>();
            }

            terrainPreview.DrawMapInEditor();

            TerrainData data = MeshToTerrain.CreateTerrainData(terrainName, 512, Vector3.zero, 0);
            GameObject terrainObject = UnityEngine.Terrain.CreateTerrainGameObject(data);
            terrainObject.name = terrainName;

            if (destroyPreview)
            {
                GameObject.DestroyImmediate(terrainPreview.gameObject);
            }
        }
    }
}
