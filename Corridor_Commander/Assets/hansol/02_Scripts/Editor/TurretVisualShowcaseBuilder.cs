using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretVisualShowcaseBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_visual_showcase.unity";
        private const string MortarLineupScenePath = "Assets/hansol/01_Scenes/test1/mortar_asset_lineup.unity";
        private const string ActiveTurretShowcaseScenePath = "Assets/hansol/01_Scenes/test1/turret_active_showcase.unity";
        private const string ActiveMortarShowcaseScenePath = "Assets/hansol/01_Scenes/test1/mortar_active_showcase.unity";
        private const string ActiveRootVerifyScenePath = "Assets/hansol/01_Scenes/test1/actual_root_turret_mortar_verify.unity";
        private const string PrefabFolder = "Assets/hansol/03_Prefabs/Showcase";
        private const string MaterialFolder = "Assets/hansol/04_Materials/Showcase";
        private static readonly Vector3 ShowcaseOrigin = new Vector3(1000f, 0f, 1000f);
        private static readonly Vector3 MortarLineupOrigin = new Vector3(2000f, 0f, 2000f);

        private static readonly SceneValidationSpec[] ActiveShowcaseSceneSpecs =
        {
            new SceneValidationSpec(ActiveTurretShowcaseScenePath, "Main Camera", "Showcase_AttackTargets"),
            new SceneValidationSpec(ActiveMortarShowcaseScenePath, "Main Camera", "MortarActiveShowcaseRoot"),
            new SceneValidationSpec(ActiveRootVerifyScenePath, "Main Camera", "Directional Light"),
        };

        private const string ExistingBasicTurret = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string ExistingRapidTurret = "Assets/hansol/03_Prefabs/Turret_Rapid.prefab";
        private const string ExistingLongRangeTurret = "Assets/hansol/03_Prefabs/Turret_LongRange.prefab";
        private const string ExistingBasicMortar = "Assets/hansol/03_Prefabs/TEMP_Mortar_Basic.prefab";
        private const string ExistingRapidMortar = "Assets/hansol/03_Prefabs/TEMP_Mortar_Rapid.prefab";
        private const string ExistingHeavyMortar = "Assets/hansol/03_Prefabs/TEMP_Mortar_Heavy.prefab";
        private const string ExistingSawTrap = "Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/yellow/saw_trap_yellow.prefab";

        private const string BaseCircleSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Base_Circle_Small.prefab";
        private const string BaseCircleThree = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Base_Circle_Three_Small.prefab";
        private const string BaseSquareSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Base_Square_Small.prefab";
        private const string BaseSquareBig = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Base_Square_Big.prefab";
        private const string BaseStarSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Base_Star_Small.prefab";
        private const string HeadCannonSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Cannon_Small.prefab";
        private const string HeadLaserSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Laser_Small.prefab";
        private const string HeadSniperSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Sniper_Small.prefab";
        private const string HeadArtillerySmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Artillery_Small.prefab";
        private const string HeadArtilleryBig = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Artillery_Big.prefab";
        private const string HeadMissileSmall = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Turret_Head_Missile_Small.prefab";
        private const string TubeShort = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Tube_Short.prefab";
        private const string TubeLong = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Tube_Long.prefab";
        private const string TubeEnd = "Assets/polyperfect/Low Poly Ultimate Pack/_M/Prefabs_M/Scifi_M/Scifi_Tube_End.prefab";
        private const string SawBlade = "Assets/90_ThirdParty/KayKit 1/Packs/KayKit - Platformer Pack (for Unity)/Prefabs/neutral/sawblade.prefab";

        [MenuItem("Corridor Commander/Art/Build Turret Visual Showcase")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildInternal(NewSceneMode.Single);
        }

        [MenuItem("Corridor Commander/Art/Build Turret Visual Showcase No Prompt")]
        public static void BuildForAutomation()
        {
            BuildInternal(HasDirtyLoadedScenes() ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        [MenuItem("Corridor Commander/Art/Build Mortar Asset Lineup No Prompt")]
        public static void BuildMortarLineupForAutomation()
        {
            BuildMortarLineupInternal(HasDirtyLoadedScenes() ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        [MenuItem("Corridor Commander/Art/Validate Active Turret Showcase Scenes")]
        public static void ValidateActiveShowcaseScenes()
        {
            ValidateActiveShowcaseScenesForAutomation();
        }

        public static string ValidateActiveShowcaseScenesForAutomation()
        {
            if (HasDirtyLoadedScenes())
            {
                throw new System.InvalidOperationException("Cannot validate showcase scenes while a loaded scene has unsaved changes.");
            }

            string previousScenePath = SceneManager.GetActiveScene().path;
            List<string> failures = new List<string>();
            try
            {
                for (int i = 0; i < ActiveShowcaseSceneSpecs.Length; i++)
                {
                    ValidateScene(ActiveShowcaseSceneSpecs[i], failures);
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(previousScenePath) && System.IO.File.Exists(previousScenePath))
                {
                    EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);
                }
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Active turret showcase scene validation failed:\n" + string.Join("\n", failures));
            }

            string result = "Active turret showcase scene validation passed. Scenes=" + ActiveShowcaseSceneSpecs.Length;
            Debug.Log(result);
            return result;
        }

        private static void BuildInternal(NewSceneMode mode)
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/hansol/01_Scenes/test1");

            Materials materials = CreateMaterials();
            List<ShowcaseEntry> entries = CreateShowcasePrefabs(materials);

            CloseExistingTargetScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "turret_visual_showcase";
            EditorSceneManager.SetActiveScene(scene);

            CreateScene(entries, materials);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Turret visual showcase built: " + ScenePath);
        }

        private static void BuildMortarLineupInternal(NewSceneMode mode)
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder("Assets/hansol/01_Scenes/test1");

            Materials materials = CreateMaterials();
            List<ShowcaseEntry> entries = CreateMortarLineupPrefabs(materials);

            CloseExistingTargetScene(MortarLineupScenePath);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "mortar_asset_lineup";
            EditorSceneManager.SetActiveScene(scene);

            CreateMortarLineupScene(entries, materials);

            if (!EditorSceneManager.SaveScene(scene, MortarLineupScenePath))
            {
                throw new System.InvalidOperationException("Failed to save scene: " + MortarLineupScenePath);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Mortar asset lineup built: " + MortarLineupScenePath);
        }

        private static List<ShowcaseEntry> CreateShowcasePrefabs(Materials materials)
        {
            return new List<ShowcaseEntry>
            {
                new ShowcaseEntry("turret_basic", "Basic", "existing turret", ExistingBasicTurret, 1.45f, ShowcaseMotionKind.Recoil),
                new ShowcaseEntry("turret_rapid", "Rapid", "existing turret", ExistingRapidTurret, 1.45f, ShowcaseMotionKind.Recoil),
                new ShowcaseEntry("turret_long_range", "Long Range", "existing turret", ExistingLongRangeTurret, 1.45f, ShowcaseMotionKind.Recoil),
                new ShowcaseEntry("saw_trap", "Saw Trap", "existing trap", ExistingSawTrap, 0.55f, ShowcaseMotionKind.Saw),
                new ShowcaseEntry("mortar_basic", "Mortar Basic", "existing mortar", ExistingBasicMortar, 1.3f, ShowcaseMotionKind.Mortar),
                new ShowcaseEntry("mortar_rapid", "Mortar Rapid", "existing mortar", ExistingRapidMortar, 1.3f, ShowcaseMotionKind.Mortar),
                new ShowcaseEntry("mortar_heavy", "Mortar Heavy", "existing mortar", ExistingHeavyMortar, 1.15f, ShowcaseMotionKind.Mortar)
            };
        }

        private static List<ShowcaseEntry> CreateMortarLineupPrefabs(Materials materials)
        {
            return new List<ShowcaseEntry>
            {
                CreateShortTubeMortar(materials),
                CreateLongTubeMortar(materials),
                CreateTwinTubeMortar(materials),
                CreateMissileRackMortar(materials),
                CreateHeavyTubeMortar(materials)
            };
        }

        private static ShowcaseEntry CreateShortTubeMortar(Materials materials)
        {
            return CreateReferenceCrawlerMortar(materials, "Showcase_RefMortar_CrawlerLight", "Crawler Light", "ref_mortar_crawler_light", 0.72f, 1.24f, 0.88f, 0.13f, 1.08f, 1.12f, 0f, 0);
        }

        private static ShowcaseEntry CreateLongTubeMortar(Materials materials)
        {
            return CreateReferenceCrawlerMortar(materials, "Showcase_RefMortar_CrawlerSiege", "Crawler Siege", "ref_mortar_crawler_siege", 0.82f, 1.5f, 1.2f, 0.2f, 0.94f, 1.75f, 0.1f, 1);
        }

        private static ShowcaseEntry CreateTwinTubeMortar(Materials materials)
        {
            return CreateReferenceCrawlerMortar(materials, "Showcase_RefMortar_CrawlerRapid", "Crawler Rapid", "ref_mortar_crawler_rapid", 0.76f, 1.36f, 0.98f, 0.16f, 1f, 0.9f, 0.2f, 2);
        }

        private static ShowcaseEntry CreateMissileRackMortar(Materials materials)
        {
            return CreateReferenceCrawlerMortar(materials, "Showcase_RefMortar_CrawlerArmor", "Crawler Armor", "ref_mortar_crawler_armor", 0.86f, 1.52f, 1.1f, 0.18f, 0.9f, 1.42f, 0.3f, 3);
        }

        private static ShowcaseEntry CreateHeavyTubeMortar(Materials materials)
        {
            return CreateReferenceCrawlerMortar(materials, "Showcase_RefMortar_CrawlerHeavy", "Crawler Heavy", "ref_mortar_crawler_heavy", 0.9f, 1.62f, 1.3f, 0.22f, 0.84f, 2.05f, 0.4f, 4);
        }

        private static ShowcaseEntry CreateReferenceCrawlerMortar(
            Materials materials,
            string rootName,
            string displayName,
            string id,
            float widthScale,
            float lengthScale,
            float tubeScale,
            float tubeRadius,
            float displayScale,
            float cycleDuration,
            float phaseOffset,
            int variant)
        {
            GameObject root = CreateRoot(rootName);

            CreateCube("TrackedUndercarriage", root.transform, new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(1.16f * widthScale, 0.18f, 1.76f * lengthScale), materials.DarkMetal);
            CreateCube("GreenArmoredHull", root.transform, new Vector3(0f, 0.34f, 0.03f), Vector3.zero, new Vector3(0.96f * widthScale, 0.32f, 1.52f * lengthScale), materials.Green);
            CreateCube("RearPowerBlock", root.transform, new Vector3(0f, 0.48f, -0.58f * lengthScale), Vector3.zero, new Vector3(0.76f * widthScale, 0.32f, 0.42f * lengthScale), materials.Olive);
            CreateCube("SlopedTopArmor", root.transform, new Vector3(0f, 0.63f, -0.08f * lengthScale), new Vector3(-10f, 0f, 0f), new Vector3(0.76f * widthScale, 0.16f, 0.98f * lengthScale), materials.Olive);
            CreateCube("LeftSideArmor", root.transform, new Vector3(-0.52f * widthScale, 0.4f, 0.02f), new Vector3(0f, 0f, 7f), new Vector3(0.12f, 0.38f, 1.22f * lengthScale), materials.Green);
            CreateCube("RightSideArmor", root.transform, new Vector3(0.52f * widthScale, 0.4f, 0.02f), new Vector3(0f, 0f, -7f), new Vector3(0.12f, 0.38f, 1.22f * lengthScale), materials.Green);
            CreateCube("TopOrangePanel", root.transform, new Vector3(0.26f * widthScale, 0.76f, 0.02f), new Vector3(-12f, 0f, 0f), new Vector3(0.18f, 0.04f, 0.24f), materials.Orange);
            CreateCube("SideOrangeWarning", root.transform, new Vector3(0.56f * widthScale, 0.48f, 0.32f * lengthScale), new Vector3(0f, 0f, -8f), new Vector3(0.04f, 0.14f, 0.2f), materials.Orange);
            CreateCube("DarkTopSpine", root.transform, new Vector3(0f, 0.77f, -0.05f * lengthScale), new Vector3(-10f, 0f, 0f), new Vector3(0.18f * widthScale, 0.045f, 0.88f * lengthScale), materials.DarkMetal);
            CreateCube("LeftCyanSideStrip", root.transform, new Vector3(-0.575f * widthScale, 0.55f, 0.2f * lengthScale), new Vector3(0f, 0f, 7f), new Vector3(0.025f, 0.045f, 0.58f * lengthScale), materials.CyanGlow);
            CreateCube("RightCyanSideStrip", root.transform, new Vector3(0.575f * widthScale, 0.55f, 0.2f * lengthScale), new Vector3(0f, 0f, -7f), new Vector3(0.025f, 0.045f, 0.58f * lengthScale), materials.CyanGlow);
            CreateCube("RearExhaustLeft", root.transform, new Vector3(-0.2f * widthScale, 0.47f, -0.82f * lengthScale), Vector3.zero, new Vector3(0.12f, 0.08f, 0.06f), materials.DarkMetal);
            CreateCube("RearExhaustRight", root.transform, new Vector3(0.2f * widthScale, 0.47f, -0.82f * lengthScale), Vector3.zero, new Vector3(0.12f, 0.08f, 0.06f), materials.DarkMetal);

            float[] wheelZ = { -0.76f * lengthScale, -0.26f * lengthScale, 0.28f * lengthScale, 0.78f * lengthScale };
            for (int i = 0; i < wheelZ.Length; i++)
            {
                CreateCylinderX("LeftWheel_" + i, root.transform, new Vector3(-0.61f * widthScale, 0.2f, wheelZ[i]), Vector3.zero, new Vector3(0.16f, 0.1f, 0.16f), materials.DarkMetal);
                CreateCylinderX("RightWheel_" + i, root.transform, new Vector3(0.61f * widthScale, 0.2f, wheelZ[i]), Vector3.zero, new Vector3(0.16f, 0.1f, 0.16f), materials.DarkMetal);
                CreateCylinderX("LeftWheelHub_" + i, root.transform, new Vector3(-0.68f * widthScale, 0.2f, wheelZ[i]), Vector3.zero, new Vector3(0.07f, 0.04f, 0.07f), materials.Orange);
                CreateCylinderX("RightWheelHub_" + i, root.transform, new Vector3(0.68f * widthScale, 0.2f, wheelZ[i]), Vector3.zero, new Vector3(0.07f, 0.04f, 0.07f), materials.Orange);
            }

            CreateCube("LeftTrackGuard", root.transform, new Vector3(-0.64f * widthScale, 0.28f, 0.04f), Vector3.zero, new Vector3(0.12f, 0.1f, 1.5f * lengthScale), materials.Olive);
            CreateCube("RightTrackGuard", root.transform, new Vector3(0.64f * widthScale, 0.28f, 0.04f), Vector3.zero, new Vector3(0.12f, 0.1f, 1.5f * lengthScale), materials.Olive);

            Transform fan = CreateChild("FrontIntakeFan_Spin", root.transform).transform;
            fan.localPosition = new Vector3(0f, 0.42f, 0.92f * lengthScale);
            CreateCylinderZ("FrontIntakeRim", fan, Vector3.zero, Vector3.zero, new Vector3(0.32f, 0.08f, 0.32f), materials.DarkMetal);
            CreateCylinderZ("FrontIntakeCore", fan, new Vector3(0f, 0f, 0.02f), Vector3.zero, new Vector3(0.18f, 0.04f, 0.18f), materials.Orange);
            CreateCube("FanBladeA", fan, Vector3.zero, Vector3.zero, new Vector3(0.52f, 0.05f, 0.04f), materials.DarkMetal);
            CreateCube("FanBladeB", fan, Vector3.zero, new Vector3(0f, 0f, 90f), new Vector3(0.52f, 0.05f, 0.04f), materials.DarkMetal);

            CreateCube("FrontTowArmLeft", root.transform, new Vector3(-0.16f * widthScale, 0.16f, 1.18f * lengthScale), new Vector3(0f, 8f, 0f), new Vector3(0.06f, 0.07f, 0.56f), materials.Green);
            CreateCube("FrontTowArmRight", root.transform, new Vector3(0.16f * widthScale, 0.16f, 1.18f * lengthScale), new Vector3(0f, -8f, 0f), new Vector3(0.06f, 0.07f, 0.56f), materials.Green);
            CreateCube("LeftDeployFoot", root.transform, new Vector3(-0.82f * widthScale, 0.12f, 0.44f * lengthScale), new Vector3(0f, 0f, -13f), new Vector3(0.36f, 0.07f, 0.2f), materials.Green);
            CreateCube("RightDeployFoot", root.transform, new Vector3(0.82f * widthScale, 0.12f, 0.44f * lengthScale), new Vector3(0f, 0f, 13f), new Vector3(0.36f, 0.07f, 0.2f), materials.Green);
            CreateCube("LeftAFrameStrut", root.transform, new Vector3(-0.36f * widthScale, 0.58f, 0.52f * lengthScale), new Vector3(-32f, 0f, -8f), new Vector3(0.06f, 0.07f, 0.76f), materials.DarkMetal);
            CreateCube("RightAFrameStrut", root.transform, new Vector3(0.36f * widthScale, 0.58f, 0.52f * lengthScale), new Vector3(-32f, 0f, 8f), new Vector3(0.06f, 0.07f, 0.76f), materials.DarkMetal);

            Transform cradle = CreateChild("MortarCradle_Recoil", root.transform).transform;
            cradle.localPosition = new Vector3(0f, 0.72f, 0.08f * lengthScale);
            cradle.localRotation = Quaternion.Euler(-48f, 0f, 0f);

            float tubeHalfLength = 0.86f * tubeScale;
            float muzzleZ = tubeHalfLength * 2f + 0.12f;
            CreateCylinderZ("MainGreenBarrel", cradle, new Vector3(0f, 0f, tubeHalfLength), Vector3.zero, new Vector3(tubeRadius, tubeHalfLength, tubeRadius), materials.Green);
            CreateCylinderZ("RearBreechCap", cradle, new Vector3(0f, 0f, 0.08f), Vector3.zero, new Vector3(tubeRadius * 1.35f, 0.16f, tubeRadius * 1.35f), materials.DarkMetal);
            CreateCylinderZ("BlackMuzzleSleeve", cradle, new Vector3(0f, 0f, muzzleZ - 0.12f), Vector3.zero, new Vector3(tubeRadius * 1.32f, 0.18f, tubeRadius * 1.32f), materials.DarkMetal);
            CreateCylinderZ("OrangeChargeBandA", cradle, new Vector3(0f, 0f, tubeHalfLength * 0.72f), Vector3.zero, new Vector3(tubeRadius * 1.2f, 0.06f, tubeRadius * 1.2f), materials.Orange);
            CreateCylinderZ("DarkChargeBandB", cradle, new Vector3(0f, 0f, tubeHalfLength * 1.28f), Vector3.zero, new Vector3(tubeRadius * 1.15f, 0.06f, tubeRadius * 1.15f), materials.DarkMetal);
            CreateCube("BarrelLowerRail", cradle, new Vector3(0f, -tubeRadius * 1.1f, tubeHalfLength * 1.05f), Vector3.zero, new Vector3(tubeRadius * 0.45f, 0.04f, tubeHalfLength * 1.3f), materials.DarkMetal);
            CreateCylinderZ("LeftRecoilCylinder", cradle, new Vector3(-0.24f * widthScale, -0.1f, tubeHalfLength * 0.85f), Vector3.zero, new Vector3(0.06f, tubeHalfLength * 0.72f, 0.06f), materials.DarkMetal);
            CreateCylinderZ("RightRecoilCylinder", cradle, new Vector3(0.24f * widthScale, -0.1f, tubeHalfLength * 0.85f), Vector3.zero, new Vector3(0.06f, tubeHalfLength * 0.72f, 0.06f), materials.DarkMetal);
            CreateSphere("LeftTargetLens", root.transform, new Vector3(-0.46f * widthScale, 0.66f, 0.56f * lengthScale), Vector3.one * 0.12f, materials.CyanGlow);
            CreateSphere("RightOrangeCore", root.transform, new Vector3(0.46f * widthScale, 0.54f, 0.5f * lengthScale), Vector3.one * 0.1f, materials.Orange);
            AddCrawlerVariantDetails(root, cradle, materials, widthScale, lengthScale, tubeHalfLength, tubeRadius, variant);

            ParticleSystem fireEffect = CreateMuzzleEffect("CrawlerMortarBlast", cradle, new Vector3(0f, 0f, muzzleZ + 0.18f), new Color(1f, 0.58f, 0.08f), materials.FireParticle);
            ParticleSystem.MainModule main = fireEffect.main;
            main.startLifetime = 0.18f;
            main.startSpeed = 2.6f;
            main.startSize = Mathf.Max(0.34f, tubeRadius * 2.4f);
            ParticleSystem.EmissionModule emission = fireEffect.emission;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)18) });

            Light pulseLight = CreatePulseLight("CrawlerMortarPulse", cradle, new Vector3(0f, 0f, muzzleZ + 0.12f), new Color(1f, 0.55f, 0.08f));
            AddMotion(root, cradle, fan, fireEffect, pulseLight, new Vector3(0f, 0f, -0.3f * tubeScale), new Vector3(-7f, 0f, 0f), Vector3.forward, 420f, cycleDuration, 0.24f, 4.4f, phaseOffset);
            return SaveShowcasePrefab(root, displayName, "reference crawler mortar", id, displayScale);
        }

        private static void AddCrawlerVariantDetails(
            GameObject root,
            Transform cradle,
            Materials materials,
            float widthScale,
            float lengthScale,
            float tubeHalfLength,
            float tubeRadius,
            int variant)
        {
            switch (variant)
            {
                case 0:
                    CreateCube("ScoutAntennaLeft", root.transform, new Vector3(-0.24f * widthScale, 0.9f, -0.56f * lengthScale), new Vector3(0f, 0f, -12f), new Vector3(0.04f, 0.46f, 0.04f), materials.DarkMetal);
                    CreateCube("ScoutAntennaRight", root.transform, new Vector3(0.24f * widthScale, 0.9f, -0.56f * lengthScale), new Vector3(0f, 0f, 12f), new Vector3(0.04f, 0.46f, 0.04f), materials.DarkMetal);
                    CreateCube("SlimNoseSensor", root.transform, new Vector3(0f, 0.38f, 1.18f * lengthScale), Vector3.zero, new Vector3(0.22f, 0.12f, 0.12f), materials.Orange);
                    break;
                case 1:
                    CreateCube("SiegeRearAmmoRack", root.transform, new Vector3(0f, 0.72f, -0.78f * lengthScale), Vector3.zero, new Vector3(0.68f * widthScale, 0.24f, 0.28f), materials.DarkMetal);
                    CreateCube("SiegeLeftHeatSink", root.transform, new Vector3(-0.56f * widthScale, 0.58f, -0.25f * lengthScale), Vector3.zero, new Vector3(0.06f, 0.34f, 0.52f), materials.DarkMetal);
                    CreateCube("SiegeRightHeatSink", root.transform, new Vector3(0.56f * widthScale, 0.58f, -0.25f * lengthScale), Vector3.zero, new Vector3(0.06f, 0.34f, 0.52f), materials.DarkMetal);
                    CreateCylinderZ("SiegeMuzzleMass", cradle, new Vector3(0f, 0f, tubeHalfLength * 2f + 0.24f), Vector3.zero, new Vector3(tubeRadius * 1.55f, 0.12f, tubeRadius * 1.55f), materials.DarkMetal);
                    break;
                case 2:
                    CreateCube("RapidLeftMagazine", root.transform, new Vector3(-0.58f * widthScale, 0.56f, -0.18f * lengthScale), Vector3.zero, new Vector3(0.14f, 0.28f, 0.48f), materials.DarkMetal);
                    CreateCube("RapidRightMagazine", root.transform, new Vector3(0.58f * widthScale, 0.56f, -0.18f * lengthScale), Vector3.zero, new Vector3(0.14f, 0.28f, 0.48f), materials.DarkMetal);
                    CreateCylinderZ("RapidFeedTubeLeft", cradle, new Vector3(-0.16f * widthScale, 0.14f, tubeHalfLength), Vector3.zero, new Vector3(0.045f, tubeHalfLength * 0.85f, 0.045f), materials.Orange);
                    CreateCylinderZ("RapidFeedTubeRight", cradle, new Vector3(0.16f * widthScale, 0.14f, tubeHalfLength), Vector3.zero, new Vector3(0.045f, tubeHalfLength * 0.85f, 0.045f), materials.Orange);
                    break;
                case 3:
                    CreateCube("ArmorLeftSkirtPlate", root.transform, new Vector3(-0.7f * widthScale, 0.36f, 0.1f), new Vector3(0f, 0f, 6f), new Vector3(0.1f, 0.42f, 1.34f * lengthScale), materials.DarkMetal);
                    CreateCube("ArmorRightSkirtPlate", root.transform, new Vector3(0.7f * widthScale, 0.36f, 0.1f), new Vector3(0f, 0f, -6f), new Vector3(0.1f, 0.42f, 1.34f * lengthScale), materials.DarkMetal);
                    CreateCube("ArmorNoseWedge", root.transform, new Vector3(0f, 0.28f, 1.16f * lengthScale), new Vector3(-10f, 0f, 0f), new Vector3(0.58f * widthScale, 0.16f, 0.26f), materials.Olive);
                    break;
                case 4:
                    CreateCube("HeavyRearStabilizer", root.transform, new Vector3(0f, 0.16f, -1.08f * lengthScale), Vector3.zero, new Vector3(0.88f * widthScale, 0.08f, 0.32f), materials.DarkMetal);
                    CreateCube("HeavyLeftOutrigger", root.transform, new Vector3(-0.86f * widthScale, 0.12f, -0.5f * lengthScale), new Vector3(0f, 0f, -10f), new Vector3(0.42f, 0.08f, 0.2f), materials.Green);
                    CreateCube("HeavyRightOutrigger", root.transform, new Vector3(0.86f * widthScale, 0.12f, -0.5f * lengthScale), new Vector3(0f, 0f, 10f), new Vector3(0.42f, 0.08f, 0.2f), materials.Green);
                    CreateCylinderZ("HeavyBoreSleeve", cradle, new Vector3(0f, 0f, tubeHalfLength * 2f + 0.2f), Vector3.zero, new Vector3(tubeRadius * 1.7f, 0.18f, tubeRadius * 1.7f), materials.DarkMetal);
                    break;
            }
        }

        private static ShowcaseEntry CreateBasicCannon(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Turret_BasicCannon");
            Transform baseRoot = CreateChild("RoundBase", root.transform).transform;
            Transform headRoot = CreateChild("AimHead", root.transform).transform;
            headRoot.localPosition = new Vector3(0f, 0.42f, 0f);

            CreateNestedPrefab(BaseCircleSmall, "CircleBase", baseRoot, Vector3.zero, Quaternion.identity, Vector3.one, materials.Blue);
            CreateNestedPrefab(HeadCannonSmall, "CannonHead", headRoot, new Vector3(0f, 0.25f, 0.08f), Quaternion.identity, Vector3.one, materials.Blue);
            CreateCylinderZ("CannonCollar", headRoot, new Vector3(0f, 0.36f, 0.38f), Vector3.zero, new Vector3(0.34f, 0.22f, 0.34f), materials.DarkMetal);

            Transform recoil = CreateChild("AnimatedMuzzle_Recoil", headRoot).transform;
            recoil.localPosition = new Vector3(0f, 0.36f, 0.52f);
            CreateCylinderZ("SlidingBarrel", recoil, new Vector3(0f, 0f, 0.38f), Vector3.zero, new Vector3(0.14f, 0.58f, 0.14f), materials.DarkMetal);
            CreateCylinderZ("OrangeHeatBand", recoil, new Vector3(0f, 0f, 0.88f), Vector3.zero, new Vector3(0.2f, 0.12f, 0.2f), materials.Orange);
            CreateSphere("MuzzleCore", recoil, new Vector3(0f, 0f, 1.1f), Vector3.one * 0.18f, materials.CyanGlow);
            CreateCube("AmmoPack_Left", headRoot, new Vector3(-0.42f, 0.25f, 0.24f), Vector3.zero, new Vector3(0.18f, 0.32f, 0.3f), materials.DarkMetal);
            CreateCube("AmmoPack_Right", headRoot, new Vector3(0.42f, 0.25f, 0.24f), Vector3.zero, new Vector3(0.18f, 0.32f, 0.3f), materials.DarkMetal);

            ParticleSystem fireEffect = CreateMuzzleEffect("CannonMuzzleFlash", recoil, new Vector3(0f, 0f, 1.26f), Color.cyan, materials.FireParticle);
            Light light = CreatePulseLight("CannonPulseLight", recoil, new Vector3(0f, 0f, 1.2f), new Color(0.35f, 0.8f, 1f));
            AddMotion(root, recoil, null, fireEffect, light, new Vector3(0f, 0f, -0.26f), Vector3.zero, Vector3.forward, 0f, 1.2f, 0.16f, 2.8f, 0f);

            return SaveShowcasePrefab(root, "Basic Cannon", "balanced cannon / short recoil", "turret_basic_cannon");
        }

        private static ShowcaseEntry CreateRapidLaser(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Turret_RapidLaser");
            Transform headRoot = CreateChild("AimHead", root.transform).transform;
            headRoot.localPosition = new Vector3(0f, 0.42f, 0f);

            CreateNestedPrefab(BaseCircleThree, "ThreePointBase", root.transform, Vector3.zero, Quaternion.identity, Vector3.one, materials.Orange);
            CreateNestedPrefab(HeadLaserSmall, "LaserHead", headRoot, new Vector3(0f, 0.26f, 0.05f), Quaternion.identity, Vector3.one, materials.Orange);

            Transform recoil = CreateChild("TwinLaserEmitter_Recoil", headRoot).transform;
            recoil.localPosition = new Vector3(0f, 0.35f, 0.5f);
            CreateCylinderZ("LaserBarrel_Left", recoil, new Vector3(-0.18f, 0f, 0.42f), Vector3.zero, new Vector3(0.09f, 0.55f, 0.09f), materials.CyanGlow);
            CreateCylinderZ("LaserBarrel_Right", recoil, new Vector3(0.18f, 0f, 0.42f), Vector3.zero, new Vector3(0.09f, 0.55f, 0.09f), materials.CyanGlow);
            CreateSphere("Emitter_Left", recoil, new Vector3(-0.18f, 0f, 1.02f), Vector3.one * 0.13f, materials.CyanGlow);
            CreateSphere("Emitter_Right", recoil, new Vector3(0.18f, 0f, 1.02f), Vector3.one * 0.13f, materials.CyanGlow);

            Transform capacitor = CreateChild("AnimatedCapacitorRotor", headRoot).transform;
            capacitor.localPosition = new Vector3(0f, 0.58f, 0.1f);
            CreateCube("CapacitorArm_A", capacitor, new Vector3(0.35f, 0f, 0f), Vector3.zero, new Vector3(0.42f, 0.08f, 0.08f), materials.CyanGlow);
            CreateCube("CapacitorArm_B", capacitor, new Vector3(-0.35f, 0f, 0f), Vector3.zero, new Vector3(0.42f, 0.08f, 0.08f), materials.CyanGlow);
            CreateCube("CapacitorArm_C", capacitor, new Vector3(0f, 0f, 0.35f), new Vector3(0f, 90f, 0f), new Vector3(0.42f, 0.08f, 0.08f), materials.CyanGlow);
            CreateCube("CapacitorArm_D", capacitor, new Vector3(0f, 0f, -0.35f), new Vector3(0f, 90f, 0f), new Vector3(0.42f, 0.08f, 0.08f), materials.CyanGlow);

            ParticleSystem fireEffect = CreateMuzzleEffect("RapidLaserPulse", recoil, new Vector3(0f, 0f, 1.18f), new Color(0.1f, 1f, 1f), materials.FireParticle);
            Light light = CreatePulseLight("RapidPulseLight", recoil, new Vector3(0f, 0f, 1.06f), new Color(0.1f, 0.9f, 1f));
            AddMotion(root, recoil, capacitor, fireEffect, light, new Vector3(0f, 0f, -0.09f), Vector3.zero, Vector3.up, 360f, 0.48f, 0.08f, 2.2f, 0.1f);

            return SaveShowcasePrefab(root, "Rapid Laser", "fast twin emitters / spinning capacitor", "turret_rapid_laser");
        }

        private static ShowcaseEntry CreateLongRangeSniper(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Turret_LongRangeSniper");
            Transform headRoot = CreateChild("AimHead", root.transform).transform;
            headRoot.localPosition = new Vector3(0f, 0.48f, 0f);

            CreateNestedPrefab(BaseSquareSmall, "SquareStabilizerBase", root.transform, Vector3.zero, Quaternion.identity, Vector3.one, materials.Purple);
            CreateNestedPrefab(HeadSniperSmall, "SniperHead", headRoot, new Vector3(0f, 0.28f, 0.03f), Quaternion.identity, Vector3.one, materials.Purple);
            CreateCube("RangeComputer", headRoot, new Vector3(0f, 0.58f, 0.02f), Vector3.zero, new Vector3(0.46f, 0.16f, 0.34f), materials.DarkMetal);
            CreateSphere("OpticLens", headRoot, new Vector3(0f, 0.58f, 0.28f), Vector3.one * 0.16f, materials.CyanGlow);

            Transform recoil = CreateChild("LongBarrel_Recoil", headRoot).transform;
            recoil.localPosition = new Vector3(0f, 0.36f, 0.45f);
            CreateCylinderZ("LongSniperBarrel", recoil, new Vector3(0f, 0f, 0.72f), Vector3.zero, new Vector3(0.1f, 0.92f, 0.1f), materials.DarkMetal);
            CreateCylinderZ("MuzzleBrake", recoil, new Vector3(0f, 0f, 1.55f), Vector3.zero, new Vector3(0.18f, 0.14f, 0.18f), materials.Purple);
            CreateCube("MuzzleBrake_SlotA", recoil, new Vector3(0.16f, 0f, 1.55f), Vector3.zero, new Vector3(0.08f, 0.22f, 0.16f), materials.CyanGlow);
            CreateCube("MuzzleBrake_SlotB", recoil, new Vector3(-0.16f, 0f, 1.55f), Vector3.zero, new Vector3(0.08f, 0.22f, 0.16f), materials.CyanGlow);

            ParticleSystem fireEffect = CreateMuzzleEffect("SniperMuzzleFlash", recoil, new Vector3(0f, 0f, 1.75f), new Color(0.65f, 0.3f, 1f), materials.FireParticle);
            Light light = CreatePulseLight("SniperPulseLight", recoil, new Vector3(0f, 0f, 1.66f), new Color(0.65f, 0.28f, 1f));
            AddMotion(root, recoil, null, fireEffect, light, new Vector3(0f, 0f, -0.36f), new Vector3(-2f, 0f, 0f), Vector3.forward, 0f, 2.1f, 0.3f, 3.5f, 0.24f);

            return SaveShowcasePrefab(root, "Long Range Sniper", "long barrel / heavy recoil", "turret_long_range_sniper");
        }

        private static ShowcaseEntry CreateBasicMortar(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Mortar_Basic");
            Transform tubeRoot = CreateChild("MortarTube_Recoil", root.transform).transform;
            tubeRoot.localPosition = new Vector3(0f, 0.72f, 0.05f);
            tubeRoot.localRotation = Quaternion.Euler(-48f, 0f, 0f);

            CreateNestedPrefab(BaseSquareSmall, "MortarSquareBase", root.transform, Vector3.zero, Quaternion.identity, Vector3.one, materials.Green);
            CreateNestedPrefab(HeadArtillerySmall, "ArtilleryBreech", tubeRoot, Vector3.zero, Quaternion.identity, Vector3.one, materials.Green);
            CreateCylinderZ("ArcTube", tubeRoot, new Vector3(0f, 0f, 0.68f), Vector3.zero, new Vector3(0.22f, 0.58f, 0.22f), materials.DarkMetal);
            CreateCylinderZ("TubeMouth", tubeRoot, new Vector3(0f, 0f, 1.18f), Vector3.zero, new Vector3(0.28f, 0.12f, 0.28f), materials.Green);
            CreateCube("LeftSupportLeg", root.transform, new Vector3(-0.55f, 0.28f, -0.26f), new Vector3(0f, 0f, -18f), new Vector3(0.12f, 0.54f, 0.12f), materials.DarkMetal);
            CreateCube("RightSupportLeg", root.transform, new Vector3(0.55f, 0.28f, -0.26f), new Vector3(0f, 0f, 18f), new Vector3(0.12f, 0.54f, 0.12f), materials.DarkMetal);

            ParticleSystem fireEffect = CreateMuzzleEffect("MortarMuzzleBlast", tubeRoot, new Vector3(0f, 0f, 1.38f), new Color(0.35f, 1f, 0.55f), materials.FireParticle);
            Light light = CreatePulseLight("MortarPulseLight", tubeRoot, new Vector3(0f, 0f, 1.24f), new Color(0.35f, 1f, 0.55f));
            AddMotion(root, tubeRoot, null, fireEffect, light, new Vector3(0f, 0f, -0.18f), new Vector3(-5f, 0f, 0f), Vector3.forward, 0f, 1.8f, 0.28f, 2.8f, 0.34f);

            return SaveShowcasePrefab(root, "Basic Mortar", "high arc tube / backward kick", "mortar_basic_arc");
        }

        private static ShowcaseEntry CreateRapidMortar(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Mortar_Rapid");
            Transform rack = CreateChild("MissileRack_Recoil", root.transform).transform;
            rack.localPosition = new Vector3(0f, 0.78f, 0.06f);
            rack.localRotation = Quaternion.Euler(-38f, 0f, 0f);

            CreateNestedPrefab(BaseStarSmall, "StarReloadBase", root.transform, Vector3.zero, Quaternion.identity, Vector3.one, materials.Orange);
            CreateNestedPrefab(HeadMissileSmall, "MissileControlHead", rack, Vector3.zero, Quaternion.identity, Vector3.one, materials.Orange);
            CreateCylinderZ("Tube_Left", rack, new Vector3(-0.22f, 0f, 0.72f), Vector3.zero, new Vector3(0.12f, 0.54f, 0.12f), materials.DarkMetal);
            CreateCylinderZ("Tube_Center", rack, new Vector3(0f, 0.02f, 0.8f), Vector3.zero, new Vector3(0.12f, 0.6f, 0.12f), materials.DarkMetal);
            CreateCylinderZ("Tube_Right", rack, new Vector3(0.22f, 0f, 0.72f), Vector3.zero, new Vector3(0.12f, 0.54f, 0.12f), materials.DarkMetal);
            CreateCube("ReloadBox_Left", root.transform, new Vector3(-0.48f, 0.38f, -0.22f), Vector3.zero, new Vector3(0.24f, 0.26f, 0.42f), materials.DarkMetal);
            CreateCube("ReloadBox_Right", root.transform, new Vector3(0.48f, 0.38f, -0.22f), Vector3.zero, new Vector3(0.24f, 0.26f, 0.42f), materials.DarkMetal);

            ParticleSystem fireEffect = CreateMuzzleEffect("RapidMortarBlast", rack, new Vector3(0f, 0f, 1.42f), new Color(1f, 0.55f, 0.2f), materials.FireParticle);
            Light light = CreatePulseLight("RapidMortarPulseLight", rack, new Vector3(0f, 0f, 1.26f), new Color(1f, 0.55f, 0.2f));
            AddMotion(root, rack, null, fireEffect, light, new Vector3(0f, 0f, -0.12f), new Vector3(-3f, 0f, 0f), Vector3.forward, 0f, 0.9f, 0.16f, 2.2f, 0.05f);

            return SaveShowcasePrefab(root, "Rapid Mortar", "salvo rack / quick kick", "mortar_rapid_salvo");
        }

        private static ShowcaseEntry CreateHeavyMortar(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_Mortar_Heavy");
            Transform sled = CreateChild("HeavyRecoilSled", root.transform).transform;
            sled.localPosition = new Vector3(0f, 0.72f, 0f);
            Transform tubeRoot = CreateChild("HeavyTube", sled).transform;
            tubeRoot.localRotation = Quaternion.Euler(-52f, 0f, 0f);

            CreateNestedPrefab(BaseSquareBig, "HeavySquareBase", root.transform, Vector3.zero, Quaternion.identity, Vector3.one * 0.62f, materials.Red);
            CreateNestedPrefab(HeadArtilleryBig, "HeavyArtilleryBreech", tubeRoot, Vector3.zero, Quaternion.identity, Vector3.one * 0.46f, materials.Red);
            CreateCylinderZ("HeavyArcTube", tubeRoot, new Vector3(0f, 0f, 0.86f), Vector3.zero, new Vector3(0.3f, 0.78f, 0.3f), materials.DarkMetal);
            CreateCylinderZ("HeavyTubeMouth", tubeRoot, new Vector3(0f, 0f, 1.52f), Vector3.zero, new Vector3(0.42f, 0.16f, 0.42f), materials.Red);
            CreateCube("Hydraulic_Left", sled, new Vector3(-0.58f, -0.14f, -0.28f), new Vector3(0f, 0f, -18f), new Vector3(0.12f, 0.62f, 0.12f), materials.DarkMetal);
            CreateCube("Hydraulic_Right", sled, new Vector3(0.58f, -0.14f, -0.28f), new Vector3(0f, 0f, 18f), new Vector3(0.12f, 0.62f, 0.12f), materials.DarkMetal);

            ParticleSystem fireEffect = CreateMuzzleEffect("HeavyMortarBlast", tubeRoot, new Vector3(0f, 0f, 1.78f), new Color(1f, 0.25f, 0.15f), materials.FireParticle);
            Light light = CreatePulseLight("HeavyMortarPulseLight", tubeRoot, new Vector3(0f, 0f, 1.6f), new Color(1f, 0.25f, 0.15f));
            AddMotion(root, sled, null, fireEffect, light, new Vector3(0f, 0f, -0.32f), new Vector3(-7f, 0f, 0f), Vector3.forward, 0f, 2.6f, 0.42f, 4.2f, 0.18f);

            return SaveShowcasePrefab(root, "Heavy Mortar", "big tube / sled recoil", "mortar_heavy_sled", 0.72f);
        }

        private static ShowcaseEntry CreateSawTrap(Materials materials)
        {
            GameObject root = CreateRoot("Showcase_SawTrap");
            CreateCube("LowTrapPlate", root.transform, new Vector3(0f, 0.1f, 0f), Vector3.zero, new Vector3(1.7f, 0.2f, 1.2f), materials.Yellow);
            CreateCube("DarkInset", root.transform, new Vector3(0f, 0.23f, 0f), Vector3.zero, new Vector3(1.25f, 0.08f, 0.82f), materials.DarkMetal);
            CreateCube("HazardStripe_Left", root.transform, new Vector3(-0.7f, 0.29f, 0f), new Vector3(0f, 35f, 0f), new Vector3(0.12f, 0.05f, 1.08f), materials.Red);
            CreateCube("HazardStripe_Right", root.transform, new Vector3(0.7f, 0.29f, 0f), new Vector3(0f, -35f, 0f), new Vector3(0.12f, 0.05f, 1.08f), materials.Red);

            Transform spinRoot = CreateChild("AnimatedSawBladeSpin", root.transform).transform;
            spinRoot.localPosition = new Vector3(0f, 0.52f, 0f);
            GameObject blade = CreateNestedPrefab(SawBlade, "CenterSawBlade", spinRoot, Vector3.zero, Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.38f, materials.DarkMetal);
            CreateSphere("BladeHub", spinRoot, Vector3.zero, Vector3.one * 0.18f, materials.CyanGlow);
            CreateCube("FrontGuard", root.transform, new Vector3(0f, 0.46f, 0.62f), Vector3.zero, new Vector3(1.3f, 0.16f, 0.08f), materials.DarkMetal);
            CreateCube("BackGuard", root.transform, new Vector3(0f, 0.46f, -0.62f), Vector3.zero, new Vector3(1.3f, 0.16f, 0.08f), materials.DarkMetal);

            AddMotion(root, null, spinRoot, null, null, Vector3.zero, Vector3.zero, Vector3.up, 1080f, 0.7f, 0.1f, 0f, 0f);
            StripColliders(blade);

            return SaveShowcasePrefab(root, "Saw Trap", "spinning floor blade / close defense", "saw_trap_spin", 0.95f);
        }

        private static void CreateScene(List<ShowcaseEntry> entries, Materials materials)
        {
            GameObject root = new GameObject("TurretVisualShowcaseRoot");
            Transform displayRoot = CreateChild("DisplayPrefabs", root.transform).transform;
            Transform labelRoot = CreateChild("Labels", root.transform).transform;

            CreateFloor(materials, ShowcaseOrigin);
            CreateLighting(ShowcaseOrigin);
            Camera camera = CreateCamera(ShowcaseOrigin);

            Vector3[] positions =
            {
                new Vector3(-4.8f, 0f, 1.35f),
                new Vector3(-1.6f, 0f, 1.35f),
                new Vector3(1.6f, 0f, 1.35f),
                new Vector3(4.8f, 0f, 1.35f),
                new Vector3(-3.2f, 0f, -1.55f),
                new Vector3(0f, 0f, -1.55f),
                new Vector3(3.2f, 0f, -1.55f)
            };

            for (int i = 0; i < entries.Count; i++)
            {
                ShowcaseEntry entry = entries[i];
                Vector3 position = ShowcaseOrigin + positions[i];
                GameObject prefab = LoadRequiredAsset<GameObject>(entry.PrefabPath);
                Transform slot = CreateChild(entry.Id + "_Slot", displayRoot).transform;
                slot.position = position;
                slot.localScale = Vector3.one * entry.DisplayScale;

                GameObject instance = InstantiatePrefabLocal(prefab, entry.Id + "_Model", slot, Vector3.zero, Quaternion.identity, Vector3.one);
                ConfigureDisplayInstance(instance);
                ConfigureShowcaseMotion(entry, instance, materials);

                CreatePedestal(entry.Id + "_Pedestal", position, materials.Pedestal);
                CreateNamePlate(entry.DisplayName, labelRoot, position + new Vector3(0f, 0.28f, 1.45f), camera, materials.DarkMetal);
            }
        }

        private static void CreateMortarLineupScene(List<ShowcaseEntry> entries, Materials materials)
        {
            GameObject root = new GameObject("MortarAssetLineupRoot");
            Transform displayRoot = CreateChild("DisplayPrefabs", root.transform).transform;
            Transform labelRoot = CreateChild("Labels", root.transform).transform;

            CreateFloor(materials, MortarLineupOrigin);
            CreateLighting(MortarLineupOrigin);
            Camera camera = CreateMortarLineupCamera(MortarLineupOrigin);

            Vector3[] positions =
            {
                new Vector3(-4.8f, 0f, 0f),
                new Vector3(-2.4f, 0f, 0f),
                new Vector3(0f, 0f, 0f),
                new Vector3(2.4f, 0f, 0f),
                new Vector3(4.8f, 0f, 0f)
            };

            for (int i = 0; i < entries.Count; i++)
            {
                ShowcaseEntry entry = entries[i];
                Vector3 position = MortarLineupOrigin + positions[i];
                GameObject prefab = LoadRequiredAsset<GameObject>(entry.PrefabPath);
                Transform slot = CreateChild(entry.Id + "_Slot", displayRoot).transform;
                slot.position = position;
                slot.localScale = Vector3.one * entry.DisplayScale;

                GameObject instance = InstantiatePrefabLocal(prefab, entry.Id + "_Model", slot, Vector3.zero, Quaternion.identity, Vector3.one);
                ConfigureDisplayInstance(instance);
                CreatePedestal(entry.Id + "_Pedestal", position, materials.Pedestal);
                CreateNamePlate(entry.DisplayName, labelRoot, position + new Vector3(0f, 0.28f, 1.45f), camera, materials.DarkMetal);
            }
        }


        private static void ConfigureDisplayInstance(GameObject instance)
        {
            Canvas[] canvases = instance.GetComponentsInChildren<Canvas>(true);
            for (int i = 0; i < canvases.Length; i++)
            {
                Object.DestroyImmediate(canvases[i].gameObject);
            }

            ShowcaseFiringMotion[] motions = instance.GetComponentsInChildren<ShowcaseFiringMotion>(true);
            for (int i = 0; i < motions.Length; i++)
            {
                SerializedObject serializedObject = new SerializedObject(motions[i]);
                SerializedProperty previewProperty = serializedObject.FindProperty("previewInEditMode");
                if (previewProperty != null)
                {
                    previewProperty.boolValue = true;
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        private static void ConfigureShowcaseMotion(ShowcaseEntry entry, GameObject instance, Materials materials)
        {
            switch (entry.MotionKind)
            {
                case ShowcaseMotionKind.Recoil:
                    AddMotion(instance, instance.transform, null, FindFirstParticleSystem(instance), null, new Vector3(0f, 0f, -0.08f), Vector3.zero, Vector3.up, 0f, 1.25f, 0.12f, 0f, 0f);
                    break;
                case ShowcaseMotionKind.Mortar:
                    AddMotion(instance, instance.transform, null, FindFirstParticleSystem(instance), null, new Vector3(0f, 0f, -0.12f), new Vector3(-3f, 0f, 0f), Vector3.up, 0f, 1.7f, 0.22f, 0f, 0.12f);
                    break;
                case ShowcaseMotionKind.Saw:
                    AddMotion(instance, null, FindRequiredChildContaining(instance.transform, "sawblade"), null, null, Vector3.zero, Vector3.zero, Vector3.forward, 720f, 0.8f, 0.1f, 0f, 0f);
                    break;
            }
        }

        private static ParticleSystem FindFirstParticleSystem(GameObject root)
        {
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            return particleSystems.Length > 0 ? particleSystems[0] : null;
        }

        private static Transform FindRequiredChildContaining(Transform root, string namePart)
        {
            string loweredNamePart = namePart.ToLowerInvariant();
            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                if (children[i].name.ToLowerInvariant().Contains(loweredNamePart))
                {
                    return children[i];
                }
            }

            throw new System.InvalidOperationException("Required child not found under " + root.name + ": " + namePart);
        }

        private static void CreateFloor(Materials materials, Vector3 origin)
        {
            CreateCube("ShowcaseFloor", null, origin + new Vector3(0f, -0.08f, 0f), Vector3.zero, new Vector3(13f, 0.16f, 7.5f), materials.Floor);
            CreateCube("DisplayRunway", null, origin + new Vector3(0f, 0.01f, 0f), Vector3.zero, new Vector3(12f, 0.04f, 6.6f), materials.RowBand);
        }

        private static void CreatePedestal(string name, Vector3 position, Material material)
        {
            CreateCube(name, null, position + new Vector3(0f, 0.09f, 0f), Vector3.zero, new Vector3(2.35f, 0.18f, 1.9f), material);
        }

        private static void CreateNamePlate(string text, Transform parent, Vector3 position, Camera camera, Material material)
        {
            CreateCube("Plate_" + text.Replace(" ", "_"), parent, position + new Vector3(0f, -0.12f, 0.05f), Vector3.zero, new Vector3(2.15f, 0.08f, 0.4f), material);
            CreateLabel(text, parent, position + new Vector3(0f, 0.12f, -0.08f), camera);
        }

        private static void CreateLabel(string text, Transform parent, Vector3 position, Camera camera)
        {
            GameObject labelObject = new GameObject("Label_" + text.Split('\n')[0].Replace(" ", "_"));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = camera.transform.rotation;

            TextMesh textMesh = labelObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = 0.22f;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = Color.white;
        }

        private static void CreateLighting(Vector3 origin)
        {
            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject fillObject = new GameObject("Soft Fill Light");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 3.4f;
            fill.range = 22f;
            fill.color = new Color(0.45f, 0.62f, 1f);
            fillObject.transform.position = origin + new Vector3(-3f, 5f, 5f);
        }

        private static Camera CreateCamera(Vector3 origin)
        {
            GameObject cameraObject = new GameObject("Turret Showcase Camera");
            cameraObject.tag = "MainCamera";
            Vector3 cameraPosition = origin + new Vector3(2.2f, 5.5f, 7.2f);
            Vector3 lookTarget = origin + new Vector3(0f, 0.85f, 0f);
            cameraObject.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.4f;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.23f, 0.24f, 0.24f);
            return camera;
        }

        private static Camera CreateMortarLineupCamera(Vector3 origin)
        {
            GameObject cameraObject = new GameObject("Mortar Lineup Camera");
            Vector3 cameraPosition = origin + new Vector3(1.8f, 4.7f, 7.4f);
            Vector3 lookTarget = origin + new Vector3(0f, 0.85f, 0f);
            cameraObject.transform.SetPositionAndRotation(
                cameraPosition,
                Quaternion.LookRotation(lookTarget - cameraPosition, Vector3.up));

            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 3.5f;
            camera.fieldOfView = 45f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 100f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.23f, 0.24f, 0.24f);
            return camera;
        }

        private static Materials CreateMaterials()
        {
            Shader litShader = FindRequiredShader("Universal Render Pipeline/Lit");
            Shader particleShader = FindRequiredShader("Universal Render Pipeline/Particles/Unlit");

            return new Materials
            {
                DarkMetal = CreateMaterial("Showcase_DarkMetal.mat", litShader, new Color(0.12f, 0.14f, 0.16f), Color.black),
                Blue = CreateMaterial("Showcase_BasicBlue.mat", litShader, new Color(0.08f, 0.32f, 0.78f), new Color(0.02f, 0.08f, 0.24f)),
                Orange = CreateMaterial("Showcase_RapidOrange.mat", litShader, new Color(0.96f, 0.5f, 0.08f), new Color(0.34f, 0.12f, 0.02f)),
                Purple = CreateMaterial("Showcase_LongRangePurple.mat", litShader, new Color(0.5f, 0.22f, 0.95f), new Color(0.13f, 0.04f, 0.28f)),
                Green = CreateMaterial("Showcase_MortarGreen.mat", litShader, new Color(0.2f, 0.46f, 0.25f), new Color(0.01f, 0.045f, 0.018f)),
                Olive = CreateMaterial("Showcase_MortarOlive.mat", litShader, new Color(0.34f, 0.39f, 0.24f), new Color(0.025f, 0.035f, 0.02f)),
                Red = CreateMaterial("Showcase_HeavyRed.mat", litShader, new Color(0.88f, 0.16f, 0.12f), new Color(0.22f, 0.03f, 0.02f)),
                Yellow = CreateMaterial("Showcase_HazardYellow.mat", litShader, new Color(1f, 0.78f, 0.12f), new Color(0.2f, 0.12f, 0.01f)),
                CyanGlow = CreateMaterial("Showcase_CyanGlow.mat", litShader, new Color(0.08f, 0.85f, 1f), new Color(0.05f, 0.75f, 1.2f)),
                Floor = CreateMaterial("Showcase_Floor.mat", litShader, new Color(0.2f, 0.23f, 0.24f), Color.black),
                RowBand = CreateMaterial("Showcase_RowBand.mat", litShader, new Color(0.1f, 0.16f, 0.18f), Color.black),
                Pedestal = CreateMaterial("Showcase_Pedestal.mat", litShader, new Color(0.28f, 0.3f, 0.32f), Color.black),
                Target = CreateMaterial("Showcase_TargetRed.mat", litShader, new Color(0.8f, 0.1f, 0.08f), new Color(0.3f, 0.02f, 0.01f)),
                FireParticle = CreateMaterial("Showcase_FireParticle.mat", particleShader, Color.white, new Color(0.5f, 0.85f, 1f))
            };
        }

        private static Material CreateMaterial(string fileName, Shader shader, Color baseColor, Color emissionColor)
        {
            string path = MaterialFolder + "/" + fileName;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.color = baseColor;
            material.SetColor("_BaseColor", baseColor);
            material.SetColor("_EmissionColor", emissionColor);
            if (emissionColor.maxColorComponent > 0f)
            {
                material.EnableKeyword("_EMISSION");
            }
            else
            {
                material.DisableKeyword("_EMISSION");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Shader FindRequiredShader(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Debug.LogError("Required shader missing: " + shaderName);
                throw new System.InvalidOperationException("Required shader missing: " + shaderName);
            }

            return shader;
        }

        private static ShowcaseEntry SaveShowcasePrefab(GameObject root, string displayName, string note, string id, float displayScale = 1.08f)
        {
            string path = PrefabFolder + "/" + root.name + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            Object.DestroyImmediate(root);

            if (!success || prefab == null)
            {
                throw new System.InvalidOperationException("Failed to save prefab: " + path);
            }

            return new ShowcaseEntry(id, displayName, note, path, displayScale, ShowcaseMotionKind.None);
        }

        private static GameObject CreateRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            return root;
        }

        private static GameObject CreateNestedPrefab(
            string path,
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            GameObject prefab = LoadRequiredAsset<GameObject>(path);
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("Failed to instantiate prefab: " + path);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            ApplyMaterial(instance, material);
            StripColliders(instance);
            return instance;
        }

        private static GameObject InstantiatePrefab(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 position,
            Quaternion rotation,
            Vector3 scale)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("Failed to instantiate prefab: " + prefab.name);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            instance.transform.localScale = scale;
            return instance;
        }

        private static GameObject InstantiatePrefabLocal(
            GameObject prefab,
            string name,
            Transform parent,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException("Failed to instantiate prefab: " + prefab.name);
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = localRotation;
            instance.transform.localScale = localScale;
            return instance;
        }

        private static GameObject CreateChild(string name, Transform parent)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static GameObject CreateCube(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Material material)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localRotation = Quaternion.Euler(localEulerAngles);
            cube.transform.localScale = localScale;
            ApplyMaterial(cube, material);
            StripColliders(cube);
            return cube;
        }

        private static GameObject CreateCylinderY(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = Quaternion.Euler(localEulerAngles);
            cylinder.transform.localScale = localScale;
            ApplyMaterial(cylinder, material);
            StripColliders(cylinder);
            return cylinder;
        }

        private static GameObject CreateCylinderX(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = Quaternion.Euler(localEulerAngles) * Quaternion.Euler(0f, 0f, 90f);
            cylinder.transform.localScale = localScale;
            ApplyMaterial(cylinder, material);
            StripColliders(cylinder);
            return cylinder;
        }

        private static GameObject CreateCylinderZ(
            string name,
            Transform parent,
            Vector3 localPosition,
            Vector3 localEulerAngles,
            Vector3 localScale,
            Material material)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = Quaternion.Euler(localEulerAngles) * Quaternion.Euler(90f, 0f, 0f);
            cylinder.transform.localScale = localScale;
            ApplyMaterial(cylinder, material);
            StripColliders(cylinder);
            return cylinder;
        }

        private static GameObject CreateSphere(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = name;
            sphere.transform.SetParent(parent, false);
            sphere.transform.localPosition = localPosition;
            sphere.transform.localRotation = Quaternion.identity;
            sphere.transform.localScale = localScale;
            ApplyMaterial(sphere, material);
            StripColliders(sphere);
            return sphere;
        }

        private static ParticleSystem CreateMuzzleEffect(string name, Transform parent, Vector3 localPosition, Color color, Material material)
        {
            GameObject effectObject = new GameObject(name);
            effectObject.transform.SetParent(parent, false);
            effectObject.transform.localPosition = localPosition;
            effectObject.transform.localRotation = Quaternion.identity;

            ParticleSystem particleSystem = effectObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.duration = 0.12f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.12f;
            main.startSpeed = 1.8f;
            main.startSize = 0.22f;
            main.startColor = color;
            main.maxParticles = 24;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)10) });

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 13f;
            shape.radius = 0.04f;

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            return particleSystem;
        }

        private static Light CreatePulseLight(string name, Transform parent, Vector3 localPosition, Color color)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;

            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 2.4f;
            light.intensity = 0f;
            light.enabled = false;
            return light;
        }

        private static void AddMotion(
            GameObject root,
            Transform recoilTarget,
            Transform spinTarget,
            ParticleSystem fireEffect,
            Light pulseLight,
            Vector3 recoilOffset,
            Vector3 recoilEulerOffset,
            Vector3 spinAxis,
            float spinSpeed,
            float cycleDuration,
            float recoilDuration,
            float pulseIntensity,
            float phaseOffset)
        {
            ShowcaseFiringMotion motion = root.AddComponent<ShowcaseFiringMotion>();
            motion.Configure(
                recoilTarget,
                spinTarget,
                fireEffect,
                pulseLight,
                recoilOffset,
                recoilEulerOffset,
                spinAxis,
                spinSpeed,
                cycleDuration,
                recoilDuration,
                pulseIntensity,
                phaseOffset,
                true);
        }

        private static void ApplyMaterial(GameObject root, Material material)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is ParticleSystemRenderer)
                {
                    continue;
                }

                renderers[i].sharedMaterial = material;
            }
        }

        private static void StripColliders(GameObject root)
        {
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }
        }

        private static void ValidateScene(SceneValidationSpec spec, List<string> failures)
        {
            if (!System.IO.File.Exists(spec.ScenePath))
            {
                failures.Add("Missing scene: " + spec.ScenePath);
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(spec.ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                failures.Add("Scene could not be opened: " + spec.ScenePath);
                return;
            }

            if (FindTransformInScene(scene, spec.RequiredPrimaryObjectName) == null)
            {
                failures.Add(spec.ScenePath + " missing " + spec.RequiredPrimaryObjectName + ".");
            }

            if (FindTransformInScene(scene, spec.RequiredSecondaryObjectName) == null)
            {
                failures.Add(spec.ScenePath + " missing " + spec.RequiredSecondaryObjectName + ".");
            }

            int missingScripts = CountMissingScriptsInScene(scene);
            if (missingScripts > 0)
            {
                failures.Add(spec.ScenePath + " has missing scripts: " + missingScripts + ".");
            }
        }

        private static Transform FindTransformInScene(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform result = FindChildByName(roots[i].transform, objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static Transform FindChildByName(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform result = FindChildByName(root.GetChild(i), objectName);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private static int CountMissingScriptsInScene(Scene scene)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                count += CountMissingScripts(roots[i]);
            }

            return count;
        }

        private static int CountMissingScripts(GameObject root)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root);
            for (int i = 0; i < root.transform.childCount; i++)
            {
                count += CountMissingScripts(root.transform.GetChild(i).gameObject);
            }

            return count;
        }

        private static void CloseExistingTargetScene()
        {
            CloseExistingTargetScene(ScenePath);
        }

        private static void CloseExistingTargetScene(string scenePath)
        {
            Scene existingScene = SceneManager.GetSceneByPath(scenePath);
            if (!existingScene.IsValid() || !existingScene.isLoaded)
            {
                return;
            }

            if (existingScene.isDirty)
            {
                if (!EditorSceneManager.SaveScene(existingScene, scenePath))
                {
                    throw new System.InvalidOperationException("Failed to save dirty target scene before rebuild: " + scenePath);
                }
            }

            if (SceneManager.sceneCount <= 1)
            {
                return;
            }

            if (!EditorSceneManager.CloseScene(existingScene, true))
            {
                throw new System.InvalidOperationException("Failed to close target scene: " + scenePath);
            }
        }

        private static bool HasDirtyLoadedScenes()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && scene.isDirty)
                {
                    return true;
                }
            }

            return false;
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("Required asset missing: " + path);
                throw new System.InvalidOperationException("Required asset missing: " + path);
            }

            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace("\\", "/");
            string name = System.IO.Path.GetFileName(path);
            if (string.IsNullOrWhiteSpace(parent))
            {
                throw new System.InvalidOperationException("Invalid asset folder path: " + path);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private enum ShowcaseMotionKind
        {
            None,
            Recoil,
            Mortar,
            Saw
        }

        private readonly struct SceneValidationSpec
        {
            public SceneValidationSpec(string scenePath, string requiredPrimaryObjectName, string requiredSecondaryObjectName)
            {
                ScenePath = scenePath;
                RequiredPrimaryObjectName = requiredPrimaryObjectName;
                RequiredSecondaryObjectName = requiredSecondaryObjectName;
            }

            public string ScenePath { get; }
            public string RequiredPrimaryObjectName { get; }
            public string RequiredSecondaryObjectName { get; }
        }

        private readonly struct ShowcaseEntry
        {
            public ShowcaseEntry(
                string id,
                string displayName,
                string note,
                string prefabPath,
                float displayScale,
                ShowcaseMotionKind motionKind)
            {
                Id = id;
                DisplayName = displayName;
                Note = note;
                PrefabPath = prefabPath;
                DisplayScale = displayScale;
                MotionKind = motionKind;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Note { get; }
            public string PrefabPath { get; }
            public float DisplayScale { get; }
            public ShowcaseMotionKind MotionKind { get; }
        }

        private sealed class Materials
        {
            public Material DarkMetal;
            public Material Blue;
            public Material Orange;
            public Material Purple;
            public Material Green;
            public Material Olive;
            public Material Red;
            public Material Yellow;
            public Material CyanGlow;
            public Material Floor;
            public Material RowBand;
            public Material Pedestal;
            public Material Target;
            public Material FireParticle;
        }
    }
}
