using System;
using CorridorCommander;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "CC Run Enemy Animation",
        description: "Runs the Corridor Commander enemy animation controller once per behavior tick.",
        story: "[Agent] runs enemy animation",
        category: "Action/Corridor Commander",
        id: "56fd1432fe33411a85868d3ed699cc8a")]
    public partial class CCRunEnemyAnimationAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private EnemyAnimationController controller;

        protected override Status OnStart()
        {
            GameObject agentObject = GetAgentObject();
            if (agentObject == null)
            {
                return Status.Failure;
            }

            controller = agentObject.GetComponent<EnemyAnimationController>();
            if (controller == null)
            {
                return Status.Running;
            }

            controller.SetUpdateLoopEnabled(false);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (controller == null)
            {
                return Status.Running;
            }

            controller.TickAnimation();
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
