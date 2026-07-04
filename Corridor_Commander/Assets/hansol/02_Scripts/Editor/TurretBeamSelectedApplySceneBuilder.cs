using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretBeamSelectedApplySceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_beam_selected_apply.unity";

        private const string GoldArrowFitPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_GoldArrow_OriginalFit_Safe.prefab";
        private const string YellowBulletFitPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_YellowBullet_OriginalFit_Safe.prefab";

        private static readonly BeamEntry[] BeamEntries =
        {
            new BeamEntry(
                "03 Gold Arrow Original",
                "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_GoldArrow_Safe.prefab",
                GoldArrowFitPath),
            new BeamEntry(
                "06 Yellow Bullet Original",
                "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_YellowBullet_Safe.prefab",
                YellowBulletFitPath)
        };

        private static readonly TurretSetup[] TurretSetups =
        {
            new TurretSetup(
                "Basic",
                "Assets/hansol/03_Prefabs/Turret_Basic.prefab",
                GoldArrowFitPath,
                "Gold Arrow / medium shot",
                -4.9f,
                5.2f,
                0.72f,
                5.2f,
                0.62f,
                0.75f,
                false,
                34f),
            new TurretSetup(
                "Rapid",
                "Assets/hansol/03_Prefabs/Turret_Rapid.prefab",
                YellowBulletFitPath,
                "Yellow Bullet / fast burst",
                0f,
                3.2f,
                1.05f,
                3.2f,
                0.55f,
                0.1f,
                true,
                8f),
            new TurretSetup(
                "LongRange",
                "Assets/hansol/03_Prefabs/Turret_LongRange.prefab",
                GoldArrowFitPath,
                "Gold Arrow / long shot",
                4.9f,
                6.4f,
                0.64f,
                6.4f,
                0.9f,
                1.15f,
                false,
                34f)
        };

        [MenuItem("Corridor Commander/Art/Build Selected Turret Beam Apply Scene")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildInternal(HasDirtyLoadedScenes() ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        public static void BuildForAutomation()
        {
            BuildInternal(ShouldUseAdditiveMode() ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        private static void BuildInternal(NewSceneMode mode)
        {
            EnsureFolder("Assets/hansol/01_Scenes/test1");
            EnsureOriginalFitBeamPrefabs();
            ValidateAssets();
            CloseExistingTargetScene();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "turret_beam_selected_apply";
            EditorSceneManager.SetActiveScene(scene);

            Materials materials = CreateMaterials();
            GameObject root = new GameObject("TurretBeamSelectedApplyRoot");
            CreateEnvironment(root.transform, materials);
            Camera sceneCamera = CreateCameraAndLight();
            CreateAppliedTurrets(root.transform, materials, sceneCamera);

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save selected turret beam apply scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Selected turret beam apply scene built: " + ScenePath);
        }

        private static void CreateAppliedTurrets(Transform root, Materials materials, Camera sceneCamera)
        {
            for (int index = 0; index < TurretSetups.Length; index++)
            {
                TurretSetup setup = TurretSetups[index];
                Vector3 turretPosition = new Vector3(setup.X, 0f, -1.6f);
                Vector3 targetPosition = turretPosition + new Vector3(0f, 0.62f, setup.TargetDistance);
                CreateAppliedTurret(root, materials, setup, turretPosition, targetPosition, index * 0.18f, sceneCamera);
            }
        }

        private static void CreateAppliedTurret(
            Transform root,
            Materials materials,
            TurretSetup setup,
            Vector3 turretPosition,
            Vector3 targetPosition,
            float phaseOffset,
            Camera sceneCamera)
        {
            GameObject turretPrefab = LoadRequiredAsset<GameObject>(setup.PrefabPath);
            GameObject beamPrefab = LoadRequiredAsset<GameObject>(setup.BeamFitPath);
            GameObject turret = (GameObject)PrefabUtility.InstantiatePrefab(turretPrefab, root);
            turret.name = "Copy_" + setup.Name + "_" + System.IO.Path.GetFileNameWithoutExtension(setup.BeamFitPath);
            turret.transform.localPosition = turretPosition;
            turret.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            RemoveSceneOnlyInteractions(turret);

            ProjectileFirePoint firePoint = turret.GetComponentInChildren<ProjectileFirePoint>(true);
            if (firePoint == null)
            {
                throw new System.InvalidOperationException("Turret prefab has no ProjectileFirePoint: " + setup.PrefabPath);
            }

            TurretTargetingController targeting = turret.GetComponentInChildren<TurretTargetingController>(true);
            if (targeting != null)
            {
                targeting.SetUpdateLoopEnabled(false);
            }

            ConfigureFirePoint(
                firePoint,
                beamPrefab,
                setup.BeamScale,
                setup.ReferenceLength,
                setup.BeamLifetime,
                setup.BeamMovesToHit,
                setup.BeamTravelSpeed);
            ConfigureSceneCameraReferences(turret, sceneCamera);
            GameObject target = CreateTarget(root, materials, setup.Name, targetPosition);
            GifVerifyDamageTarget damageTarget = target.AddComponent<GifVerifyDamageTarget>();
            ConfigureAutoFire(turret, firePoint, target.transform, damageTarget, setup.FireInterval, phaseOffset);

            CreateBlock(
                turret.name + "_Pad",
                root,
                turretPosition + new Vector3(0f, -0.08f, setup.TargetDistance * 0.5f),
                new Vector3(3.6f, 0.08f, setup.TargetDistance + 1.35f),
                materials.Pad);
            CreateLabel(setup.Name + "\n" + setup.Label, root, turretPosition + new Vector3(0f, 0.08f, -1.45f), 0.16f, Color.white);
        }

        private static GameObject CreateTarget(Transform root, Materials materials, string key, Vector3 position)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "Target_" + key;
            target.transform.SetParent(root, false);
            target.transform.localPosition = position;
            target.transform.localScale = new Vector3(0.48f, 0.58f, 0.48f);
            target.GetComponent<Renderer>().sharedMaterial = materials.Target;
            return target;
        }

        private static void ConfigureFirePoint(
            ProjectileFirePoint firePoint,
            GameObject beamPrefab,
            float beamScale,
            float referenceLength,
            float beamLifetime,
            bool beamMovesToHit,
            float beamTravelSpeed)
        {
            SerializedObject serializedObject = new SerializedObject(firePoint);
            SetObject(serializedObject, "beamVfxPrefab", beamPrefab);
            SetObject(serializedObject, "beamEffectPrefab", null);
            SetFloat(serializedObject, "beamEffectLifetime", beamLifetime);
            SetFloat(serializedObject, "beamEffectWidth", 0.08f);
            SetFloat(serializedObject, "beamVfxReferenceLength", referenceLength);
            SetFloat(serializedObject, "beamVfxScale", beamScale);
            SetBool(serializedObject, "beamVfxUsesXAxis", false);
            SetBool(serializedObject, "beamVfxMovesToHit", beamMovesToHit);
            SetFloat(serializedObject, "beamVfxTravelSpeed", beamTravelSpeed);
            SetFloat(serializedObject, "beamVfxMovingSegmentLength", 1.8f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAutoFire(
            GameObject turret,
            ProjectileFirePoint firePoint,
            Transform targetPoint,
            GifVerifyDamageTarget damageTarget,
            float fireInterval,
            float phaseOffset)
        {
            TurretGifVerifyAutoFire autoFire = turret.AddComponent<TurretGifVerifyAutoFire>();
            SerializedObject serializedObject = new SerializedObject(autoFire);
            SetObject(serializedObject, "firePoint", firePoint);
            SetObject(serializedObject, "targetPoint", targetPoint);
            SetObject(serializedObject, "damageTarget", damageTarget);
            SetFloat(serializedObject, "fireInterval", fireInterval);
            SetFloat(serializedObject, "phaseOffset", phaseOffset);
            SetFloat(serializedObject, "damage", 1f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEnvironment(Transform root, Materials materials)
        {
            CreateBlock("SelectedApplyFloor", root, new Vector3(0f, -0.16f, 1.35f), new Vector3(16.4f, 0.12f, 10.5f), materials.Floor);
            CreateLabel("Turret VFX visual check", root, new Vector3(0f, 0.08f, -4.45f), 0.22f, materials.LabelTint);
        }

        private static Camera CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 9.4f, -10.8f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 8.0f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.05f, 0.055f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(48f, -30f, 0f);

            GameObject fillObject = new GameObject("Soft Fill Light");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 2.8f;
            fill.range = 20f;
            fill.transform.position = new Vector3(0f, 4.6f, 1.4f);

            return camera;
        }

        private static void ConfigureSceneCameraReferences(GameObject root, Camera sceneCamera)
        {
            if (sceneCamera == null)
            {
                return;
            }

            Canvas[] canvases = root.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    canvas.worldCamera = sceneCamera;
                }
            }

            WorldSpaceCameraBillboard[] billboards = root.GetComponentsInChildren<WorldSpaceCameraBillboard>(true);
            foreach (WorldSpaceCameraBillboard billboard in billboards)
            {
                SerializedObject serializedObject = new SerializedObject(billboard);
                SetObject(serializedObject, "targetCamera", sceneCamera);
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void RemoveSceneOnlyInteractions(GameObject root)
        {
            InstalledObjectInteraction[] interactions = root.GetComponentsInChildren<InstalledObjectInteraction>(true);
            foreach (InstalledObjectInteraction interaction in interactions)
            {
                Object.DestroyImmediate(interaction, true);
            }
        }

        private static void CreateBlock(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = localScale;
            block.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static void CreateLabel(string text, Transform parent, Vector3 localPosition, float size, Color color)
        {
            GameObject label = new GameObject("Label_" + text.Replace(" ", "_").Replace("/", "_").Replace("\n", "_"));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 32;
            textMesh.characterSize = size * 0.28f;
            textMesh.color = color;
        }

        private static void ValidateAssets()
        {
            foreach (TurretSetup setup in TurretSetups)
            {
                if (LoadRequiredAsset<GameObject>(setup.PrefabPath) == null)
                {
                    throw new System.InvalidOperationException("Missing turret prefab: " + setup.PrefabPath);
                }

                if (LoadRequiredAsset<GameObject>(setup.BeamFitPath) == null)
                {
                    throw new System.InvalidOperationException("Missing assigned beam prefab: " + setup.BeamFitPath);
                }
            }

            foreach (BeamEntry beam in BeamEntries)
            {
                if (LoadRequiredAsset<GameObject>(beam.SourcePath) == null)
                {
                    throw new System.InvalidOperationException("Missing source beam prefab: " + beam.SourcePath);
                }

                if (LoadRequiredAsset<GameObject>(beam.FitPath) == null)
                {
                    throw new System.InvalidOperationException("Missing original-fit beam prefab: " + beam.FitPath);
                }
            }
        }

        private static void EnsureOriginalFitBeamPrefabs()
        {
            foreach (BeamEntry beam in BeamEntries)
            {
                GameObject source = LoadRequiredAsset<GameObject>(beam.SourcePath);
                if (source == null)
                {
                    continue;
                }

                GameObject wrapper = new GameObject(System.IO.Path.GetFileNameWithoutExtension(beam.FitPath));
                GameObject original = (GameObject)PrefabUtility.InstantiatePrefab(source, wrapper.transform);
                original.name = source.name;
                original.transform.localPosition = Vector3.zero;
                original.transform.localRotation = Quaternion.identity;
                original.transform.localScale = Vector3.one;
                ConfigureOriginalParticles(original);

                PrefabUtility.SaveAsPrefabAsset(wrapper, beam.FitPath);
                Object.DestroyImmediate(wrapper);
            }
        }

        private static void ConfigureOriginalParticles(GameObject root)
        {
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.loop = false;
                main.playOnAwake = true;
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("[TurretBeamSelectedApplySceneBuilder] Missing required asset: " + path);
            }

            return asset;
        }

        private static void CloseExistingTargetScene()
        {
            Scene existingScene = SceneManager.GetSceneByPath(ScenePath);
            if (existingScene.IsValid() && existingScene.isLoaded && SceneManager.sceneCount == 1)
            {
                return;
            }

            if (existingScene.IsValid() && existingScene.isLoaded && !EditorSceneManager.CloseScene(existingScene, true))
            {
                throw new System.InvalidOperationException("Failed to close already open scene: " + ScenePath);
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

        private static bool ShouldUseAdditiveMode()
        {
            Scene targetScene = SceneManager.GetSceneByPath(ScenePath);
            if (targetScene.IsValid() && targetScene.isLoaded && SceneManager.sceneCount == 1)
            {
                return false;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (SceneManager.sceneCount == 1 && activeScene.name == "turret_beam_selected_apply")
            {
                return false;
            }

            return HasDirtyLoadedScenes();
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folder);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, name);
            }
        }

        private static Materials CreateMaterials()
        {
            return new Materials(
                CreateMaterial("SelectedApplyFloorMat", new Color(0.095f, 0.105f, 0.115f)),
                CreateMaterial("SelectedApplyPadMat", new Color(0.155f, 0.16f, 0.18f)),
                CreateMaterial("SelectedApplyTargetMat", new Color(0.42f, 0.45f, 0.48f)),
                new Color(0.74f, 0.88f, 1f));
        }

        private static Material CreateMaterial(string name, Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material = new Material(shader) { name = name };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }
            else
            {
                material.color = color;
            }

            return material;
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetFloat(SerializedObject serializedObject, string propertyName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
            }
        }

        private readonly struct BeamEntry
        {
            public BeamEntry(
                string name,
                string sourcePath,
                string fitPath)
            {
                Name = name;
                SourcePath = sourcePath;
                FitPath = fitPath;
            }

            public string Name { get; }
            public string SourcePath { get; }
            public string FitPath { get; }
        }

        private readonly struct TurretSetup
        {
            public TurretSetup(
                string name,
                string prefabPath,
                string beamFitPath,
                string label,
                float x,
                float targetDistance,
                float beamScale,
                float referenceLength,
                float beamLifetime,
                float fireInterval,
                bool beamMovesToHit,
                float beamTravelSpeed)
            {
                Name = name;
                PrefabPath = prefabPath;
                BeamFitPath = beamFitPath;
                Label = label;
                X = x;
                TargetDistance = targetDistance;
                BeamScale = beamScale;
                ReferenceLength = referenceLength;
                BeamLifetime = beamLifetime;
                FireInterval = fireInterval;
                BeamMovesToHit = beamMovesToHit;
                BeamTravelSpeed = beamTravelSpeed;
            }

            public string Name { get; }
            public string PrefabPath { get; }
            public string BeamFitPath { get; }
            public string Label { get; }
            public float X { get; }
            public float TargetDistance { get; }
            public float BeamScale { get; }
            public float ReferenceLength { get; }
            public float BeamLifetime { get; }
            public float FireInterval { get; }
            public bool BeamMovesToHit { get; }
            public float BeamTravelSpeed { get; }
        }

        private readonly struct Materials
        {
            public Materials(Material floor, Material pad, Material target, Color labelTint)
            {
                Floor = floor;
                Pad = pad;
                Target = target;
                LabelTint = labelTint;
            }

            public Material Floor { get; }
            public Material Pad { get; }
            public Material Target { get; }
            public Color LabelTint { get; }
        }
    }
}
