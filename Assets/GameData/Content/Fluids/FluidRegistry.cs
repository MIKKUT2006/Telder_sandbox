using System.Collections.Generic;
using Game.Content;

namespace Game.Fluids
{
    public static class FluidRegistry
    {
        private static readonly ContentRegistry<FluidDefinition> registry = new ContentRegistry<FluidDefinition>();
        public static void Register(FluidDefinition fluid) { if (fluid != null) registry.Register(fluid.ID, fluid); }
        public static FluidDefinition Get(ContentID id) => registry.Get(id);
        public static bool Contains(ContentID id) => registry.Contains(id);
        public static IEnumerable<FluidDefinition> GetAll() => registry.GetAll();
        public static int Count => registry.Count;
        public static void Clear() => registry.Clear();

        public static bool TryGet(string id, out FluidDefinition fluid)
        {
            fluid = null;
            if (string.IsNullOrWhiteSpace(id)) return false;
            try { ContentID cid = ContentID.Parse(id); if (!registry.Contains(cid)) return false; fluid = registry.Get(cid); return fluid != null; }
            catch { return false; }
        }
    }
}
