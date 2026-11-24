#if UNITY_EDITOR
using Boreatek.Core;
using Boreatek.Data;
using Boreatek.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Boreatek.EditorTools
{
    /// <summary>
    /// Herramientas del menú superior 'Boreatek' en Unity Editor para poblar la escena
    /// y generar los activos ScriptableObject en Assets/_Project/Data/.
    /// </summary>
    public static class BoreatekSceneSetupEditor
    {
        [MenuItem("Boreatek/1. Configurar Manager en Escena Actual (SampleScene)")]
        public static void SetupSceneManagers()
        {
            BoreatekWorldBuilder builder = Object.FindAnyObjectByType<BoreatekWorldBuilder>();
            if (builder == null)
            {
                GameObject mgr = new GameObject("Boreatek_WorldManager");
                mgr.AddComponent<DayNightCycleManager>();
                builder = mgr.AddComponent<BoreatekWorldBuilder>();
                Undo.RegisterCreatedObjectUndo(mgr, "Create Boreatek World Manager");
            }

            builder.BuildOrRefreshWorld();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("[BOREATEK] Mundo 3D Low-Poly, Biomas, Cuevas N1-N3 y Duende Ingeniero construidos y guardados en SampleScene.");
        }

        [MenuItem("Boreatek/2. Generar ScriptableObjects Base en Assets/_Project/Data")]
        public static void GenerateBaseScriptableObjectAssets()
        {
            CreateResourceAsset("SO_Res_CrystallizedWood.asset", ResourceType.CrystallizedWood, "Madera Cristalizada",
                "Madera densa de los Pinos Cristalizados de la tundra exterior.", 6, new Color(0.79f, 0.91f, 1.0f));
            CreateResourceAsset("SO_Res_PolarScrap.asset", ResourceType.PolarScrap, "Chatarra Polar",
                "Engranajes y placas rescatadas de trineos estrellados.", 12, new Color(0.69f, 0.49f, 0.31f));
            CreateResourceAsset("SO_Res_BlackCoal.asset", ResourceType.BlackCoal, "Carbón Negro",
                "Combustible básico para los hornos y calderas de la aldea.", 15, new Color(0.20f, 0.22f, 0.25f));
            CreateResourceAsset("SO_Res_CandyCrystal.asset", ResourceType.CandyCrystal, "Cristal de Caramelo",
                "Mineral energético rojo brillante que alimenta herramientas y autómatas.", 28, new Color(0.90f, 0.22f, 0.27f));
            CreateResourceAsset("SO_Res_PolarIron.asset", ResourceType.PolarIron, "Hierro Polar",
                "Metal azulado templado bajo cero para herramientas y maquinaria avanzada.", 45, new Color(0.45f, 0.58f, 0.72f));
            CreateResourceAsset("SO_Res_DeepBlueIce.asset", ResourceType.DeepBlueIce, "Hielo Azul Profundo",
                "Cristal glacial hipercomprimido de Nivel 2 para enfriar reactores.", 80, new Color(0.37f, 0.66f, 0.83f));
            CreateResourceAsset("SO_Res_FrozenChestSupply.asset", ResourceType.FrozenChestSupply, "Reliquia de Cofre Congelado",
                "Suministros de alto valor rescatados tras derretir un Cofre Congelado.", 135, new Color(1.0f, 0.78f, 0.22f));
            CreateResourceAsset("SO_Res_LuminousCore.asset", ResourceType.LuminousCore, "Núcleo Luminoso Abismal",
                "Esfera geotérmica de magma festivo del Nivel 3 para La Gran Nochebuena.", 220, new Color(1.0f, 0.38f, 0.05f), true);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BOREATEK] ScriptableObjects de recursos generados en Assets/_Project/Data/Resources/.");
        }

        private static void CreateResourceAsset(
            string fileName,
            ResourceType type,
            string displayName,
            string desc,
            int coinValue,
            Color color,
            bool insured = false)
        {
            string path = $"Assets/_Project/Data/Resources/{fileName}";
            ResourceDataSO existing = AssetDatabase.LoadAssetAtPath<ResourceDataSO>(path);
            if (existing != null)
            {
                return;
            }

            ResourceDataSO asset = ScriptableObject.CreateInstance<ResourceDataSO>();
            asset.InitializeRuntime(type, displayName, desc, coinValue, color, insured);
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
#endif
