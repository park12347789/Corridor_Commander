using System.Collections.Generic;
using CorridorCommander.Audio;
using CorridorCommander.PlayerControl;
using CorridorCommander.PlayerUI;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TutorialMapSceneBuilder
    {
        private const string TutorialScenePath = "Assets/hansol/01_Scenes/TutorialMap.unity";
        private const string StartMenuScenePath = "Assets/hansol/01_Scenes/StartMenu.unity";
        private const string MainScenePath = "Assets/hansol/01_Scenes/MainScene.unity";
        private const string LayerLabSpriteRoot = "Assets/90_ThirdParty/Layer Lab/GUI Pro-SuperCasual/ResourcesData/Sprites";
        private const string LayerLabPanelFramePath = LayerLabSpriteRoot + "/Components/Icon_Chest/Frame/ListFrame00_03~04_Bg.png";
        private const string LayerLabDialogueBubblePath = LayerLabSpriteRoot + "/Components/Popup/Popup_Chat_Single.png";
        private const string LayerLabDialogueFramePath = LayerLabSpriteRoot + "/Components/Popup/Popup02~09_Topber_White_Bg.png";
        private const string LayerLabDialogueHeaderPath = LayerLabSpriteRoot + "/Components/Popup/Popup02~09_Topber_White_BgTop.png";
        private const string LayerLabPrimaryButtonPath = LayerLabSpriteRoot + "/Components/Button/Button01_s_Blue.png";
        private const string LayerLabSecondaryButtonPath = LayerLabSpriteRoot + "/Components/Button/Button01_s_DarkGray.png";
        private const string LayerLabChapterButtonPath = LayerLabSpriteRoot + "/Components/Button/Button01_l_Blue.png";
        private const string LayerLabPortraitFramePath = LayerLabSpriteRoot + "/Components/Icon_Chest/Frame/ProfileFrame01_m~s_Bg.png";
        private const string LayerLabDialogueTabPath = LayerLabSpriteRoot + "/Components/Popup/Popup_Chat_White_TabFocus.png";
        private const string LayerLabDialogueDividerPath = LayerLabSpriteRoot + "/Components/Label/Title_Line03_Divider.png";
        private const string PortraitGuardPath = "Assets/hansol/05_Art/UI/Portraits/Portrait_Guard.png";
        private const string PortraitOperatorPath = "Assets/hansol/05_Art/UI/Portraits/Portrait_Operator.png";
        private const string PortraitPlayerPath = "Assets/hansol/05_Art/UI/Portraits/Portrait_Player.png";
        private const string MouseCursorIconPath = "Assets/hansol/04_Art/UI/Icons/Generated/icon_mouse_cursor_sf_casual.png";
        private const string TrainingRootName = "TutorialTrainingRoot";
        private const string TutorialControllerName = "TutorialDialogueStepController";
        private const string PlacementName = "Tutorial_Objective_PlacementPoint";
        private const string ShopName = "Tutorial_SupportTruck_Shop";

        [MenuItem("Corridor Commander/Tutorial/Validate Tutorial Map")]
        public static void Validate()
        {
            ValidateInternal(askBeforeOpeningScene: true);
        }

        public static void ValidateForAutomation()
        {
            ValidateInternal(askBeforeOpeningScene: false);
        }

        [MenuItem("Corridor Commander/Tutorial/Validate Tutorial Map No Prompt")]
        public static void ValidateNoPrompt()
        {
            ValidateForAutomation();
        }

        private static void ValidateInternal(bool askBeforeOpeningScene)
        {
            if (askBeforeOpeningScene && !Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(TutorialScenePath, OpenSceneMode.Single);
            List<string> failures = new List<string>();

            if (!scene.IsValid())
            {
                failures.Add("Tutorial scene could not be opened: " + TutorialScenePath);
            }

            RequireTransform(TrainingRootName, failures);
            RequireTransform("Tutorial_PlayerSetup", failures);
            RequireTransform("BgmSystem", failures);
            RequireTransform("Tutorial_PlayerWeaponAudio", failures);
            RequireTransform(PlacementName, failures);
            RequireTransform(ShopName, failures);
            RequireTransform("Tutorial_Final_Goal_YELLOW", failures);
            RequireTransform("MainCanvas", failures);
            RequireTransform("money", failures);
            RequireTransform("HudIcon_Heart", failures);
            RequireTransform("HudIcon_Stamina", failures);
            RequireTransform("WeaponHudPanel", failures);
            RequireTransform("WeaponNameText", failures);
            RequireTransform("TutorialChapterSelectPresenter", failures);
            RequireTransform("TutorialChapterCompletionPresenter", failures);
            RequireTransform("TutorialChapterCompletionPanel", failures);
            RequireTransform("TutorialChapterFlowController", failures);
            RequireTransform("Tutorial_GuideArrow", failures);
            RequireTransform("TutorialDialoguePanel", failures);
            RequireTransform("TutorialDialogueHistoryPanel", failures);
            RequireTransform("TutorialDialogueResumeRoot", failures);
            RequireTransform("TutorialDialogueMouseCursorIcon", failures);
            RequireTransform("SpeakerPortraitRoot", failures);
            RequireTransform("PortraitAvatarImage", failures);
            RequireTransform(TutorialControllerName, failures);
            RequireTransform("TutorialStageInitializer", failures);
            RequireTransform("Tutorial_Operation_Console", failures);
            RequireTransform("OperationDialogueAnchor", failures);
            RequireTransform("Scifi_M_Visuals", failures);
            RequireTransform("Scifi_Floor_Tile_00_00", failures);
            RequireTransform("Scifi_Wall_North_Upper_00", failures);
            RequireTransform("Training_Ceiling", failures);
            RequireTransform("Scifi_Ceiling_Roof_00_00", failures);
            RequireTransform("Scifi_Ceiling_LightPanel_01_01", failures);
            RequireTransform("Tutorial_Ceiling_Light_00", failures);
            RequireTransform("Tutorial_Key_DirectionalLight", failures);
            RequireTransform("Operation_Console_Scifi_Table", failures);
            RequireTransform("Tutorial_EnemyRoute", failures);
            RequireTransform("Tutorial_Enemy_SpawnPoint_RED", failures);
            RequireTransform("Tutorial_TreasureChest_Basic", failures);

            if (!BuildSettingsContains(StartMenuScenePath))
            {
                failures.Add("Build Settings is missing StartMenu scene.");
            }

            if (!BuildSettingsContains(MainScenePath))
            {
                failures.Add("Build Settings is missing MainScene.");
            }

            if (!BuildSettingsContains(TutorialScenePath))
            {
                failures.Add("Build Settings is missing TutorialMap scene.");
            }

            RequireComponent<Camera>("Main Camera", failures);
            RequireComponent<Light>("Tutorial lighting", failures);
            RequireComponent<GameManager>("GameManager", failures);
            RequireComponent<BgmPlayer>("BgmPlayer", failures);
            RequireComponent<UiInputCoordinator>("UiInputCoordinator", failures);
            RequireComponent<StageInitializer>("StageInitializer", failures);
            RequireComponent<StageLayoutRoot>("StageLayoutRoot", failures);
            RequireComponent<EnemyRoute>("EnemyRoute", failures);
            RequireComponent<EnemySpawner>("EnemySpawner", failures);
            RequireComponent<TreasureChest>("TreasureChest", failures);
            RequireComponent<PlacementBuildMenuPresenter>("PlacementBuildMenuPresenter", failures);
            RequireComponent<InstalledObjectActionPresenter>("InstalledObjectActionPresenter", failures);
            RequireComponent<SupportTruckShopPresenter>("SupportTruckShopPresenter", failures);
            RequireComponent<PlayerRuntimeHudBinding>("PlayerRuntimeHudBinding", failures);
            RequireComponent<PlayerWeaponHudPresenter>("PlayerWeaponHudPresenter", failures);
            RequireComponent<TutorialDialoguePresenter>("TutorialDialoguePresenter", failures);
            RequireComponent<TutorialDialogueStepController>("TutorialDialogueStepController", failures);
            RequireComponent<TutorialChapterSelectPresenter>("TutorialChapterSelectPresenter", failures);
            RequireComponent<TutorialChapterCompletionPresenter>("TutorialChapterCompletionPresenter", failures);
            RequireComponent<TutorialChapterFlowController>("TutorialChapterFlowController", failures);
            RequireComponent<TutorialFloatingGuideArrow>("TutorialFloatingGuideArrow", failures);
            RequireComponent<PlacementPoint>("PlacementPoint", failures);
            RequireComponent<SupportTruckShop>("SupportTruckShop", failures);
            RequireComponent<SupportTruckShopInteraction>("SupportTruckShopInteraction", failures);
            RequireComponent<PlayerCurrencyWallet>("PlayerCurrencyWallet", failures);
            RequireComponent<PlayerWeaponAudioController>("PlayerWeaponAudioController", failures);
            RequireTutorialNavMesh(failures);
            RequireLayerLabSprite(LayerLabPanelFramePath, failures);
            RequireLayerLabSprite(LayerLabDialogueBubblePath, failures);
            RequireLayerLabSprite(LayerLabDialogueFramePath, failures);
            RequireLayerLabSprite(LayerLabDialogueHeaderPath, failures);
            RequireLayerLabSprite(LayerLabPrimaryButtonPath, failures);
            RequireLayerLabSprite(LayerLabSecondaryButtonPath, failures);
            RequireLayerLabSprite(LayerLabChapterButtonPath, failures);
            RequireLayerLabSprite(LayerLabPortraitFramePath, failures);
            RequireLayerLabSprite(LayerLabDialogueTabPath, failures);
            RequireLayerLabSprite(LayerLabDialogueDividerPath, failures);
            RequireSprite(PortraitGuardPath, failures);
            RequireSprite(PortraitOperatorPath, failures);
            RequireSprite(PortraitPlayerPath, failures);
            RequireSprite(MouseCursorIconPath, failures);
            RequireTutorialStageConfiguration(failures);
            RequireTutorialPlacementConfiguration(failures);
            RequireTutorialShopConfiguration(failures);

            int missingScriptCount = CountMissingScripts();
            if (missingScriptCount > 0)
            {
                failures.Add("Missing script components found: " + missingScriptCount + ".");
            }

            int missingPrefabCount = CountMissingPrefabAssets();
            if (missingPrefabCount > 0)
            {
                failures.Add("Missing prefab asset instances found: " + missingPrefabCount + ".");
            }

            if (failures.Count > 0)
            {
                throw new System.InvalidOperationException("Tutorial map validation failed: " + string.Join(" | ", failures));
            }

            Debug.Log("Tutorial map validation passed. Scene=" + TutorialScenePath);
        }

        private static void RequireComponent<T>(string label, List<string> failures) where T : UnityEngine.Object
        {
            T component = UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
            if (component == null)
            {
                failures.Add(label + " is missing.");
            }
        }

        private static void RequireTransform(string name, List<string> failures)
        {
            if (FindTransformByName(name) == null)
            {
                failures.Add("Missing GameObject: " + name);
            }
        }

        private static void RequireLayerLabSprite(string path, List<string> failures)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                failures.Add("Missing Layer Lab sprite: " + path);
            }
        }

        private static void RequireSprite(string path, List<string> failures)
        {
            if (AssetDatabase.LoadAssetAtPath<Sprite>(path) == null)
            {
                failures.Add("Missing sprite: " + path);
            }
        }

        private static void RequireTutorialPlacementConfiguration(List<string> failures)
        {
            PlacementPoint placementPoint = UnityEngine.Object.FindFirstObjectByType<PlacementPoint>(FindObjectsInactive.Include);
            if (placementPoint == null)
            {
                return;
            }

            int validDefinitionCount = 0;
            IReadOnlyList<BuildableDefinitionSO> definitions = placementPoint.BuildableDefinitions;
            if (definitions != null)
            {
                for (int i = 0; i < definitions.Count; i++)
                {
                    if (definitions[i] != null && definitions[i].Prefab != null)
                    {
                        validDefinitionCount++;
                    }
                }
            }

            if (validDefinitionCount < 3)
            {
                failures.Add("Tutorial placement point needs three buildable definitions with prefabs.");
            }
        }

        private static void RequireTutorialStageConfiguration(List<string> failures)
        {
            StageInitializer initializer = UnityEngine.Object.FindFirstObjectByType<StageInitializer>(FindObjectsInactive.Include);
            if (initializer == null)
            {
                return;
            }

            if (initializer.StageDefinition == null)
            {
                failures.Add("Tutorial StageInitializer.stageDefinition is missing.");
            }

            if (initializer.Runtime == null)
            {
                failures.Add("Tutorial StageInitializer.runtime is missing.");
            }

            StageLayoutRoot layoutRoot = initializer.LayoutRoot;
            if (layoutRoot == null)
            {
                failures.Add("Tutorial StageInitializer.layoutRoot is missing.");
                return;
            }

            layoutRoot.CollectChildren();
            if (layoutRoot.MainTarget == null)
            {
                failures.Add("Tutorial StageLayoutRoot.mainTarget is missing.");
            }

            if (layoutRoot.PlacementPoints == null || layoutRoot.PlacementPoints.Length == 0)
            {
                failures.Add("Tutorial StageLayoutRoot has no placement points.");
            }

            if (layoutRoot.SupportTruckShops == null || layoutRoot.SupportTruckShops.Length == 0)
            {
                failures.Add("Tutorial StageLayoutRoot has no support truck shops.");
            }

            if (layoutRoot.EnemySpawners == null || layoutRoot.EnemySpawners.Length == 0)
            {
                failures.Add("Tutorial StageLayoutRoot has no enemy spawners.");
            }

            if (layoutRoot.EnemyRoutes == null || layoutRoot.EnemyRoutes.Length == 0)
            {
                failures.Add("Tutorial StageLayoutRoot has no enemy routes.");
            }

            if (layoutRoot.TreasureChests == null || layoutRoot.TreasureChests.Length == 0)
            {
                failures.Add("Tutorial StageLayoutRoot has no treasure chests.");
            }
        }

        private static void RequireTutorialShopConfiguration(List<string> failures)
        {
            SupportTruckShop shop = UnityEngine.Object.FindFirstObjectByType<SupportTruckShop>(FindObjectsInactive.Include);
            if (shop != null && shop.Catalog == null)
            {
                failures.Add("Tutorial support truck shop catalog is missing.");
            }
        }

        private static void RequireTutorialNavMesh(List<string> failures)
        {
            NavMeshSurface surface = UnityEngine.Object.FindFirstObjectByType<NavMeshSurface>(FindObjectsInactive.Include);
            if (surface == null)
            {
                failures.Add("Tutorial NavMeshSurface is missing.");
                return;
            }

            if (surface.navMeshData == null)
            {
                failures.Add("Tutorial NavMeshSurface has no baked NavMeshData.");
            }
        }

        private static Transform FindTransformByName(string name)
        {
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i].name == name)
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static int CountMissingScripts()
        {
            int missingCount = 0;
            GameObject[] roots = SceneManager.GetActiveScene().GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                missingCount += CountMissingScripts(roots[i]);
            }

            return missingCount;
        }

        private static int CountMissingScripts(GameObject gameObject)
        {
            int missingCount = 0;
            Component[] components = gameObject.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] == null)
                {
                    missingCount++;
                }
            }

            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                missingCount += CountMissingScripts(gameObject.transform.GetChild(i).gameObject);
            }

            return missingCount;
        }

        private static int CountMissingPrefabAssets()
        {
            int missingCount = 0;
            Transform[] transforms = UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (PrefabUtility.GetPrefabInstanceStatus(transforms[i].gameObject) == PrefabInstanceStatus.MissingAsset)
                {
                    missingCount++;
                }
            }

            return missingCount;
        }

        private static bool BuildSettingsContains(string path)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            for (int i = 0; i < scenes.Length; i++)
            {
                if (scenes[i].enabled && scenes[i].path == path)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
