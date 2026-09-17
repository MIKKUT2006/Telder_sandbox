using System.Reflection;
using Game.Inventory;
using Game.Inventory.UI;

namespace Game.ToolBench
{
    // Adapter for the current InventoryUI, where cursorStack and RefreshAll are private.
    internal static class InventoryUIAccess
    {
        private static FieldInfo cursorField;
        private static MethodInfo refreshMethod;
        private static bool initialized;

        private static void Initialize()
        {
            if (initialized) return;
            initialized = true;

            System.Type type = typeof(InventoryUI);
            cursorField = type.GetField("cursorStack", BindingFlags.Instance | BindingFlags.NonPublic);
            refreshMethod = type.GetMethod("RefreshAll", BindingFlags.Instance | BindingFlags.NonPublic);
        }

        public static ItemStack GetCursor(InventoryUI ui)
        {
            Initialize();
            return ui == null || cursorField == null ? null : cursorField.GetValue(ui) as ItemStack;
        }

        public static void RefreshAll(InventoryUI ui)
        {
            Initialize();
            if (ui != null && refreshMethod != null)
                refreshMethod.Invoke(ui, null);
        }
    }
}
