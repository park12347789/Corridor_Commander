using System.Collections.Generic;
using UnityEngine;

namespace CorridorCommander
{
    public interface IInstalledObjectActionProvider
    {
        string Prompt { get; }
        string Title { get; }
        string GetSummary();
        void CollectActions(IList<InstalledObjectAction> actions);
        bool ExecuteAction(int actionIndex, Transform player, out string statusMessage);
    }
}
