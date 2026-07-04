using UnityEngine;
using System;
using System.Collections.Generic;

namespace CorridorCommander.PlayerControl
{
    public enum PlayerSquadCommandType
    {
        HoldPosition = 0,
        ReturnToPlayer = 1,
        Charge = 2,
        SelectAll = 3
    }

    [DisallowMultipleComponent]
    public sealed class PlayerSquadCommandController : MonoBehaviour
    {
        [Header("Targets")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Camera aimCamera;

        [Header("Aim")]
        [SerializeField] private LayerMask aimLayers = ~0;
        [SerializeField] private float aimDistance = 80f;
        [SerializeField] private PlayerSquadRoster roster;

        public bool TryIssueCommand(
            PlayerSquadCommandType commandType,
            string displayName,
            out string statusMessage)
        {
            ResolveRoster();
            List<AlliedSquadMemberFollower> members = new List<AlliedSquadMemberFollower>();
            roster?.FillCommandTargets(members);

            if (members.Count == 0)
            {
                statusMessage = "No selected squad member";
                return false;
            }

            Transform resolvedPlayerTarget = ResolvePlayerTarget();
            Vector3 chargeTarget = commandType == PlayerSquadCommandType.Charge
                ? ResolveAimPoint()
                : Vector3.zero;

            int issuedCount = 0;
            for (int i = 0; i < members.Count; i++)
            {
                AlliedSquadMemberFollower member = members[i];
                if (member == null)
                {
                    continue;
                }

                switch (commandType)
                {
                    case PlayerSquadCommandType.HoldPosition:
                        member.SetHoldPosition();
                        issuedCount++;
                        break;
                    case PlayerSquadCommandType.ReturnToPlayer:
                        member.ReturnToPlayer(resolvedPlayerTarget, i);
                        issuedCount++;
                        break;
                    case PlayerSquadCommandType.Charge:
                        member.SetChargeTarget(chargeTarget, i);
                        issuedCount++;
                        break;
                }
            }

            if (issuedCount <= 0)
            {
                statusMessage = "No valid squad members";
                return false;
            }

            string resolvedName = string.IsNullOrWhiteSpace(displayName)
                ? ResolveCommandName(commandType)
                : displayName;

            statusMessage = "Squad command: " + resolvedName + " x" + issuedCount.ToString();
            CommandIssued?.Invoke(commandType);
            return true;
        }

        public event Action<PlayerSquadCommandType> CommandIssued;

        public bool SelectMemberSlot(int slotNumber, out string statusMessage)
        {
            ResolveRoster();
            int slotIndex = slotNumber - 1;
            if (roster == null || !roster.SelectSlot(slotIndex))
            {
                statusMessage = $"No squad member in F{slotNumber}";
                return false;
            }

            statusMessage = $"Selected F{slotNumber}";
            return true;
        }

        public bool TryCallMemberSlot(int slotNumber, out string statusMessage)
        {
            if (!SelectMemberSlot(slotNumber, out statusMessage))
            {
                return false;
            }

            return TryIssueCommand(PlayerSquadCommandType.ReturnToPlayer, $"F{slotNumber} Call", out statusMessage);
        }

        public bool SelectAll(out string statusMessage)
        {
            ResolveRoster();
            if (roster == null || !roster.SelectAll())
            {
                statusMessage = "No squad members";
                return false;
            }

            statusMessage = "Selected all squad members";
            return true;
        }

        public bool SelectAdjacentMember(int direction, out string statusMessage)
        {
            ResolveRoster();
            if (roster == null || !roster.SelectAdjacent(direction))
            {
                statusMessage = "No squad member to select";
                return false;
            }

            int selectedSlot = roster.SelectedSlotIndex + 1;
            statusMessage = $"Selected F{selectedSlot}";
            return true;
        }

        private Transform ResolvePlayerTarget()
        {
            if (playerTarget != null)
            {
                return playerTarget;
            }

            PlayerLocomotionController locomotionController = GetComponentInParent<PlayerLocomotionController>();
            if (locomotionController != null)
            {
                playerTarget = locomotionController.transform;
                return playerTarget;
            }

            PlayerCentralInputController inputController = GetComponentInParent<PlayerCentralInputController>();
            if (inputController != null)
            {
                playerTarget = inputController.transform;
                return playerTarget;
            }

            playerTarget = transform;
            return playerTarget;
        }

        private Vector3 ResolveAimPoint()
        {
            Camera resolvedCamera = ResolveCamera();
            if (resolvedCamera == null)
            {
                return transform.position + transform.forward * Mathf.Max(1f, aimDistance);
            }

            Ray ray = resolvedCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, aimDistance, aimLayers, QueryTriggerInteraction.Ignore))
            {
                return hit.point;
            }

            return ray.origin + ray.direction * aimDistance;
        }

        private Camera ResolveCamera()
        {
            if (aimCamera != null)
            {
                return aimCamera;
            }

            aimCamera = Camera.main;
            return aimCamera;
        }

        private static string ResolveCommandName(PlayerSquadCommandType commandType)
        {
            return commandType switch
            {
                PlayerSquadCommandType.HoldPosition => "Hold",
                PlayerSquadCommandType.ReturnToPlayer => "Return",
                PlayerSquadCommandType.Charge => "Charge",
                PlayerSquadCommandType.SelectAll => "Select All",
                _ => "Squad"
            };
        }

        private void ResolveRoster()
        {
            if (roster == null)
            {
                roster = GetComponent<PlayerSquadRoster>();
            }

            if (roster == null)
            {
                roster = PlayerSquadRoster.Instance;
            }

            if (roster == null)
            {
                roster = FindFirstObjectByType<PlayerSquadRoster>(FindObjectsInactive.Include);
            }
        }
    }
}
