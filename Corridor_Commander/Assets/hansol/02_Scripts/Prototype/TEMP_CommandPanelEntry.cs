using System;
using UnityEngine;

namespace CorridorCommander
{
    [Serializable]
    public sealed class TEMP_CommandPanelEntry
    {
        [SerializeField] private string entryId = "entry";
        [SerializeField] private string displayName = "Entry";
        [SerializeField] [TextArea] private string description = "Temporary command entry";
        [SerializeField] private TEMP_CommandActionType actionType;
        [SerializeField] private TEMP_SquadCommandType squadCommandType;
        [SerializeField] [Min(0f)] private float value = 1f;
        [SerializeField] [Min(0f)] private float radius = 3f;

        public TEMP_CommandPanelEntry(
            string configuredId,
            string configuredDisplayName,
            string configuredDescription,
            TEMP_CommandActionType configuredActionType,
            TEMP_SquadCommandType configuredSquadCommandType = TEMP_SquadCommandType.HoldPosition,
            float configuredValue = 1f,
            float configuredRadius = 3f)
        {
            entryId = configuredId;
            displayName = configuredDisplayName;
            description = configuredDescription;
            actionType = configuredActionType;
            squadCommandType = configuredSquadCommandType;
            value = configuredValue;
            radius = configuredRadius;
        }

        public string EntryId => entryId;
        public string DisplayName => displayName;
        public string Description => description;
        public TEMP_CommandActionType ActionType => actionType;
        public TEMP_SquadCommandType SquadCommandType => squadCommandType;
        public float Value => value;
        public float Radius => radius;
    }
}
