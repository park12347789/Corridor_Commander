using System.IO;
using System.Linq;
using CorridorCommander;
using Unity.Behavior;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class ZombieEnemyPrefabBuilder
    {
        private const string ZombieVisualPrefabPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/prefabs/zombie_male/TT_zombie_M_22_soldier.prefab";
        private const string ZombieEnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Zombie_Basic.prefab";
        private const string ZombieMaterialPath = "Assets/hansol/04_Materials/Zombie_TT_A_URP.mat";
        private const string ZombieAnimatorControllerPath = "Assets/hansol/09_Settings/Animation/Zombie_Basic.controller";
        private const string ZombieIdleClipPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/animation/Z_idle_A.FBX";
        private const string ZombieWalkClipPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/animation/Z_walk.FBX";
        private const string ZombieAttackClipPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/animation/Z_melee_attack_A.FBX";
        private const string ZombieDeathClipPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/animation/Death/Z_death_A.FBX";
        private const string ZombieDeadClipPath = "Assets/90_ThirdParty/ToonyTinyPeople/TT_Zombies/animation/Death/Z_dead_A.FBX";
        private const string EnemyBehaviorPath = "Assets/hansol/09_Settings/Behavior/Enemy_Basic_Unity_Behavior.asset";
        private const string EnemySpawnerPrefabPath = "Assets/hansol/03_Prefabs/Enemy_SpawnPoint_RED.prefab";
        private const string StageLayoutPrefabPath = "Assets/hansol/03_Prefabs/Stage/StageLayout_RoomCorridorSamples.prefab";
        private const string StageDefinitionPath = "Assets/hansol/09_Settings/Stage/Stage_RoomCorridorSamples.asset";
        private const string StageScenePath = "Assets/hansol/01_Scenes/test1/stage_room_corridor_samples.unity";
        private const string MoveSpeedParameter = "MoveSpeed";
        private const string AttackTriggerParameter = "Attack";
        private const string DeathTriggerParameter = "Death";

        [MenuItem("Corridor Commander/Enemies/Build Zombie Basic Enemy")]
        public static void Build()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildInternal(updateStageScene: true);
        }

        public static void BuildForAutomation()
        {
            BuildInternal(updateStageScene: true);
        }

        private static void BuildInternal(bool updateStageScene)
        {
            EnsureFolder(Path.GetDirectoryName(ZombieEnemyPrefabPath)?.Replace('\\', '/'));

            GameObject zombieEnemyPrefab = CreateZombieEnemyPrefab();
            UpdateEnemySpawnerPrefab(zombieEnemyPrefab);
            UpdateStageDefinition(zombieEnemyPrefab);
            UpdateStageLayoutPrefab(zombieEnemyPrefab);

            if (updateStageScene)
            {
                UpdateStageScene(zombieEnemyPrefab);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static GameObject CreateZombieEnemyPrefab()
        {
            GameObject zombieVisualPrefab = LoadRequiredAsset<GameObject>(ZombieVisualPrefabPath);
            Material zombieMaterial = LoadRequiredAsset<Material>(ZombieMaterialPath);
            AnimatorController zombieAnimatorController = ConfigureZombieAnimatorController();
            BehaviorGraph behaviorGraph = LoadRequiredBehaviorGraph(EnemyBehaviorPath);

            GameObject root = new GameObject("Enemy_Zombie_Basic");
            root.layer = 7;
            root.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

            EnemyMovementController movementController = root.AddComponent<EnemyMovementController>();
            SetSerialized(movementController, "waypointReachDistance", 0.65f);
            SetSerialized(movementController, "refreshInterval", 0.25f);
            SetSerialized(movementController, "runUpdateLoop", true);

            EnemyMeleeAttackController meleeAttack = root.AddComponent<EnemyMeleeAttackController>();
            SetSerialized(meleeAttack, "attackRange", 2.25f);
            SetSerialized(meleeAttack, "attackInterval", 0.6f);
            SetSerialized(meleeAttack, "damage", 20f);
            SetSerialized(meleeAttack, "runUpdateLoop", true);

            Health health = root.AddComponent<Health>();
            SetSerialized(health, "maxHitPoints", 30f);
            SetSerialized(health, "destroyOnDeath", false);

            BehaviorGraphAgent behaviorAgent = root.AddComponent<BehaviorGraphAgent>();
            behaviorAgent.Graph = behaviorGraph;

            EnemyAnimationController animationController = root.AddComponent<EnemyAnimationController>();
            SetSerialized(animationController, "fullWalkSpeed", 2.6f);
            SetSerialized(animationController, "destroyDelay", 1.35f);
            SetSerialized(animationController, "runUpdateLoop", true);
            SetSerialized(animationController, "destroyAfterDeath", true);

            CharacterController characterController = root.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.45f;
            characterController.slopeLimit = 55f;
            characterController.stepOffset = 0.35f;
            characterController.skinWidth = 0.08f;
            characterController.minMoveDistance = 0.001f;
            characterController.center = new Vector3(0f, 1f, 0f);

            NavMeshAgent navMeshAgent = root.AddComponent<NavMeshAgent>();
            navMeshAgent.radius = 0.45f;
            navMeshAgent.speed = 2.6f;
            navMeshAgent.acceleration = 8f;
            navMeshAgent.angularSpeed = 540f;
            navMeshAgent.stoppingDistance = 0.35f;
            navMeshAgent.height = 2f;
            navMeshAgent.baseOffset = 0f;
            navMeshAgent.autoTraverseOffMeshLink = false;

            NavMeshMovementMotor motor = root.AddComponent<NavMeshMovementMotor>();
            SetMovementStats(motor, 2.6f, 540f, 8f, 0.35f);
            SetSerialized(motor, "gravity", -18f);
            SetSerialized(motor, "maxOffMeshLinkDistance", 4f);
            SetSerialized(motor, "maxOffMeshLinkHeightDelta", 1.5f);

            CapsuleCollider capsuleCollider = root.AddComponent<CapsuleCollider>();
            capsuleCollider.radius = 0.5000001f;
            capsuleCollider.height = 2f;
            capsuleCollider.direction = 1;
            capsuleCollider.center = new Vector3(0.000000059604645f, 1f, -0.00000008940697f);

            GameObject visual = PrefabUtility.InstantiatePrefab(zombieVisualPrefab) as GameObject;
            if (visual == null)
            {
                Object.DestroyImmediate(root);
                throw new System.InvalidOperationException($"Could not instantiate zombie visual prefab: {ZombieVisualPrefabPath}");
            }

            visual.name = "Visual_TT_zombie_M_22_soldier";
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            SetLayerRecursively(visual, root.layer);
            ApplyVisualMaterial(visual, zombieMaterial);
            ConfigureVisualAnimators(visual, zombieAnimatorController);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, ZombieEnemyPrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static AnimatorController ConfigureZombieAnimatorController()
        {
            EnsureFolder(Path.GetDirectoryName(ZombieAnimatorControllerPath)?.Replace('\\', '/'));

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ZombieAnimatorControllerPath);
            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(ZombieAnimatorControllerPath);
            }

            EnsureParameter(controller, MoveSpeedParameter, AnimatorControllerParameterType.Float);
            EnsureParameter(controller, AttackTriggerParameter, AnimatorControllerParameterType.Trigger);
            EnsureParameter(controller, DeathTriggerParameter, AnimatorControllerParameterType.Trigger);

            AnimationClip idleClip = LoadRequiredAsset<AnimationClip>(ZombieIdleClipPath);
            AnimationClip walkClip = LoadRequiredAsset<AnimationClip>(ZombieWalkClipPath);
            AnimationClip attackClip = LoadRequiredAsset<AnimationClip>(ZombieAttackClipPath);
            AnimationClip deathClip = LoadRequiredAsset<AnimationClip>(ZombieDeathClipPath);
            AnimationClip deadClip = LoadRequiredAsset<AnimationClip>(ZombieDeadClipPath);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState moveState = FindOrCreateState(stateMachine, "Move", new Vector3(280f, 80f, 0f));
            AnimatorState attackState = FindOrCreateState(stateMachine, "Attack", new Vector3(560f, 0f, 0f));
            AnimatorState deathState = FindOrCreateState(stateMachine, "Death", new Vector3(560f, 160f, 0f));
            AnimatorState deadState = FindOrCreateState(stateMachine, "Dead", new Vector3(820f, 160f, 0f));

            moveState.motion = ConfigureMoveBlendTree(controller, moveState.motion, idleClip, walkClip);
            attackState.motion = attackClip;
            deathState.motion = deathClip;
            deadState.motion = deadClip;

            stateMachine.defaultState = moveState;
            ResetTransitions(stateMachine);

            AnimatorStateTransition attackTransition = stateMachine.AddAnyStateTransition(attackState);
            attackTransition.AddCondition(AnimatorConditionMode.If, 0f, AttackTriggerParameter);
            attackTransition.hasExitTime = false;
            attackTransition.duration = 0.05f;
            attackTransition.canTransitionToSelf = false;

            AnimatorStateTransition deathTransition = stateMachine.AddAnyStateTransition(deathState);
            deathTransition.AddCondition(AnimatorConditionMode.If, 0f, DeathTriggerParameter);
            deathTransition.hasExitTime = false;
            deathTransition.duration = 0.05f;
            deathTransition.canTransitionToSelf = false;

            AnimatorStateTransition attackReturn = attackState.AddTransition(moveState);
            attackReturn.hasExitTime = true;
            attackReturn.exitTime = 0.9f;
            attackReturn.duration = 0.1f;

            AnimatorStateTransition deathFinish = deathState.AddTransition(deadState);
            deathFinish.hasExitTime = true;
            deathFinish.exitTime = 0.95f;
            deathFinish.duration = 0.1f;

            EditorUtility.SetDirty(controller);
            return controller;
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string parameterName,
            AnimatorControllerParameterType parameterType)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == parameterName)
                {
                    return;
                }
            }

            controller.AddParameter(parameterName, parameterType);
        }

        private static AnimatorState FindOrCreateState(
            AnimatorStateMachine stateMachine,
            string stateName,
            Vector3 position)
        {
            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state != null && childState.state.name == stateName)
                {
                    return childState.state;
                }
            }

            return stateMachine.AddState(stateName, position);
        }

        private static Motion ConfigureMoveBlendTree(
            AnimatorController controller,
            Motion currentMotion,
            Motion idleClip,
            Motion walkClip)
        {
            BlendTree blendTree = currentMotion as BlendTree;
            if (blendTree == null)
            {
                blendTree = new BlendTree { name = "MoveSpeedBlend" };
                AssetDatabase.AddObjectToAsset(blendTree, controller);
            }

            blendTree.blendType = BlendTreeType.Simple1D;
            blendTree.blendParameter = MoveSpeedParameter;
            blendTree.useAutomaticThresholds = false;
            blendTree.children = new[]
            {
                new ChildMotion { motion = idleClip, threshold = 0f, timeScale = 1f },
                new ChildMotion { motion = walkClip, threshold = 0.4f, timeScale = 1f }
            };
            EditorUtility.SetDirty(blendTree);
            return blendTree;
        }

        private static void ResetTransitions(AnimatorStateMachine stateMachine)
        {
            foreach (AnimatorStateTransition transition in stateMachine.anyStateTransitions)
            {
                stateMachine.RemoveAnyStateTransition(transition);
            }

            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                if (childState.state == null)
                {
                    continue;
                }

                foreach (AnimatorStateTransition transition in childState.state.transitions)
                {
                    childState.state.RemoveTransition(transition);
                }
            }
        }

        private static void UpdateEnemySpawnerPrefab(GameObject zombieEnemyPrefab)
        {
            if (!File.Exists(EnemySpawnerPrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(EnemySpawnerPrefabPath);
            try
            {
                EnemySpawner spawner = root.GetComponent<EnemySpawner>();
                if (spawner != null)
                {
                    SetSerialized(spawner, "enemyPrefab", zombieEnemyPrefab);
                    PrefabUtility.SaveAsPrefabAsset(root, EnemySpawnerPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateStageDefinition(GameObject zombieEnemyPrefab)
        {
            StageDefinitionSO definition = AssetDatabase.LoadAssetAtPath<StageDefinitionSO>(StageDefinitionPath);
            if (definition == null)
            {
                return;
            }

            SetSerialized(definition, "enemyPrefab", zombieEnemyPrefab);
            EditorUtility.SetDirty(definition);
        }

        private static void UpdateStageLayoutPrefab(GameObject zombieEnemyPrefab)
        {
            if (!File.Exists(StageLayoutPrefabPath))
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(StageLayoutPrefabPath);
            try
            {
                bool changed = UpdateSpawners(root.GetComponentsInChildren<EnemySpawner>(true), zombieEnemyPrefab);
                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, StageLayoutPrefabPath);
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateStageScene(GameObject zombieEnemyPrefab)
        {
            if (!File.Exists(StageScenePath))
            {
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(StageScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                return;
            }

            bool changed = UpdateSpawners(Object.FindObjectsByType<EnemySpawner>(FindObjectsInactive.Include, FindObjectsSortMode.None), zombieEnemyPrefab);
            if (changed)
            {
                EditorSceneManager.SaveScene(scene);
            }
        }

        private static bool UpdateSpawners(EnemySpawner[] spawners, GameObject zombieEnemyPrefab)
        {
            bool changed = false;
            foreach (EnemySpawner spawner in spawners)
            {
                if (spawner == null)
                {
                    continue;
                }

                SetSerialized(spawner, "enemyPrefab", zombieEnemyPrefab);
                SetSerialized(spawner, "spawnHeightOffset", 0.35f);
                EditorUtility.SetDirty(spawner);
                changed = true;
            }

            return changed;
        }

        private static void SetMovementStats(MonoBehaviour motor, float moveSpeed, float rotationSpeed, float acceleration, float stoppingDistance)
        {
            SerializedObject serializedObject = new SerializedObject(motor);
            SerializedProperty stats = serializedObject.FindProperty("stats");
            stats.FindPropertyRelative("moveSpeed").floatValue = moveSpeed;
            stats.FindPropertyRelative("rotationSpeed").floatValue = rotationSpeed;
            stats.FindPropertyRelative("acceleration").floatValue = acceleration;
            stats.FindPropertyRelative("stoppingDistance").floatValue = stoppingDistance;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureVisualAnimators(GameObject visualRoot, RuntimeAnimatorController animatorController)
        {
            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);
            foreach (Animator animator in animators)
            {
                animator.runtimeAnimatorController = animatorController;
                animator.applyRootMotion = false;
            }
        }

        private static void ApplyVisualMaterial(GameObject visualRoot, Material material)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null)
                    {
                        materials[i] = material;
                    }
                }

                renderer.sharedMaterials = materials;
            }
        }

        private static void SetLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private static T LoadRequiredAsset<T>(string path) where T : Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                throw new System.InvalidOperationException($"Missing required asset: {path}");
            }

            return asset;
        }

        private static BehaviorGraph LoadRequiredBehaviorGraph(string path)
        {
            BehaviorGraph graph = AssetDatabase.LoadAllAssetsAtPath(path).OfType<BehaviorGraph>().FirstOrDefault();
            if (graph == null)
            {
                throw new System.InvalidOperationException($"Missing required BehaviorGraph: {path}");
            }

            return graph;
        }

        private static void EnsureFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void SetSerialized(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerialized(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerialized(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.boolValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }
    }
}
