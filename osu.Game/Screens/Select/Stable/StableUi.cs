// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// Global switch for the osu!stable-style UI. Resolved once at startup (a restart is required to change it),
    /// so layout constants that were previously compile-time can depend on it.
    /// </summary>
    public static class StableUi
    {
        public static bool Enabled { get; set; }

        public static readonly Color4 SET_PANEL_COLOUR = new Color4(221, 71, 147, 255);
        public static readonly Color4 DIFFICULTY_PANEL_COLOUR = new Color4(1, 129, 202, 255);
        public static readonly Color4 SELECTED_PANEL_COLOUR = new Color4(217, 217, 217, 255);
        public static readonly Color4 GROUP_PANEL_COLOUR = new Color4(150, 65, 255, 255);

        /// <summary>
        /// Height of a beatmap (set) panel at 1080p, matching stable.
        /// </summary>
        public const float PANEL_HEIGHT = 122f;

        public const float GROUP_PANEL_HEIGHT = 64f;

        public const float FOOTER_HEIGHT = 110f;
    }
}
