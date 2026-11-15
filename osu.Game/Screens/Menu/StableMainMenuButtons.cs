// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// osu!stable's vertical Play / Edit / Options / Exit buttons, drawn with stable's own (skinnable) art.
    /// Lazer's <see cref="ButtonSystem"/> keeps running underneath for state and logo handling; this only replaces how it looks.
    /// </summary>
    public partial class StableMainMenuButtons : Container
    {
        private readonly ButtonSystem buttons;

        public StableMainMenuButtons(ButtonSystem buttons)
        {
            this.buttons = buttons;
            RelativeSizeAxes = Axes.Both;
            Alpha = 0;
            AlwaysPresent = true;
            Depth = 1;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            string[] names = { "play", "edit", "options", "exit" };
            Action?[] actions =
            {
                () => buttons.OnSolo?.Invoke(),
                () => buttons.OnEditBeatmap?.Invoke(),
                () => buttons.OnSettings?.Invoke(),
                () => buttons.OnExit?.Invoke(new ClickEvent(new osu.Framework.Input.States.InputState(), osuTK.Input.MouseButton.Left, Vector2.Zero)),
            };

            for (int i = 0; i < names.Length; i++)
            {
                Add(new StableButton(names[i], actions[i])
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    X = 0,
                    Y = (i - 1.5f) * 125,
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            buttons.StateChanged += s => Schedule(() => this.FadeTo(s == ButtonSystemState.Initial ? 0 : 1, 200));
        }

        private partial class StableButton : Container
        {
            private readonly string name;
            private readonly Action? action;
            private StableSkinSprite normal = null!;
            private StableSkinSprite over = null!;

            public StableButton(string name, Action? action)
            {
                this.name = name;
                this.action = action;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                Scale = new Vector2(StableUi.SCALE);

                AddRange(new Drawable[]
                {
                    new StableSkinSprite(@"menu-button-background"),
                    normal = new StableSkinSprite($@"menu-button-{name}"),
                    over = new StableSkinSprite($@"menu-button-{name}-over") { Alpha = 0 },
                });
            }

            protected override bool OnHover(HoverEvent e)
            {
                over.FadeTo(1, 100);
                normal.FadeTo(0, 100);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                over.FadeTo(0, 100);
                normal.FadeTo(1, 100);
            }

            protected override bool OnClick(ClickEvent e)
            {
                action?.Invoke();
                return true;
            }
        }
    }
}
