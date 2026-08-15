using HarmonyLib;
using StardewModdingAPI;

namespace ForjaLivre
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance { get; private set; } = null!;

        internal static ModConfig Config { get; private set; } = null!;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = helper.ReadConfig<ModConfig>();

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }
    }
}
