using System;
using System.Globalization;

namespace ExactFertilityGrowingZones
{
    // Cache only presentation text, never a cell's acceptance or fertility.
    // The dragger can request the same rejection for thousands of cells per frame.
    internal sealed class FertilityMessageCache
    {
        private readonly Func<float, string> createMessage;
        private bool hasValue;
        private float lastTarget;
        private object lastLanguage;
        private CultureInfo lastCulture;
        private string message;

        internal FertilityMessageCache(Func<float, string> createMessage)
        {
            this.createMessage = createMessage;
        }

        internal string Get(float target, object language)
        {
            CultureInfo culture = CultureInfo.CurrentCulture;
            if (!hasValue || target != lastTarget || !ReferenceEquals(language, lastLanguage)
                || !ReferenceEquals(culture, lastCulture))
            {
                // Commit the key only after successful formatting/translation.
                string updated = createMessage(target);
                lastTarget = target;
                lastLanguage = language;
                lastCulture = culture;
                message = updated;
                hasValue = true;
            }
            return message;
        }
    }
}
