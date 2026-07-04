using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class TurretBeamVfxCandidateSceneBuilder
    {
        private const string ScenePath = "Assets/hansol/01_Scenes/test1/turret_beam_vfx_candidates.unity";

        private static readonly BeamEntry[] CandidateEntries =
        {
            new BeamEntry("Blue Light", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_BlueLight_Safe.prefab"),
            new BeamEntry("Blue Star", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_BlueStar_Safe.prefab"),
            new BeamEntry("Gold Arrow", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_GoldArrow_Safe.prefab"),
            new BeamEntry("Magic Light", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_MagicLight_Safe.prefab"),
            new BeamEntry("Star Line", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_StarLine_Safe.prefab"),
            new BeamEntry("Yellow Bullet", "Assets/hansol/03_Prefabs/VFX/SafeCopies/TurretBeam_Source_YellowBullet_Safe.prefab"),
            new BeamEntry("Weapon Blue Star", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Beam_BlueStar_Safe.prefab"),
            new BeamEntry("Weapon Laser Gun", "Assets/hansol/03_Prefabs/VFX/SafeCopies/Weapon_Beam_LaserGun_Safe.prefab")
        };

        [MenuItem("Corridor Commander/Art/Build Turret Beam VFX Candidates")]
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
            ValidateEntries();
            CloseExistingTargetScene();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, mode);
            scene.name = "turret_beam_vfx_candidates";
            EditorSceneManager.SetActiveScene(scene);

            Materials materials = CreateMaterials();
            GameObject root = new GameObject("TurretBeamVfxCandidatesRoot");
            CreateEnvironment(root.transform, materials);
            CreateRows(root.transform, materials);
            CreateCameraAndLight();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new System.InvalidOperationException("Failed to save turret beam VFX candidate scene: " + ScenePath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Turret beam VFX candidate scene built: " + ScenePath);
        }

        private static void CreateRows(Transform root, Materials materials)
        {
            for (int i = 0; i < CandidateEntries.Length; i++)
            {
                int row = i / 4;
                int column = i % 4;
                Vector3 position = new Vector3(-6f + column * 4f, 0.2f, -1.8f + row * 5.2f);
                BeamEntry entry = CandidateEntries[i];

                CreateBlock(entry.Name + "_Pad", root, position + new Vector3(0f, -0.08f, 0f), new Vector3(3.2f, 0.08f, 2.4f), materials.Pad);
                CreateLabel(entry.Name, root, position + new Vector3(0f, 2.05f, -0.9f), 0.23f, Color.white);
                CreateLabel("beamVfxPrefab candidate", root, position + new Vector3(0f, 1.68f, -0.9f), 0.16f, materials.LabelTint);
                InstantiateBeam(entry, root, position);
            }
        }

        private static void InstantiateBeam(BeamEntry entry, Transform parent, Vector3 position)
        {
            GameObject prefab = LoadRequiredAsset<GameObject>(entry.Path);
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = entry.Name.Replace(" ", "_");
            instance.transform.localPosition = position;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 0.45f;
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
                particleSystem.gameObject.SetActive(true);
                particleSystem.Play(true);
            }
        }

        private static void CreateEnvironment(Transform root, Materials materials)
        {
            CreateBlock("BeamCandidateFloor", root, new Vector3(0f, -0.12f, 0.8f), new Vector3(18f, 0.12f, 11f), materials.Floor);
            CreateLabel("Turret Beam VFX Candidates - particle source samples", root, new Vector3(0f, 3.05f, -5.8f), 0.43f, Color.white);
            CreateLabel("Rejected LineRenderer beams are not included", root, new Vector3(0f, 2.5f, -5.8f), 0.24f, materials.LabelTint);
        }

        private static void CreateCameraAndLight()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0f, 10.5f, -12.5f);
            camera.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            camera.fieldOfView = 43f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.045f, 0.05f, 0.06f);

            GameObject lightObject = new GameObject("Directional Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            GameObject fillObject = new GameObject("Soft Fill Light");
            Light fill = fillObject.AddComponent<Light>();
            fill.type = LightType.Point;
            fill.intensity = 2.4f;
            fill.range = 18f;
            fill.transform.position = new Vector3(0f, 4.5f, -1f);
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

        private static void ValidateEntries()
        {
            foreach (BeamEntry entry in CandidateEntries)
            {
                if (LoadRequiredAsset<GameObject>(entry.Path) == null)
                {
                    throw new System.InvalidOperationException("Missing beam candidate: " + entry.Path);
                }
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                Debug.LogError("[TurretBeamVfxCandidateSceneBuilder] Missing required asset: " + path);
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
                CreateMaterial("BeamCandidateFloorMat", new Color(0.11f, 0.12f, 0.14f)),
                CreateMaterial("BeamCandidatePadMat", new Color(0.18f, 0.18f, 0.22f)),
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

        private readonly struct BeamEntry
        {
            public BeamEntry(string name, string path)
            {
                Name = name;
                Path = path;
            }

            public string Name { get; }
            public string Path { get; }
        }

        private readonly struct Materials
        {
            public Materials(Material floor, Material pad, Color labelTint)
            {
                Floor = floor;
                Pad = pad;
                LabelTint = labelTint;
            }

            public Material Floor { get; }
            public Material Pad { get; }
            public Color LabelTint { get; }
        }
    }
}
