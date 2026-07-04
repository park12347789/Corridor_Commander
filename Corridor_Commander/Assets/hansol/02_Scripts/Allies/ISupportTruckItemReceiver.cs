namespace CorridorCommander
{
    public interface ISupportTruckItemReceiver
    {
        bool TryReceiveSupportTruckItem(
            SupportTruckShopItemGrant itemGrant,
            int amount,
            out string statusMessage);
    }
}
