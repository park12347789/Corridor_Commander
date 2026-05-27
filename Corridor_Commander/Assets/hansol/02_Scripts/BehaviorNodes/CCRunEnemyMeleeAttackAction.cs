using System;
using CorridorCommander;
using Unity.Properties;
using UnityEngine;

namespace Unity.Behavior
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(
        name: "CC Run Enemy Melee Attack",
        description: "Runs the Corridor Commander enemy melee attack controller once per behavior tick.",
        story: "[Agent] runs enemy melee attack",
        category: "Action/Corridor Commander",
        id: "1fb81ec627be4f0c9ff602e805d4a8c0")]
    public partial class CCRunEnemyMeleeAttackAction : Action
    {
        [SerializeReference] public BlackboardVariable<GameObject> Agent;

        private EnemyMeleeAttackController controller;

        protected override Status OnStart()
        {
            GameObject agentObject = GetAgentObject();
            if (agentObject == null)
            {
                return Status.Failure;
            }

            controller = agentObject.GetComponent<EnemyMeleeAttackController>();
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

            controller.TickMeleeAttack();
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
