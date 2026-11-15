// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// The in-map skip button as stable draws it: just the skin's <c>play-skip</c> image in the bottom right corner.
    /// </summary>
    public partial class StableSkipButton : OsuClickableContainer
    {
        private StableSkinSprite sprite = null!;

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        /// <summary>Size of the button in the parent's layout units.</summary>
        public Vector2 ButtonSize { get; private set; } = new Vector2(100);

        public StableSkipButton()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Child = sprite = new StableSkinSprite(@"play-skip")
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
            };
        }

        protected override void Update()
        {
            base.Update();

            float s = StableUi.DesignScale;
            sprite.Scale = new Vector2(s);

            if (sprite.Texture != null)
                ButtonSize = new Vector2(sprite.Texture.DisplayWidth, sprite.Texture.DisplayHeight) * s;
        }
    }
}
