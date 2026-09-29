using System.Collections.Generic;
using Game.Content;

namespace Game.Fluids
{
    public static class FluidIDRegistry
    {
        private static readonly Dictionary<string, ushort> byName = new Dictionary<string, ushort>(System.StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<ushort, string> byId = new Dictionary<ushort, string>();
        private static ushort next = 1;
        public static ushort GetOrRegister(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return 0;
            if (byName.TryGetValue(id, out ushort value)) return value;
            value = next++; byName[id] = value; byId[value] = id; return value;
        }
        public static string GetString(ushort id) { return id == 0 ? "" : (byId.TryGetValue(id, out string s) ? s : ""); }
        public static void RegisterLoadedFluids() { foreach (FluidDefinition f in FluidRegistry.GetAll()) GetOrRegister(f.ID.ToString()); }
    }
}
