using System;
using CorridorCommander;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "CC Run Enemy Movement",
        description: "Runs the Corridor Commander enemy movement controller once per behavior tick.",
        story: "[Agent] runs enemy movement",
        category: "Action/Corridor Commander",
        id: "11b8aa5b8ad9420ca63166107b44c101")]
    public partial class CCRunEnemyMovementAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private EnemyMovementController controller;

        protected override Status OnStart()
        {
            GameObject agentObject = GetAgentObject();
            if (agentObject == null)
            {
                return Status.Failure;
            }

            controller = agentObject.GetComponent<EnemyMovementController>();
            if (controller == null)
            {
                return Status.Failure;
            }

            controller.SetUpdateLoopEnabled(false);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (controller == null)
            {
                return Status.Failure;
            }

            controller.TickMovement();
            return Status.Running;
        }

        protected override void OnEnd()
        {
            if (controller != null)
            {
                controller.SetUpdateLoopEnabled(true);
            }

            controller = null;
        }

        private GameObject GetAgentObject()
        {
            return Agent != null && Agent.Value != null ? Agent.Value : GameObject;
        }
    }
}
