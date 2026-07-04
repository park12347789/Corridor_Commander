using System;
using System.Collections.Generic;
using CorridorCommander.PlayerCombat;
using CorridorCommander.PlayerItems;
using UnityEditor;
using UnityEngine;

namespace CorridorCommander.EditorTools
{
    public static class PrefabIconWorkFinisher
    {
        private const int IconSize = 256;
        private const string OutputFolder = "Assets/hansol/04_Art/UI/Icons/Generated";
        private const string PendingRequestPath = "Temp/finish_prefab_icon_work.request";

        [InitializeOnLoadMethod]
        private static void RunPendingRequest()
        {
            if (!System.IO.File.Exists(PendingRequestPath))
            {
                return;
            }

            System.IO.File.Delete(PendingRequestPath);
            Debug.Log("[PrefabIconWorkFinisher] Pending request found.");
            EditorApplication.update += RunPendingRequestOnUpdate;
        }

        private static void RunPendingRequestOnUpdate()
        {
            EditorApplication.update -= RunPendingRequestOnUpdate;
            FinishPrefabIconWork();
        }

        [MenuItem("Corridor Commander/UI/Finish Prefab Icon Work")]
        public static void FinishFromMenu()
        {
            FinishPrefabIconWork();
        }

        public static void FinishPrefabIconWork()
        {
            EnsureFolder(OutputFolder);

            Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();

            CaptureAndStore(sprites, "icon_buildable_turret", "Assets/hansol/03_Prefabs/Turret_Basic.prefab");
            CaptureAndStore(sprites, "icon_buildable_turret_rapid", "Assets/hansol/03_Prefabs/Turret_Rapid.prefab");
            CaptureAndStore(sprites, "icon_buildable_turret_long_range", "Assets/hansol/03_Prefabs/Turret_LongRange.prefab");
            CaptureAndStore(sprites, "icon_buildable_barricade", "Assets/hansol/03_Prefabs/Barricade_Basic.prefab");
            CaptureAndStore(sprites, "icon_buildable_mortar", "Assets/hansol/03_Prefabs/TEMP_Mortar_Basic.prefab");
            CaptureAndStore(sprites, "icon_buildable_mortar_rapid", "Assets/hansol/03_Prefabs/TEMP_Mortar_Rapid.prefab");
            CaptureAndStore(sprites, "icon_buildable_mortar_heavy", "Assets/hansol/03_Prefabs/TEMP_Mortar_Heavy.prefab");
            CaptureAndStore(sprites, "icon_buildable_saw_trap", "Assets/hansol/03_Prefabs/SawTrap_Turret_Yellow.prefab");

            CaptureWeaponIcon(sprites, "icon_weapon_ak2", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_AK2.asset");
            CaptureWeaponIcon(sprites, "icon_weapon_beam_cannon", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_BeamCannon.asset");
            CaptureWeaponIcon(sprites, "icon_weapon_grenade_launcher", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_GrenadeLauncher.asset");
            CaptureWeaponIcon(sprites, "icon_weapon_laser_gun", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_LaserGun.asset");
            CaptureWeaponIcon(sprites, "icon_weapon_pistol", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_Pistol.asset");
            CaptureWeaponIcon(sprites, "icon_weapon_shotgun", "Assets/junhee/10_ScriptableObjects/Weapons/Weapon_Shotgun.asset");

            CaptureAndStore(sprites, "icon_item_medkit", "Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/Survival_T/Medkit.prefab");
            CaptureAndStore(sprites, "icon_item_grenade", "Assets/polyperfect/Low Poly Ultimate Pack/_T/Prefabs_T/Weapons_T/Grenade_Frag.prefab");

            CaptureAndStore(sprites, "icon_squad_dummy_rifle", "Assets/hansol/03_Prefabs/AlliedDummy/TEMP_AlliedDummy.prefab");
            CaptureAndStore(sprites, "icon_squad_dummy_blue", "Assets/hansol/03_Prefabs/AlliedDummy/TEMP_AlliedDummy_Blue.prefab");
            CaptureAndStore(sprites, "icon_squad_dummy_red", "Assets/hansol/03_Prefabs/AlliedDummy/TEMP_AlliedDummy_laser_gun.prefab");
            CaptureAndStore(sprites, "icon_squad_dummy_purple", "Assets/hansol/03_Prefabs/AlliedDummy/TEMP_AlliedDummy_purple.prefab");

            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Turret.asset", "icon", sprites["icon_buildable_turret"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Turret_Rapid.asset", "icon", sprites["icon_buildable_turret_rapid"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Turret_LongRange.asset", "icon", sprites["icon_buildable_turret_long_range"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Barricade.asset", "icon", sprites["icon_buildable_barricade"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Mortar.asset", "icon", sprites["icon_buildable_mortar"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Mortar_Rapid.asset", "icon", sprites["icon_buildable_mortar_rapid"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_Mortar_Heavy.asset", "icon", sprites["icon_buildable_mortar_heavy"]);
            AssignSprite("Assets/hansol/09_Settings/Construction/Buildable_SawTrap.asset", "icon", sprites["icon_buildable_saw_trap"]);

            AssignSprite("Assets/hansol/09_Settings/Skills/Skill_Mortar.asset", "icon", sprites["icon_buildable_mortar"]);
            AssignSprite("Assets/hansol/09_Settings/Skills/Skill_Mortar_Rapid.asset", "icon", sprites["icon_buildable_mortar_rapid"]);
            AssignSprite("Assets/hansol/09_Settings/Skills/Skill_Mortar_Heavy.asset", "icon", sprites["icon_buildable_mortar_heavy"]);

            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_AK2.asset", "icon", sprites["icon_weapon_ak2"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_BeamCannon.asset", "icon", sprites["icon_weapon_beam_cannon"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_GrenadeLauncher.asset", "icon", sprites["icon_weapon_grenade_launcher"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_LaserGun.asset", "icon", sprites["icon_weapon_laser_gun"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_Pistol.asset", "icon", sprites["icon_weapon_pistol"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Weapons/Weapon_Shotgun.asset", "icon", sprites["icon_weapon_shotgun"]);

            AssignSprite("Assets/junhee/10_ScriptableObjects/Items/Item_Medkit.asset", "icon", sprites["icon_item_medkit"]);
            AssignSprite("Assets/junhee/10_ScriptableObjects/Items/Item_Grenade.asset", "icon", sprites["icon_item_grenade"]);

            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Items.asset", "weapon_ak2", sprites["icon_weapon_ak2"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Items.asset", "item_heal", sprites["icon_item_medkit"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Items.asset", "item_grenade", sprites["icon_item_grenade"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Upgrades.asset", "upgrade_mortar_install", sprites["icon_buildable_mortar"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Upgrades.asset", "upgrade_barricade_level2", sprites["icon_buildable_barricade"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Upgrades.asset", "unlock_saw_trap_turret", sprites["icon_buildable_saw_trap"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Squad.asset", "squad_dummy_rifle", sprites["icon_squad_dummy_rifle"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Squad.asset", "squad_dummy_blue", sprites["icon_squad_dummy_blue"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Squad.asset", "squad_dummy_red", sprites["icon_squad_dummy_red"]);
            AssignOfferIcon("Assets/hansol/09_Settings/Shops/SupportTruck_Squad.asset", "squad_dummy_purple", sprites["icon_squad_dummy_purple"]);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PrefabIconWorkFinisher] Prefab icon work finished.");
        }

        private static void CaptureWeaponIcon(Dictionary<string, Sprite> sprites, string iconName, string weaponAssetPath)
        {
            LoadStoredIcon(sprites, iconName);
        }

        private static void CaptureAndStore(Dictionary<string, Sprite> sprites, string iconName, string prefabPath)
        {
            LoadStoredIcon(sprites, iconName);
        }

        private static void LoadStoredIcon(Dictionary<string, Sprite> sprites, string iconName)
        {
            string iconPath = OutputFolder + "/" + iconName + ".png";

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Stored sprite missing: " + iconPath);
            }

            sprites[iconName] = sprite;
        }

        private static void CapturePrefabIcon(string prefabPath, string outputPath)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                throw new InvalidOperationException("Prefab missing: " + prefabPath);
            }

            GameObject instance = null;
            GameObject cameraObject = null;
            GameObject lightObject = null;
            RenderTexture renderTexture = null;
            Texture2D texture = null;

            try
            {
                instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (instance == null)
                {
                    instance = UnityEngine.Object.Instantiate(prefab);
                }

                instance.hideFlags = HideFlags.HideAndDontSave;
                instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                Bounds bounds = CalculateBounds(instance);
                Vector3 center = bounds.center;
                float maxSize = Mathf.Max(0.25f, bounds.size.x, bounds.size.y, bounds.size.z);

                cameraObject = new GameObject("PrefabIconCamera");
                cameraObject.hideFlags = HideFlags.HideAndDontSave;
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                camera.orthographic = true;
                camera.orthographicSize = maxSize * 0.72f;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = maxSize * 8f;

                Quaternion rotation = Quaternion.Euler(28f, -35f, 0f);
                cameraObject.transform.position = center + rotation * (Vector3.back * maxSize * 3f);
                cameraObject.transform.rotation = rotation;

                lightObject = new GameObject("PrefabIconLight");
                lightObject.hideFlags = HideFlags.HideAndDontSave;
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.35f;
                lightObject.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

                renderTexture = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
                renderTexture.antiAliasing = 4;
                camera.targetTexture = renderTexture;
                camera.Render();

                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = renderTexture;
                texture = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
                texture.Apply();
                RenderTexture.active = previous;

                System.IO.File.WriteAllBytes(outputPath, texture.EncodeToPNG());
                AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
                ConfigureAsSprite(outputPath);
            }
            finally
            {
                if (texture != null)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                if (renderTexture != null)
                {
                    renderTexture.Release();
                    UnityEngine.Object.DestroyImmediate(renderTexture);
                }

                if (instance != null)
                {
                    UnityEngine.Object.DestroyImmediate(instance);
                }

                if (cameraObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(cameraObject);
                }

                if (lightObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(lightObject);
                }
            }
        }

        private static Bounds CalculateBounds(GameObject root)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bool hasBounds = false;
            Bounds bounds = new Bounds(root.transform.position, Vector3.one);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return bounds;
        }

        private static void ConfigureAsSprite(string assetPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("Texture importer missing: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.maxTextureSize = IconSize;
            importer.SaveAndReimport();
        }

        private static void AssignSprite(string assetPath, string propertyName, Sprite sprite)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
            if (asset == null)
            {
                throw new InvalidOperationException("Asset missing: " + assetPath);
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                throw new InvalidOperationException("Property missing: " + assetPath + "." + propertyName);
            }

            property.objectReferenceValue = sprite;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void AssignOfferIcon(string listAssetPath, string offerId, Sprite sprite)
        {
            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(listAssetPath);
            if (asset == null)
            {
                throw new InvalidOperationException("Offer list missing: " + listAssetPath);
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            SerializedProperty offers = serializedObject.FindProperty("offers");
            if (offers == null || !offers.isArray)
            {
                throw new InvalidOperationException("Offers array missing: " + listAssetPath);
            }

            for (int i = 0; i < offers.arraySize; i++)
            {
                SerializedProperty entry = offers.GetArrayElementAtIndex(i);
                SerializedProperty id = entry.FindPropertyRelative("offerId");
                if (id == null || id.stringValue != offerId)
                {
                    continue;
                }

                SerializedProperty icon = entry.FindPropertyRelative("icon");
                if (icon == null)
                {
                    throw new InvalidOperationException("Offer icon missing: " + listAssetPath + "." + offerId);
                }

                icon.objectReferenceValue = sprite;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
                return;
            }

            throw new InvalidOperationException("Offer id missing: " + listAssetPath + "." + offerId);
        }

        private static void EnsureFolder(string folderPath)
        {
            string[] parts = folderPath.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
