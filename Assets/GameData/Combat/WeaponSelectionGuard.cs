namespace Game.Combat
{
    public static class WeaponSelectionGuard
    {
        public static bool IsWeaponSelected(
            PlayerInventoryWeaponBridge inventory)
        {
            if (inventory == null)
                return false;

            return WeaponMetadataRegistry.IsWeapon(
                inventory.GetSelectedItemId()
            );
        }
    }
}
