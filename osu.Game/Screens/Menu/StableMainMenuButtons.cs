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
using osu.Framework.Utils;
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
        private StableSpace space = null!;

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

            space = new StableSpace();
            AddInternal(space);

            // measured from osu!stable: right edge / vertical centre of each button (1920x1080)
            float[] rightEdges = { 1530, 1548, 1548, 1530 };
            float[] centresY = { 339, 484, 631, 777 };

            for (int i = 0; i < names.Length; i++)
            {
                space.Add(new StableButton(names[i], actions[i])
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreRight,
                    RelativePositionAxes = Axes.X,
                    X = rightEdges[i] / 1920f,
                    Y = centresY[i] - 540,
                    EdgeX = rightEdges[i] / 1920f,
                });
            }
        }

        protected override void Update()
        {
            base.Update();

            // measured in stable: the logo (and buttons) follow the cursor by roughly 3.5% of its distance from the screen centre
            bool active = buttons.State == ButtonSystemState.TopLevel || buttons.State == ButtonSystemState.Play || buttons.State == ButtonSystemState.Initial;
            var input = GetContainingInputManager();
            var target = Vector2.Zero;

            if (active && input != null)
            {
                var centre = ToScreenSpace(DrawSize / 2f);
                target = (input.CurrentState.Mouse.Position - centre) * -0.025f;
            }

            var cur = StableUi.MenuParallax;
            StableUi.MenuParallax = new Vector2((float)Interpolation.DampContinuously(cur.X, target.X, 80, Time.Elapsed), (float)Interpolation.DampContinuously(cur.Y, target.Y, 80, Time.Elapsed));
            space.Position = StableUi.MenuParallax / StableUi.PxPerUnit;
        }

        protected override void Dispose(bool isDisposing)
        {
            StableUi.MenuParallax = Vector2.Zero;
            base.Dispose(isDisposing);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Alpha = 1;

            ButtonSystemState previous = buttons.State;

            buttons.StateChanged += s =>
            {
                var from = previous;
                previous = s;

                Schedule(() =>
                {
                    bool show = s == ButtonSystemState.TopLevel || s == ButtonSystemState.Play;

                    // the logo has to arrive first (it travels back from the song select corner), then the buttons come out of it
                    double delay = from == ButtonSystemState.EnteringMode ? 330 : 0;
                    int i = 0;

                    foreach (var b in space.Children.OfType<StableButton>())
                        b.SlideTo(show, i++, delay);
                });
            };
        }

        private partial class StableButton : Container
        {
            private readonly string name;
            private readonly Action? action;
            private StableSkinSprite normal = null!;
            private StableSkinSprite over = null!;

            [Resolved]
            private ISkinSource skin { get; set; } = null!;

            // stable slides the buttons out from behind the logo
            private const float offscreen_x = -420;

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
                Alpha = 0;

                AddRange(new Drawable[]
                {
                    normal = new StableSkinSprite($@"menu-button-{name}"),
                    over = new StableSkinSprite($@"menu-button-{name}-over") { Alpha = 0 },
                });
            }

            public float EdgeX;

            private bool shown;

            /// <summary>
            /// stable slides the buttons out from behind the logo one after another (and back when the menu closes).
            /// </summary>
            public void SlideTo(bool show, int index, double delay)
            {
                shown = show;
                ClearTransforms();

                float from = EdgeX - 300f / 1920f;

                if (show)
                {
                    // returning from song select (delay > 0) is a slower, clearly visible slide; opening the menu keeps the quick one
                    double fade = delay > 0 ? 160 : 100;
                    double slide = delay > 0 ? 700 : 400;
                    this.MoveToX(from).FadeOut();
                    // fade and slide run together, so the slide is visible instead of only fading in
                    this.Delay(delay + index * 60).FadeIn(fade);
                    this.Delay(delay + index * 60).MoveToX(EdgeX, slide, Easing.OutQuint);
                }
                else
                    this.MoveToX(from, 170, Easing.InQuad).FadeOut(170);
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!shown)
                    return false;

                // stable pulls the hovered button towards the middle of the screen
                this.MoveToX(EdgeX + 30f / 1920f, 150, Easing.OutQuint);
                over.FadeTo(1, 100);
                normal.FadeTo(0, 100);

                if (!StableUi.PlaySkinSample(skin, $@"menu-{name}-hover"))
                    StableUi.PlaySkinSample(skin, StableUi.HoverSampleName);

                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                if (shown)
                    this.MoveToX(EdgeX, 250, Easing.OutQuint);

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
