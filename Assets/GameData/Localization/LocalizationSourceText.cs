using UnityEngine;

namespace Game.Localization
{
    /// <summary>
    /// Preserves the untranslated source for labels localized while they are
    /// created, so language switching is reversible without visible flashing.
    /// </summary>
    public sealed class LocalizationSourceText : MonoBehaviour
    {
        public string Source;
    }
}
