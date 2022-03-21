using System;

using Microsoft.Xna.Framework;

using Newtonsoft.Json;

using Leclair.Stardew.ThemeManager;

namespace ThemeManager
{
    class ThemeData : BaseThemeData
    {

        public float TextScale { get; set; } = 1;

        [JsonConverter(typeof(ColorConverter))]
        public Color? TextColor { get; set; }

    }
}
