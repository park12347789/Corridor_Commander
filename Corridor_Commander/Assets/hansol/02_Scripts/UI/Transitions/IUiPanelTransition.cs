using System;

namespace CorridorCommander
{
    public interface IUiPanelTransition
    {
        bool IsVisible { get; }
        bool IsTransitioning { get; }

        void Show(Action onComplete = null);
        void Hide(Action onComplete = null);
        void HideImmediate();
    }
}
