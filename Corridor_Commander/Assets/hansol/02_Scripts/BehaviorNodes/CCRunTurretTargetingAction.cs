using System;
using CorridorCommander;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "CC Run Turret Targeting",
        description: "Runs the Corridor Commander turret targeting controller once per behavior tick.",
        story: "[Agent] runs turret targeting",
        category: "Action/Corridor Commander",
        id: "2a835af125724c588a1695f8cd4176d9")]
    public partial class CCRunTurretTargetingAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private TurretTargetingController controller;

        protected override Status OnStart()
        {
            GameObject agentObject = GetAgentObject();
            if (agentObject == null)
            {
                return Status.Failure;
            }

            controller = agentObject.GetComponent<TurretTargetingController>();
            if (controller == null)
            {
                return Status.Failure;
            }

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (controller == null)
            {
                return Status.Failure;
            }

            controller.TickTargeting();
            return Status.Running;
        }

        protected override void OnEnd()
        {
            controller = null;
        }

        private GameObject GetAgentObject()
        {
            return Agent != null && Agent.Value != null ? Agent.Value : GameObject;
        }
    }
}
