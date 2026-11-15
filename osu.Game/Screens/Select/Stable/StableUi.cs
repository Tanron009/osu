// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

using osu.Game.Audio;
using osu.Game.Skinning;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// Global switch for the osu!stable-style UI. Resolved once at startup (a restart is required to change it),
    /// so layout constants that were previously compile-time can depend on it.
    /// </summary>
    public static class StableUi
    {
        public static bool Enabled { get; set; }

        /// <summary>Set while a skin's full-screen song select art replaces the osu! logo in the bottom right.</summary>
        public static bool HideLogo { get; set; }

        /// <summary>stable's main menu: logo position (fraction of screen width) and scale while the buttons are showing.</summary>
        public const float MENU_LOGO_X = 695f / 1920f;

        public const float MENU_LOGO_SCALE = 1.24f;

        /// <summary>stable skin sample names used for general menu navigation (hover / click).</summary>
        public const string HoverSampleName = @"click-short";

        public const string ClickSampleName = @"click-short-confirm";

        /// <summary>
        /// Plays a sample from the skin if it provides one.
        /// </summary>
        /// <returns>Whether a skin sample was played.</returns>
        public static bool PlaySkinSample(ISkinSource? skin, string name)
        {
            var channel = skin?.GetSample(new SampleInfo(name))?.GetChannel();

            if (channel == null)
                return false;

            channel.Play();
            return true;
        }

        /// <summary>
        /// stable lays everything out for 1366x768 and scales to the window height; at 1080p that is this factor.
        /// </summary>
        public const float SCALE = 1080f / 768f;

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
