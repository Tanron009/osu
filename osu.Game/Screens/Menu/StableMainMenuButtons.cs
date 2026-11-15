// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using System.Linq;
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
                    Y = (i - 1.5f) * 100,
                });
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Alpha = 1;

            buttons.StateChanged += s => Schedule(() =>
            {
                int i = 0;

                foreach (var b in Children.OfType<StableButton>())
                    b.SlideTo(s != ButtonSystemState.Initial && s != ButtonSystemState.Exit, i++);
            });
        }

        private partial class StableButton : Container
        {
            private readonly string name;
            private readonly Action? action;
            private StableSkinSprite normal = null!;
            private StableSkinSprite over = null!;

            [Resolved]
            private ISkinSource skin { get; set; } = null!;

            private const float offscreen_x = 900;

            public StableButton(string name, Action? action)
            {
                this.name = name;
                this.action = action;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;
                Scale = new Vector2(StableUi.SCALE * 0.5f);
                X = offscreen_x;

                AddRange(new Drawable[]
                {
                    normal = new StableSkinSprite($@"menu-button-{name}"),
                    over = new StableSkinSprite($@"menu-button-{name}-over") { Alpha = 0 },
                });
            }

            private bool shown;

            /// <summary>
            /// stable slides the buttons out from behind the logo one after another (and back when the menu closes).
            /// </summary>
            public void SlideTo(bool show, int index)
            {
                shown = show;
                this.Delay(index * 60).MoveToX(show ? 0 : offscreen_x, show ? 400 : 250, show ? Easing.OutQuint : Easing.InQuint);
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!shown)
                    return false;

                // stable pulls the hovered button towards the middle of the screen
                this.MoveToX(-30, 150, Easing.OutQuint);
                over.FadeTo(1, 100);
                normal.FadeTo(0, 100);

                if (!StableUi.PlaySkinSample(skin, $@"menu-{name}-hover"))
                    StableUi.PlaySkinSample(skin, StableUi.HoverSampleName);

                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (shown)
                    this.MoveToX(0, 250, Easing.OutQuint);

                over.FadeTo(0, 100);
                normal.FadeTo(1, 100);
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (!shown)
                    return false;

                if (!StableUi.PlaySkinSample(skin, $@"menu-{name}-click"))
                    StableUi.PlaySkinSample(skin, StableUi.ClickSampleName);

                action?.Invoke();
                return true;
            }
        }
    }
}
