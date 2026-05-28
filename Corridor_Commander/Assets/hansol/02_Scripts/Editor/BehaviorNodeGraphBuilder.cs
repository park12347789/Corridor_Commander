using System;
using System.Linq;
using CorridorCommander;
using Unity.Behavior;
using Unity.Behavior.GraphFramework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CorridorCommander.EditorTools
{
    public static class BehaviorNodeGraphBuilder
    {
        private const string EnemyPrefabPath = "Assets/hansol/03_Prefabs/Enemy_Basic.prefab";
        private const string TurretPrefabPath = "Assets/hansol/03_Prefabs/Turret_Basic.prefab";
        private const string EnemyBehaviorPath = "Assets/hansol/09_Settings/Behavior/Enemy_Basic_Unity_Behavior.asset";
        private const string TurretBehaviorPath = "Assets/hansol/09_Settings/Behavior/Turret_Basic_Unity_Behavior.asset";
        private const string SpawnerBehaviorPath = "Assets/hansol/09_Settings/Behavior/EnemySpawner_Unity_Behavior.asset";

        [MenuItem("Corridor Commander/Build Behavior Node Graphs")]
        public static void BuildAll()
        {
            BuildAll(saveOpenScene: true);
        }

        public static void BuildAll(bool saveOpenScene)
        {
            EnsureFolder("Assets/hansol/09_Settings/Behavior");

            BehaviorGraph enemyGraph = BuildEnemyGraph();
            BehaviorGraph turretGraph = BuildTurretGraph();
            BehaviorGraph spawnerGraph = BuildSpawnerGraph();

            AssignPrefabGraph(EnemyPrefabPath, enemyGraph);
            AssignPrefabGraph(TurretPrefabPath, turretGraph);
            AssignSceneSpawnerGraph(spawnerGraph, saveOpenScene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static BehaviorGraph BuildEnemyGraph()
        {
            BehaviorAuthoringGraph graph = LoadOrCreateGraph(EnemyBehaviorPath);
            ResetGraph(graph);

            NodeModel start = CreateBehaviorNode(graph, typeof(Start), new Vector2(0f, 0f));
            NodeModel parallel = CreateBehaviorNode(graph, typeof(ParallelAllComposite), new Vector2(260f, 0f));
            NodeModel movement = CreateBehaviorNode(graph, typeof(CCRunEnemyMovementAction), new Vector2(560f, -80f));
            NodeModel melee = CreateBehaviorNode(graph, typeof(CCRunEnemyMeleeAttackAction), new Vector2(560f, 100f));

            LinkSelf(graph, movement, "Agent");
            LinkSelf(graph, melee, "Agent");

            ConnectDefault(graph, start, parallel);
            ConnectDefault(graph, parallel, movement);
            ConnectDefault(graph, parallel, melee);

            return SaveRuntimeGraph(graph);
        }

        private static BehaviorGraph BuildTurretGraph()
        {
            BehaviorAuthoringGraph graph = LoadOrCreateGraph(TurretBehaviorPath);
            ResetGraph(graph);

            NodeModel start = CreateBehaviorNode(graph, typeof(Start), new Vector2(0f, 0f));
            NodeModel targeting = CreateBehaviorNode(graph, typeof(CCRunTurretTargetingAction), new Vector2(300f, 0f));

            LinkSelf(graph, targeting, "Agent");
            ConnectDefault(graph, start, targeting);

            return SaveRuntimeGraph(graph);
        }

        private static BehaviorGraph BuildSpawnerGraph()
        {
            BehaviorAuthoringGraph graph = LoadOrCreateGraph(SpawnerBehaviorPath);
            ResetGraph(graph);

            NodeModel start = CreateBehaviorNode(graph, typeof(Start), new Vector2(0f, 0f));
            NodeModel spawner = CreateBehaviorNode(graph, typeof(CCRunEnemySpawnerAction), new Vector2(300f, 0f));

            LinkSelf(graph, spawner, "Agent");
            ConnectDefault(graph, start, spawner);

            return SaveRuntimeGraph(graph);
        }

        private static BehaviorAuthoringGraph LoadOrCreateGraph(string path)
        {
            BehaviorAuthoringGraph graph = AssetDatabase.LoadAssetAtPath<BehaviorAuthoringGraph>(path);
            if (graph != null)
            {
                return graph;
            }

            graph = ScriptableObject.CreateInstance<BehaviorAuthoringGraph>();
            graph.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AssetDatabase.CreateAsset(graph, path);
            return graph;
        }

        private static void ResetGraph(BehaviorAuthoringGraph graph)
        {
            GraphAssetProcessor.EnsureBlackboardGraphOwnerVariable(graph.Blackboard);
            graph.Nodes.Clear();
            graph.SetAssetDirty(true);
        }

        private static NodeModel CreateBehaviorNode(BehaviorAuthoringGraph graph, Type runtimeNodeType, Vector2 position)
        {
            NodeInfo info = Unity.Behavior.NodeRegistry.GetInfo(runtimeNodeType);
            if (info == null)
            {
                throw new InvalidOperationException($"Unity Behavior node not registered: {runtimeNodeType.FullName}");
            }

            Type modelType = info.ModelType;
            return graph.CreateNode(modelType, position, args: new object[] { info });
        }

        private static void LinkSelf(BehaviorAuthoringGraph graph, NodeModel node, string fieldName)
        {
            if (node is not BehaviorGraphNodeModel behaviorNode)
            {
                return;
            }

            VariableModel self = graph.Blackboard.Variables.FirstOrDefault(variable =>
                variable.ID == BehaviorGraph.k_GraphSelfOwnerID && variable.Type == typeof(GameObject));

            if (self != null)
            {
                behaviorNode.SetField(fieldName, self, typeof(GameObject));
            }
        }

        private static void ConnectDefault(BehaviorAuthoringGraph graph, NodeModel from, NodeModel to)
        {
            if (!from.TryDefaultOutputPortModel(out PortModel outputPort) ||
                !to.TryDefaultInputPortModel(out PortModel inputPort))
            {
                throw new InvalidOperationException($"Cannot connect {from.GetType().Name} to {to.GetType().Name}");
            }

            graph.ConnectEdge(outputPort, inputPort);
        }

        private static BehaviorGraph SaveRuntimeGraph(BehaviorAuthoringGraph graph)
        {
            graph.ValidateAsset();
            BehaviorGraph runtimeGraph = graph.BuildRuntimeGraph(forceRebuild: true);
            EditorUtility.SetDirty(graph);
            if (runtimeGraph != null)
            {
                EditorUtility.SetDirty(runtimeGraph);
            }

            return runtimeGraph;
        }

        private static void AssignPrefabGraph(string prefabPath, BehaviorGraph graph)
        {
            if (graph == null)
            {
                return;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                BehaviorGraphAgent agent = root.GetComponent<BehaviorGraphAgent>();
                if (agent == null)
                {
                    agent = root.AddComponent<BehaviorGraphAgent>();
                }

                SetAgentGraph(agent, graph);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AssignSceneSpawnerGraph(BehaviorGraph graph, bool saveOpenScene)
        {
            if (graph == null)
            {
                return;
            }

            bool changed = false;
            foreach (EnemySpawner spawner in UnityEngine.Object.FindObjectsByType<EnemySpawner>(FindObjectsSortMode.None))
            {
                BehaviorGraphAgent agent = spawner.GetComponent<BehaviorGraphAgent>();
                if (agent == null)
                {
                    agent = spawner.gameObject.AddComponent<BehaviorGraphAgent>();
                    changed = true;
                }

                if (agent.Graph != graph)
                {
                    SetAgentGraph(agent, graph);
                    changed = true;
                }
            }

            if (!changed)
            {
                return;
            }

            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(activeScene);
                if (saveOpenScene)
                {
                    EditorSceneManager.SaveScene(activeScene);
                }
            }
        }

        private static void SetAgentGraph(BehaviorGraphAgent agent, BehaviorGraph graph)
        {
            SerializedObject serializedAgent = new SerializedObject(agent);
            serializedAgent.FindProperty("m_Graph").objectReferenceValue = graph;
            serializedAgent.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            string folderName = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
                AssetDatabase.CreateFolder(parent, folderName);
            }
        }
    }
}
