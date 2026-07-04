using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretVfxLineupSceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_vfx_lineup.unity";

        private static readonly VfxEntry[] FireEntries =
        {
            new VfxEntry("Turret muzzle rifle", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretMuzzleFlash_Rifle_Safe.prefab", VfxSlotKind.Fire),
            new VfxEntry("Weapon muzzle rifle", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Muzzle_Flash_Rifle_Safe.prefab", VfxSlotKind.Fire),
            new VfxEntry("Weapon muzzle handgun", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Muzzle_Flash_Handgun_Safe.prefab", VfxSlotKind.Fire),
            new VfxEntry("Weapon muzzle shotgun", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Muzzle_Flash_Shotgun_Safe.prefab", VfxSlotKind.Fire)
        };

        private static readonly VfxEntry[] ImpactEntries =
        {
            new VfxEntry("Turret impact orange", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretImpact_OrangeSpark_Safe.prefab", VfxSlotKind.Impact),
            new VfxEntry("Turret impact blue", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretImpact_Blue02_Safe.prefab", VfxSlotKind.Impact),
            new VfxEntry("Weapon hit laser", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Hit_LaserGun_Safe.prefab", VfxSlotKind.Impact),
            new VfxEntry("Weapon hit blue", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Hit_Blue02_Safe.prefab", VfxSlotKind.Impact)
        };

        private static readonly VfxEntry[] BeamEntries =
        {
            new VfxEntry("Beam basic orange white", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Basic_OrangeWhite.prefab", VfxSlotKind.Beam),
            new VfxEntry("Beam rapid blue", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Rapid_ContinuousBlue.prefab", VfxSlotKind.Beam),
            new VfxEntry("Beam long heavy orange", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_LongRange_HeavyOrange.prefab", VfxSlotKind.Beam),
            new VfxEntry("Beam rapid thin amber", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Rapid_ThinAmber.prefab", VfxSlotKind.Beam)
        };

        [MenuItem("Corridor Commander/Art/Build Turret VFX Lineup")]
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
            BuildInternal(HasDirtyLoadedScenes() ? NewSceneMode.Additive : NewSceneMode.Single);
        }

        private static void BuildInternal(NewSceneMode mode)
        {
            EnsureFolder("Assets/hansol/01_Scenes/test1");
            ValidateEntries(FireEntries);
            ValidateEntries(ImpactEntries);
            ValidateEntries(BeamEntries);

            CloseExistingTargetScene();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "turret_vfx_lineup";
            EditorSceneManager.SetActiveScene(scene);

            Materials materials = CreateMaterials();
            GameObject root = new GameObject("TurretVfxLineupRoot");
            CreateEnvironment(root.transform, materials);
            CreateRow(root.transform, "Fire / Muzzle VFX", FireEntries, new Vector3(0f, 0f, -4f), materials);
            CreateRow(root.transform, "Impact / Hit VFX", ImpactEntries, new Vector3(0f, 0f, 1.5f), materials);
            CreateRow(root.transform, "Beam / Line VFX", BeamEntries, new Vector3(0f, 0f, 7f), materials);
            CreateCameraAndLight();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save turret VFX lineup scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Turret VFX lineup scene built: " + ScenePath);
        }

        private static void CreateRow(Transform root, string title, IReadOnlyList<VfxEntry> entries, Vector3 rowOrigin, Materials materials)
        {
            GameObject rowRoot = new GameObject(title);
            rowRoot.transform.SetParent(root, false);
            rowRoot.transform.position = rowOrigin;
            CreateLabel(title, rowRoot.transform, new Vector3(-9.5f, 2.3f, 0f), 0.42f, Color.white);

            for (int i = 0; i < entries.Count; i++)
            {
                Vector3 localPosition = new Vector3(-5.7f + i * 3.8f, 0.2f, 0f);
                CreatePad(rowRoot.transform, entries[i].Name + "_Pad", localPosition + new Vector3(0f, -0.08f, 0f), materials.Pad);
                CreateLabel(entries[i].Name, rowRoot.transform, localPosition + new Vector3(0f, 1.7f, -0.75f), 0.22f, Color.white);
                CreateLabel(entries[i].SlotLabel, rowRoot.transform, localPosition + new Vector3(0f, 1.35f, -0.75f), 0.16f, materials.LabelTint);
                InstantiateEntry(entries[i], rowRoot.transform, localPosition, materials);
            }
        }

        private static void InstantiateEntry(VfxEntry entry, Transform parent, Vector3 localPosition, Materials materials)
        {
            GameObject prefab = LoadRequiredAsset<GameObject>(entry.Path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = entry.Name.Replace(" ", "_");
            instance.transform.localPosition = localPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;

            if (entry.Kind == VfxSlotKind.Beam)
            {
                ConfigureBeam(instance, localPosition, materials);
                return;
            }

            ConfigureParticles(instance);
        }

        private static void ConfigureParticles(GameObject root)
        {
            ParticleSystem[] particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            foreach (ParticleSystem particleSystem in particleSystems)
            {
                ParticleSystem.MainModule main = particleSystem.main;
                main.loop = true;
                main.playOnAwake = true;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                particleSystem.gameObject.SetActive(true);
                particleSystem.Play(true);
            }
        }

        private static void ConfigureBeam(GameObject root, Vector3 localPosition, Materials materials)
        {
            LineRenderer[] lineRenderers = root.GetComponentsInChildren<LineRenderer>(true);
            foreach (LineRenderer lineRenderer in lineRenderers)
            {
                lineRenderer.useWorldSpace = false;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, new Vector3(-1.35f, 0.55f, 0f));
                lineRenderer.SetPosition(1, new Vector3(1.35f, 0.55f, 0f));
                lineRenderer.widthMultiplier = Mathf.Max(0.08f, lineRenderer.widthMultiplier);
                lineRenderer.gameObject.SetActive(true);
            }

            CreateMarker(root.transform.parent, root.name + "_MuzzleMarker", localPosition + new Vector3(-1.5f, 0.55f, 0f), materials.Muzzle);
            CreateMarker(root.transform.parent, root.name + "_HitMarker", localPosition + new Vector3(1.5f, 0.55f, 0f), materials.Target);
        }

        private static void CreateEnvironment(Transform root, Materials materials)
        {
            CreateBlock("LineupFloor", root, new Vector3(0f, -0.12f, 1.5f), new Vector3(18f, 0.12f, 15f), materials.Floor);
            CreateLabel("Turret VFX Lineup - no turret prefab changes", root, new Vector3(0f, 3.1f, -7.2f), 0.45f, Color.white);
            CreateLabel("Slots: fireVfxPrefab / impactEffectPrefab / beamEffectPrefab", root, new Vector3(0f, 2.55f, -7.2f), 0.26f, materials.LabelTint);
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 12.5f, -15f);
            camera.transform.rotation = Quaternion.Euler(56f, 0f, 0f);
            camera.fieldOfView = 42f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.055f, 0.065f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.35f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject fillObject = new GameObject("Soft Fill Light");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 2.2f;
            fill.range = 18f;
            fill.transform.position = new Vector3(0f, 5f, -2f);
        }

        private static void CreatePad(Transform parent, string name, Vector3 localPosition, Material material)
        {
            CreateBlock(name, parent, localPosition, new Vector3(2.8f, 0.08f, 2.2f), material);
        }

        private static void CreateMarker(Transform parent, string name, Vector3 localPosition, Material material)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            marker.name = name;
            marker.transform.SetParent(parent, false);
            marker.transform.localPosition = localPosition;
            marker.transform.localScale = Vector3.one * 0.18f;
            marker.GetComponent<Renderer>().sharedMaterial = material;
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
            GameObject label = new GameObject("Label_" + text.Replace(" ", "_").Replace("/", "_"));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            label.transform.localRotation = Quaternion.Euler(65f, 0f, 0f);
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 48;
            textMesh.characterSize = size;
            textMesh.color = color;
        }

        private static void ValidateEntries(IEnumerable<VfxEntry> entries)
        {
            foreach (VfxEntry entry in entries)
            {
                if (LoadRequiredAsset<GameObject>(entry.Path) == null)
                {
                    throw new System.InvalidOperationException("Missing turret VFX candidate: " + entry.Path);
                }
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("[TurretVfxLineupSceneBuilder] Missing required asset: " + path);
            }

            return asset;
        }

        private static void CloseExistingTargetScene()
        {
            Scene existingScene = SceneManager.GetSceneByPath(ScenePath);
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
                CreateMaterial("LineupFloorMat", new Color(0.12f, 0.13f, 0.13f)),
                CreateMaterial("LineupPadMat", new Color(0.18f, 0.19f, 0.21f)),
                CreateMaterial("LineupMuzzleMarkerMat", new Color(1f, 0.7f, 0.18f)),
                CreateMaterial("LineupTargetMarkerMat", new Color(0.2f, 0.75f, 1f)),
                new Color(0.72f, 0.9f, 1f));
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

        private readonly struct VfxEntry
        {
            public VfxEntry(string name, string path, VfxSlotKind kind)
            {
                Name = name;
                Path = path;
                Kind = kind;
            }

            public string Name { get; }
            public string Path { get; }
            public VfxSlotKind Kind { get; }
            public string SlotLabel => Kind switch
            {
                VfxSlotKind.Fire => "fireVfxPrefab",
                VfxSlotKind.Impact => "impactEffectPrefab",
                _ => "beamEffectPrefab"
            };
        }

        private enum VfxSlotKind
        {
            Fire,
            Impact,
            Beam
        }

        private readonly struct Materials
        {
            public Materials(Material floor, Material pad, Material muzzle, Material target, Color labelTint)
            {
                Floor = floor;
                Pad = pad;
                Muzzle = muzzle;
                Target = target;
                LabelTint = labelTint;
            }

            public Material Floor { get; }
            public Material Pad { get; }
            public Material Muzzle { get; }
            public Material Target { get; }
            public Color LabelTint { get; }
        }
    }
}
