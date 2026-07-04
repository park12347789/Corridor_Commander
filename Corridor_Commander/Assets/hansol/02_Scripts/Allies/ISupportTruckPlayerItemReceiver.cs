using CorridorCommander.PlayerItems;

namespace CorridorCommander
{
    public interface ISupportTruckPlayerItemReceiver
    {
        bool TryReceiveSupportTruckPlayerItem(
            ItemDefinitionSO itemDefinition,
            int amount,
            out string statusMessage);
    }
}
