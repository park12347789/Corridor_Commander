using UnityEngine;

namespace CorridorCommander
{
    public interface IInteractionPromptSource
    {
        bool IsPromptVisible { get; }
        string PromptText { get; }
        Vector3 PromptWorldPosition { get; }
        float PromptDistanceSqr { get; }
        int PromptPriority { get; }
    }
}
