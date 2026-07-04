using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretVfxSwapValidationSceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_vfx_swap_validation.unity";

        private const string BasicTurretPath = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string RapidTurretPath = "Assets/hansol/03_Prefabs/Turret_Rapid.prefab";
        private const string LongRangeTurretPath = "Assets/hansol/03_Prefabs/Turret_LongRange.prefab";

        private const string MuzzleRiflePath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretMuzzleFlash_Rifle_Safe.prefab";
        private const string MuzzleWeaponRiflePath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Muzzle_Flash_Rifle_Safe.prefab";
        private const string WeaponBeamBlueStarPath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Beam_BlueStar_Safe.prefab";
        private const string WeaponHitBluePath = "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Hit_Blue02_Safe.prefab";

        private static readonly SwapRow[] Rows =
        {
            new SwapRow(
                "Weapon BeamCannon Settings",
                0.5f,
                new SwapSlot("Basic", BasicTurretPath, "Blue Laser", WeaponBeamBlueStarPath, MuzzleRiflePath, WeaponHitBluePath, -5.4f, 4.8f, 1f, 0.16f, 0.75f, 0f, 1f, new Color(0.2f, 0.85f, 1f, 1f)),
                new SwapSlot("Rapid", RapidTurretPath, "Red Sustain Laser", WeaponBeamBlueStarPath, MuzzleWeaponRiflePath, WeaponHitBluePath, 0f, 4.2f, 1.15f, 1.15f, 2.1f, 0.45f, 4f, new Color(1f, 0.22f, 0.18f, 1f)),
                new SwapSlot("LongRange", LongRangeTurretPath, "Green Laser", WeaponBeamBlueStarPath, MuzzleRiflePath, WeaponHitBluePath, 5.4f, 6.2f, 1f, 0.22f, 1.05f, 0f, 1.4f, new Color(0.35f, 1f, 0.22f, 1f)))
        };

        [MenuItem("Corridor Commander/Art/Build Turret VFX Swap Validation Scene")]
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
            ValidateAssets();
            CloseExistingTargetScene();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "turret_vfx_swap_validation";
            EditorSceneManager.SetActiveScene(scene);

            Materials materials = CreateMaterials();
            GameObject root = new GameObject("TurretVfxSwapValidationRoot");
            CreateEnvironment(root.transform, materials);
            CreateCameraAndLight();

            for (int rowIndex = 0; rowIndex < Rows.Length; rowIndex++)
            {
                CreateRow(root.transform, materials, Rows[rowIndex], rowIndex);
            }

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save turret VFX swap validation scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Turret VFX swap validation scene built: " + ScenePath);
        }

        private static void CreateRow(Transform root, Materials materials, SwapRow row, int rowIndex)
        {
            CreateLabel(row.Name, root, new Vector3(-8.3f, 0.08f, row.Z - 1.9f), 0.18f, materials.LabelTint);
            CreateSlot(root, materials, row.Basic, row.Z, rowIndex, 0);
            CreateSlot(root, materials, row.Rapid, row.Z, rowIndex, 1);
            CreateSlot(root, materials, row.LongRange, row.Z, rowIndex, 2);
        }

        private static void CreateSlot(Transform root, Materials materials, SwapSlot slot, float rowZ, int rowIndex, int slotIndex)
        {
            GameObject turretPrefab = LoadRequiredAsset<GameObject>(slot.TurretPath);
            GameObject beamPrefab = LoadRequiredAsset<GameObject>(slot.BeamPath);
            GameObject muzzlePrefab = LoadRequiredAsset<GameObject>(slot.MuzzlePath);
            ParticleSystem impactPrefab = LoadRequiredParticle(slot.ImpactPath);

            Vector3 turretPosition = new Vector3(slot.X, 0f, rowZ);

            GameObject turret = (GameObject)PrefabUtility.InstantiatePrefab(turretPrefab, root);
            turret.name = rowIndex + "_" + slotIndex + "_" + slot.TurretName + "_" + slot.BeamName;
            turret.transform.localPosition = turretPosition;
            turret.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            RemoveSceneOnlyInteractions(turret);

            ProjectileFirePoint firePoint = turret.GetComponentInChildren<ProjectileFirePoint>(true);
            if (firePoint == null)
            {
                throw new System.InvalidOperationException("Turret prefab has no ProjectileFirePoint: " + slot.TurretPath);
            }

            TurretTargetingController targeting = turret.GetComponentInChildren<TurretTargetingController>(true);
            if (targeting != null)
            {
                targeting.SetUpdateLoopEnabled(false);
            }

            Vector3 hitPosition = firePoint.Position + Vector3.forward * slot.TargetDistance;
            ConfigureFirePoint(firePoint, beamPrefab, muzzlePrefab, impactPrefab, slot);

            TargetRig target = CreateTarget(root, materials, slot.TurretName + "_" + slot.BeamName, hitPosition);
            GifVerifyDamageTarget damageTarget = target.Body.AddComponent<GifVerifyDamageTarget>();
            ConfigureAutoFire(turret, firePoint, target.HitPoint, damageTarget, slot, rowIndex * 0.11f + slotIndex * 0.07f);

            CreateBlock(
                turret.name + "_Pad",
                root,
                turretPosition + new Vector3(0f, -0.08f, slot.TargetDistance * 0.5f),
                new Vector3(3.5f, 0.08f, slot.TargetDistance + 1.25f),
                materials.Pad);
            CreateLabel(slot.TurretName + "\n" + slot.BeamName, root, turretPosition + new Vector3(0f, 0.08f, -1.35f), 0.14f, Color.white);
        }

        private static void ConfigureFirePoint(
            ProjectileFirePoint firePoint,
            GameObject beamPrefab,
            GameObject muzzlePrefab,
            ParticleSystem impactPrefab,
            SwapSlot slot)
        {
            SerializedObject serializedObject = new SerializedObject(firePoint);
            SetObject(serializedObject, "beamVfxPrefab", beamPrefab);
            SetObject(serializedObject, "beamEffectPrefab", null);
            SetObject(serializedObject, "fireVfxPrefab", muzzlePrefab);
            SetObject(serializedObject, "impactEffectPrefab", impactPrefab);
            SetFloat(serializedObject, "beamEffectLifetime", slot.BeamLifetime);
            SetFloat(serializedObject, "beamVfxReferenceLength", 2.02f);
            SetFloat(serializedObject, "beamVfxScale", slot.BeamScale);
            SetBool(serializedObject, "beamVfxUsesXAxis", false);
            SetBool(serializedObject, "beamVfxMovesToHit", false);
            SetVector3(serializedObject, "beamVfxRotationOffset", new Vector3(0f, -90f, 0f));
            SetBool(serializedObject, "useBeamVfxTint", true);
            SetColor(serializedObject, "beamVfxTint", slot.BeamTint);
            SetBool(serializedObject, "stretchBeamVfxToHitPoint", true);
            SetEnum(serializedObject, "beamVfxStretchAxis", 2);
            SetString(serializedObject, "beamVfxStretchTransformName", "position");
            SetString(serializedObject, "beamVfxStretchChildNameContains", "line");
            SetFloat(serializedObject, "beamVfxVisualLengthMultiplier", 1f);
            SetFloat(serializedObject, "beamVfxEndPadding", 1.35f);
            SetFloat(serializedObject, "fireVfxScale", 1f);
            SetFloat(serializedObject, "fireVfxLifetime", 0.5f);
            SetFloat(serializedObject, "impactEffectScale", 1f);
            SetFloat(serializedObject, "impactEffectLifetime", 1f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureAutoFire(
            GameObject turret,
            ProjectileFirePoint firePoint,
            Transform targetPoint,
            GifVerifyDamageTarget damageTarget,
            SwapSlot slot,
            float phaseOffset)
        {
            TurretGifVerifyAutoFire autoFire = turret.AddComponent<TurretGifVerifyAutoFire>();
            SerializedObject serializedObject = new SerializedObject(autoFire);
            SetObject(serializedObject, "firePoint", firePoint);
            SetObject(serializedObject, "targetPoint", targetPoint);
            SetObject(serializedObject, "damageTarget", damageTarget);
            SetFloat(serializedObject, "fireInterval", slot.FireInterval);
            SetFloat(serializedObject, "preFireDelay", slot.PreFireDelay);
            SetFloat(serializedObject, "phaseOffset", phaseOffset);
            SetFloat(serializedObject, "damage", slot.Damage);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static TargetRig CreateTarget(Transform root, Materials materials, string key, Vector3 hitPosition)
        {
            GameObject target = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            target.name = "Target_" + key;
            target.transform.SetParent(root, false);
            target.transform.localPosition = hitPosition + Vector3.forward * 0.28f;
            target.transform.localScale = new Vector3(0.48f, 0.58f, 0.48f);
            target.GetComponent<Renderer>().sharedMaterial = materials.Target;

            GameObject hitPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitPoint.name = "HitPoint_" + key;
            hitPoint.transform.SetParent(root, false);
            hitPoint.transform.localPosition = hitPosition;
            hitPoint.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
            hitPoint.GetComponent<Renderer>().sharedMaterial = materials.HitPoint;
            Collider hitPointCollider = hitPoint.GetComponent<Collider>();
            if (hitPointCollider != null)
            {
                Object.DestroyImmediate(hitPointCollider);
            }

            return new TargetRig(target, hitPoint.transform);
        }

        private static void CreateEnvironment(Transform root, Materials materials)
        {
            CreateBlock("SwapValidationFloor", root, new Vector3(0f, -0.16f, 4.5f), new Vector3(18.5f, 0.12f, 30f), materials.Floor);
            CreateLabel("Turret VFX swap validation", root, new Vector3(0f, 0.08f, -13.4f), 0.25f, materials.LabelTint);
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 16f, -15f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.orthographic = true;
            camera.orthographicSize = 16f;
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
            fill.range = 25f;
            fill.transform.position = new Vector3(0f, 5f, 2.5f);
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
            foreach (SwapRow row in Rows)
            {
                ValidateSlot(row.Basic);
                ValidateSlot(row.Rapid);
                ValidateSlot(row.LongRange);
            }
        }

        private static void ValidateSlot(SwapSlot slot)
        {
            if (LoadRequiredAsset<GameObject>(slot.TurretPath) == null)
            {
                throw new System.InvalidOperationException("Missing turret prefab: " + slot.TurretPath);
            }

            if (LoadRequiredAsset<GameObject>(slot.BeamPath) == null)
            {
                throw new System.InvalidOperationException("Missing beam prefab: " + slot.BeamPath);
            }

            if (LoadRequiredAsset<GameObject>(slot.MuzzlePath) == null)
            {
                throw new System.InvalidOperationException("Missing muzzle prefab: " + slot.MuzzlePath);
            }

            if (LoadRequiredParticle(slot.ImpactPath) == null)
            {
                throw new System.InvalidOperationException("Missing impact particle prefab: " + slot.ImpactPath);
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("[TurretVfxSwapValidationSceneBuilder] Missing required asset: " + path);
            }

            return asset;
        }

        private static ParticleSystem LoadRequiredParticle(string path)
        {
            GameObject asset = LoadRequiredAsset<GameObject>(path);
            return asset != null ? asset.GetComponentInChildren<ParticleSystem>(true) : null;
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
            if (SceneManager.sceneCount == 1 && activeScene.name == "turret_vfx_swap_validation")
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
                CreateMaterial("SwapValidationFloorMat", new Color(0.095f, 0.105f, 0.115f)),
                CreateMaterial("SwapValidationPadMat", new Color(0.155f, 0.16f, 0.18f)),
                CreateMaterial("SwapValidationTargetMat", new Color(0.42f, 0.45f, 0.48f)),
                CreateMaterial("SwapValidationHitPointMat", new Color(0.08f, 0.95f, 1f)),
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

        private static void SetVector3(SerializedObject serializedObject, string propertyName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static void SetColor(SerializedObject serializedObject, string propertyName, Color value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.colorValue = value;
            }
        }

        private static void SetEnum(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = value;
            }
        }

        private static void SetString(SerializedObject serializedObject, string propertyName, string value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.stringValue = value;
            }
        }

        private readonly struct SwapRow
        {
            public SwapRow(string name, float z, SwapSlot basic, SwapSlot rapid, SwapSlot longRange)
            {
                Name = name;
                Z = z;
                Basic = basic;
                Rapid = rapid;
                LongRange = longRange;
            }

            public string Name { get; }
            public float Z { get; }
            public SwapSlot Basic { get; }
            public SwapSlot Rapid { get; }
            public SwapSlot LongRange { get; }
        }

        private readonly struct SwapSlot
        {
            public SwapSlot(
                string turretName,
                string turretPath,
                string beamName,
                string beamPath,
                string muzzlePath,
                string impactPath,
                float x,
                float targetDistance,
                float beamScale,
                float beamLifetime,
                float fireInterval,
                float preFireDelay,
                float damage,
                Color beamTint)
            {
                TurretName = turretName;
                TurretPath = turretPath;
                BeamName = beamName;
                BeamPath = beamPath;
                MuzzlePath = muzzlePath;
                ImpactPath = impactPath;
                X = x;
                TargetDistance = targetDistance;
                BeamScale = beamScale;
                BeamLifetime = beamLifetime;
                FireInterval = fireInterval;
                PreFireDelay = preFireDelay;
                Damage = damage;
                BeamTint = beamTint;
            }

            public string TurretName { get; }
            public string TurretPath { get; }
            public string BeamName { get; }
            public string BeamPath { get; }
            public string MuzzlePath { get; }
            public string ImpactPath { get; }
            public float X { get; }
            public float TargetDistance { get; }
            public float BeamScale { get; }
            public float BeamLifetime { get; }
            public float FireInterval { get; }
            public float PreFireDelay { get; }
            public float Damage { get; }
            public Color BeamTint { get; }
        }

        private readonly struct Materials
        {
            public Materials(Material floor, Material pad, Material target, Material hitPoint, Color labelTint)
            {
                Floor = floor;
                Pad = pad;
                Target = target;
                HitPoint = hitPoint;
                LabelTint = labelTint;
            }

            public Material Floor { get; }
            public Material Pad { get; }
            public Material Target { get; }
            public Material HitPoint { get; }
            public Color LabelTint { get; }
        }

        private readonly struct TargetRig
        {
            public TargetRig(GameObject body, Transform hitPoint)
            {
                Body = body;
                HitPoint = hitPoint;
            }

            public GameObject Body { get; }
            public Transform HitPoint { get; }
        }
    }
}
