using UnityEngine;

namespace CorridorCommander
{
    public interface IInteractionPromptScreenPosition
    {
        bool TryGetPromptScreenPosition(Vector2 screenSize, Vector2 promptSize, out Vector2 screenPosition);
    }
}
