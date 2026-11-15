// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osuTK;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// A container whose local coordinates are real screen pixels (at 1080p), regardless of lazer's UI scaling.
    /// All stable layout numbers (measured from 1920x1080 screenshots) can be used as-is inside of it.
    /// </summary>
    public partial class StableSpace : Container
    {
        public StableSpace()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateScale();
        }

        protected override void Update()
        {
            base.Update();
            updateScale();
        }

        private void updateScale()
        {
            if (Parent == null)
                return;

            float px = Parent.ToScreenSpace(Vector2.UnitX).X - Parent.ToScreenSpace(Vector2.Zero).X;

            if (px <= 0.01f)
                return;

            StableUi.PxPerUnit = px;

            // relative size: one parent-sized unit becomes `px` times bigger, then scaled back down so local units are screen pixels
            Size = new Vector2(px);
            Scale = new Vector2(1f / px);
        }
    }
}
