// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// The strip along the top of osu!stable's main menu: user info on the left, welcome text in the middle, music controls on the right.
    /// </summary>
    public partial class StableMainMenuTopBar : CompositeDrawable
    {
        private const float bar_height = 112;

        private readonly IBindable<APIUser> user = new Bindable<APIUser>();

        private Container avatarContainer = null!;
        private OsuSpriteText nameText = null!;
        private OsuSpriteText ppText = null!;
        private OsuSpriteText accText = null!;
        private OsuSpriteText levelText = null!;
        private Box levelFill = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private MusicController music { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = bar_height;

            const float level_bar_x = 183;
            const float level_bar_w = 277;

            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black, Alpha = 0.55f },
                avatarContainer = new Container { Size = new Vector2(105), Position = new Vector2(12, 0) },
                nameText = new OsuSpriteText { Position = new Vector2(122, -4), Font = OsuFont.GetFont(Typeface.Torus, 34, FontWeight.Regular) },
                ppText = new OsuSpriteText { Position = new Vector2(122, 30), Font = OsuFont.GetFont(Typeface.Torus, 22, FontWeight.Regular) },
                accText = new OsuSpriteText { Position = new Vector2(122, 52), Font = OsuFont.GetFont(Typeface.Torus, 22, FontWeight.Regular) },
                levelText = new OsuSpriteText { Position = new Vector2(122, 74), Font = OsuFont.GetFont(Typeface.Torus, 22, FontWeight.Regular) },
                new Container
                {
                    Position = new Vector2(level_bar_x, 86),
                    Size = new Vector2(level_bar_w, 14),
                    Masking = true,
                    CornerRadius = 7,
                    BorderThickness = 2,
                    BorderColour = new Color4(190, 190, 190, 255),
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(30, 30, 30, 255) },
                        levelFill = new Box { RelativeSizeAxes = Axes.Y, Colour = new Color4(255, 200, 40, 255) },
                    }
                },
                createMusicControls(),
            };

            user.BindTo(api.LocalUser);
            user.BindValueChanged(_ => refreshUser(), true);
        }

        private Drawable createMusicControls()
        {
            var flow = new FillFlowContainer
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(14, 0),
                Position = new Vector2(-20, 8),
            };

            (IconUsage, Action)[] items =
            {
                (FontAwesome.Solid.StepBackward, () => music.PreviousTrack()),
                (FontAwesome.Solid.Play, () => music.Play(requestedByUser: true)),
                (FontAwesome.Solid.Pause, () => music.TogglePause()),
                (FontAwesome.Solid.Stop, () => music.Stop(true)),
                (FontAwesome.Solid.StepForward, () => music.NextTrack()),
                (FontAwesome.Solid.InfoCircle, () => { }),
                (FontAwesome.Solid.Bars, () => { }),
            };

            foreach (var (icon, action) in items)
                flow.Add(new ControlButton(icon, action));

            return flow;
        }

        private void refreshUser()
        {
            var u = user.Value;

            avatarContainer.Child = new UpdateableAvatar(u, false) { RelativeSizeAxes = Axes.Both };
            nameText.Text = u.Username;

            var stats = u.Statistics;

            ppText.Text = stats.PP != null ? $"Performance:{stats.PP:N0}pp" : string.Empty;
            accText.Text = stats.Accuracy > 0 ? $"Accuracy:{stats.Accuracy:0.00}%" : string.Empty;
            levelText.Text = stats.Level.Current > 0 ? $"Lv{stats.Level.Current}" : string.Empty;
            levelFill.Width = 277 * Math.Clamp(stats.Level.Progress / 100f, 0, 1);
        }

        private partial class ControlButton : CompositeDrawable
        {
            private readonly Action action;
            private readonly SpriteIcon icon;

            public ControlButton(IconUsage usage, Action action)
            {
                this.action = action;

                Size = new Vector2(32);
                InternalChild = icon = new SpriteIcon { Icon = usage, Size = new Vector2(26), Anchor = Anchor.Centre, Origin = Anchor.Centre };
            }

            protected override bool OnHover(HoverEvent e)
            {
                icon.FadeColour(new Color4(255, 200, 40, 255), 100);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e) => icon.FadeColour(Color4.White, 100);

            protected override bool OnClick(ClickEvent e)
            {
                action();
                return true;
            }
        }
    }
}
