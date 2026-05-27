using System;
using CorridorCommander;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "CC Run Enemy Spawner",
        description: "Runs the Corridor Commander enemy spawner once per behavior tick.",
        story: "[Agent] runs enemy spawner",
        category: "Action/Corridor Commander",
        id: "35aeb4bf313c4d5bb3fd82b9c960c26b")]
    public partial class CCRunEnemySpawnerAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private EnemySpawner spawner;

        protected override Status OnStart()
        {
            GameObject agentObject = GetAgentObject();
            if (agentObject == null)
            {
                return Status.Failure;
            }

            spawner = agentObject.GetComponent<EnemySpawner>();
            if (spawner == null)
            {
                return Status.Failure;
            }

            spawner.SetUpdateLoopEnabled(false);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (spawner == null)
            {
                return Status.Failure;
            }

            spawner.TickSpawner();
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (spawner != null)
            {
                spawner.SetUpdateLoopEnabled(true);
            }

            spawner = null;
        }

        private GameObject GetAgentObject()
        {
            return Agent != null && Agent.Value != null ? Agent.Value : GameObject;
        }
    }
}
