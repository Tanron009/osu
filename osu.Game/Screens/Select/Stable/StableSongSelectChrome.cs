// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Play.Leaderboards;
using osu.Game.Screens.Select.Filter;
using osu.Game.Online;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osu.Game.Screens.Footer;
using osu.Game.Users.Drawables;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// The osu!stable song select chrome: top art + beatmap info, group/sort tabs, search label and the leaderboard.
    /// Drawn on top of the carousel, which scrolls underneath the top art like in stable.
    /// </summary>
    public partial class StableSongSelectChrome : CompositeDrawable
    {
        private const float scale = 1.40625f;
        private const float tab_strip_top = 54f;
        private const float tab_strip_left = 700f;

        private readonly FilterControl filterControl;

        private OsuSpriteText titleText = null!;
        private OsuSpriteText mapperText = null!;
        private OsuSpriteText lengthText = null!;
        private OsuSpriteText countText = null!;
        private OsuSpriteText statsText = null!;
        private OsuSpriteText searchLabel = null!;
        private OsuSpriteText searchValue = null!;
        private OsuSpriteText calculationText = null!;

        private readonly Bindable<SortMode> sortMode = new Bindable<SortMode>();
        private readonly Bindable<GroupMode> groupMode = new Bindable<GroupMode>();

        private readonly StableLeaderboard leaderboard = new StableLeaderboard();

        private CancellationTokenSource? starCancellation;
        private IBindable<StarDifficulty>? starBindable;
        private string starText = string.Empty;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        private StableSkinSprite headerArt = null!;
        private Container croppedHeader = null!;
        private StableSkinSprite modeArt = null!;
        private Drawable[] content = null!;
        private Container? footerArtLayer;
        private Container? profileStats;
        private OsuSpriteText performanceText = null!;
        private OsuSpriteText accuracyText = null!;
        private UpdateableAvatar avatar = null!;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private LocalUserStatisticsProvider statisticsProvider { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private ScreenFooter? footer { get; set; }

        public StableSongSelectChrome(FilterControl filterControl)
        {
            this.filterControl = filterControl;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.SongSelectSortingMode, sortMode);
            config.BindWith(OsuSetting.SongSelectGroupMode, groupMode);

            var colourDim = new Color4(120, 180, 230, 255);

            // skins that ship a big selection-mode image use it as full screen decoration (stable draws it bottom right, above the footer bar)
            modeArt = new StableSkinSprite(@"selection-mode")
            {
                Anchor = Anchor.BottomRight,
                Origin = Anchor.BottomRight,
                Scale = new Vector2(scale),
                Alpha = 0,
            };

            content = new Drawable[]
            {
                // stable draws this at its native size (scaled to the window), top-aligned; skins may make it taller than the default
                headerArt = new StableSkinSprite(@"songselect-top")
                {
                    Scale = new Vector2(scale),
                },
                // the same art with its baked group tab strip cut out (used whenever the skin supplies the art)
                croppedHeader = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            Masking = true,
                            Size = new Vector2(1366 * scale, tab_strip_top * scale),
                            Child = new StableSkinSprite(@"songselect-top") { Scale = new Vector2(scale) },
                        },
                        new Container
                        {
                            Masking = true,
                            Position = new Vector2(0, tab_strip_top * scale),
                            Size = new Vector2(tab_strip_left * scale, (400 - tab_strip_top) * scale),
                            Child = new StableSkinSprite(@"songselect-top") { Scale = new Vector2(scale), Y = -tab_strip_top * scale },
                        },
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Left = 10, Top = 0 },
                    Children = new Drawable[]
                    {
                        new SpriteIcon
                        {
                            Icon = FontAwesome.Solid.AngleDoubleUp,
                            Size = new Vector2(30),
                            Colour = new Color4(80, 170, 255, 255),
                            X = 4,
                            Y = 6,
                        },
                        titleText = new OsuSpriteText { X = 40, Y = -4, Font = StableUi.GetFont(34, FontWeight.Regular) },
                        mapperText = new OsuSpriteText { X = 40, Y = 28, Font = StableUi.GetFont(26, FontWeight.Regular) },
                        lengthText = new OsuSpriteText { Y = 56, Font = StableUi.GetFont(24, FontWeight.Bold) },
                        countText = new OsuSpriteText { Y = 82, Font = StableUi.GetFont(24, FontWeight.Regular) },
                        statsText = new OsuSpriteText { Y = 108, Font = StableUi.GetFont(16, FontWeight.Regular) },
                    }
                },
                calculationText = new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    X = -16,
                    Y = 2,
                    Font = StableUi.GetFont(22, FontWeight.Regular),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1243) - 0,
                    Y = 25,
                    Text = "Group",
                    Font = StableUi.GetFont(44, FontWeight.Light),
                    Colour = colourDim,
                },
                new SelectorBox<GroupMode>(groupMode, v => groupName(v))
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1523),
                    Y = 22,
                    Size = new Vector2(265, 40),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1625),
                    Y = 25,
                    Text = "Sort",
                    Font = StableUi.GetFont(44, FontWeight.Light),
                    Colour = new Color4(160, 230, 120, 255),
                },
                new SelectorBox<SortMode>(sortMode, v => $"By {v}")
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -16,
                    Y = 22,
                    Size = new Vector2(268, 40),
                },
                new FillFlowContainer
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopLeft,
                    X = -490,
                    Y = 118,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        searchLabel = new OsuSpriteText
                        {
                            Text = "Search: ",
                            Colour = new Color4(190, 255, 40, 255),
                            Font = StableUi.GetFont(30, FontWeight.Bold),
                        },
                        searchValue = new OsuSpriteText
                        {
                            Text = "Type to search!",
                            Font = StableUi.GetFont(30, FontWeight.Bold),
                        },
                    }
                },
                new Container
                {
                    Padding = new MarginPadding { Left = 14, Top = 196, Bottom = StableUi.FOOTER_PX },
                    RelativeSizeAxes = Axes.Y,
                    Width = 570,
                    Child = leaderboard
                },
                new SelectorBox<BeatmapLeaderboardScope>(leaderboard.Scope, v => $"{v} Ranking")
                {
                    X = 14,
                    Y = 182,
                    Size = new Vector2(430, 36),
                },
            };

            // everything in here uses screen pixels measured from stable at 1080p
            InternalChild = new StableSpace { Children = content };
        }

        /// <summary>
        /// Fades the parts of the stable UI that live outside of the song select screen itself (the footer bar and the art on it).
        /// </summary>
        public void Transition(bool show, bool restoreFooterAfterwards)
        {
            double duration = show ? 450 : 250;
            var easing = show ? Easing.OutQuint : Easing.OutQuad;

            // the footer itself (bar, buttons, sliding) is left to the stock footer logic; only our art on it fades.
            footerArtLayer?.FadeTo(show ? 1 : 0, duration, easing);

            if (show && InternalChild != null)
                InternalChild.FadeInFromZero(duration, easing);
        }

        private void updateProfile(APIUser user)
        {
            avatar.User = user;
            var stats = statisticsProvider.GetStatisticsFor(ruleset.Value);
            performanceText.Text = stats?.PP != null ? $"Performance:{stats.PP:N0}pp" : "Performance:0pp";
            accuracyText.Text = stats != null ? $"Accuracy:{stats.Accuracy:0.00}%" : "Accuracy:0.00%";
        }

        protected override void Update()
        {
            base.Update();

            // the skin's textures can arrive after LoadComplete, so keep the baked-art checks live
            if (content != null && headerArt != null && modeArt != null)
            {
                updateModeArt();

                if (headerArt.FromSkin)
                {
                    foreach (var d in content)
                    {
                        if (d is OsuSpriteText t && (t.Text.ToString() == "Group" || t.Text.ToString() == "Sort"))
                            t.Alpha = 0;
                    }
                }
            }
        }

        private void updateModeArt()
        {
            // skins bake their own group tabs into the top art; those buttons never worked here, so draw the art without them.
            if (headerArt != null && croppedHeader != null)
            {
                headerArt.Alpha = headerArt.FromSkin ? 0 : 1;
                croppedHeader.Alpha = headerArt.FromSkin ? 1 : 0;
            }

            bool big = modeArt.FromSkin && modeArt.Texture != null && modeArt.Texture.DisplayWidth > 600;
            modeArt.Alpha = big ? 1 : 0;
            StableUi.HideLogo = big;

            // the skin art already draws the group tabs
        }

        protected override void Dispose(bool isDisposing)
        {
            StableUi.HideLogo = false;

            base.Dispose(isDisposing);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            profileStats = new Container
            {
                Children = new Drawable[]
                {
                    // measured positions on stable's bottom bar (1080p, relative to the bar's top edge at y=966)
                    avatar = new UpdateableAvatar(isInteractive: false)
                    {
                        Size = new Vector2(56),
                        Position = new Vector2(909, 36),
                        Masking = true,
                        CornerRadius = 28,
                    },
                    performanceText = new OsuSpriteText { Position = new Vector2(980, 38), Font = StableUi.GetFont(22, FontWeight.Regular) },
                    accuracyText = new OsuSpriteText { Position = new Vector2(980, 62), Font = StableUi.GetFont(22, FontWeight.Regular) },
                }
            };

            api.LocalUser.BindValueChanged(u => updateProfile(u.NewValue), true);
            statisticsProvider.StatisticsUpdated += _ => Schedule(() => updateProfile(api.LocalUser.Value));

            if (footer != null)
            {
                footerArtLayer = footer.StableArtLayer;

                // the layer is shared between song select instances, so drop what a previous instance left behind.
                footerArtLayer.Clear(false);
                footerArtLayer.Add(modeArt);
                footerArtLayer.Add(profileStats);
                footerArtLayer.Alpha = 1;
            }
            else
                ((StableSpace)InternalChild).Add(modeArt);

            modeArt.Active.BindValueChanged(_ => updateModeArt(), true);
            headerArt.Active.BindValueChanged(_ => updateModeArt());

            // skins that ship their own songselect-top art usually bake the Group/Sort labels (and tabs) into it
            if (headerArt.FromSkin)
            {
                foreach (var d in content)
                {
                    if (d is OsuSpriteText t && (t.Text.ToString() == "Group" || t.Text.ToString() == "Sort"))
                        t.Alpha = 0;
                }
            }

            filterControl.CriteriaChanged += c => Schedule(() => onCriteria(c.SearchText));
            working.BindValueChanged(_ => updateInfo(), true);
        }

        private void onCriteria(string text)
        {
            bool empty = string.IsNullOrEmpty(text);

            searchValue.Text = empty ? "Type to search!" : text;
            searchValue.Colour = empty ? Color4.White : new Color4(255, 255, 120, 255);
        }

        private static string groupName(GroupMode mode) => mode switch
        {
            GroupMode.None => "No Grouping",
            GroupMode.Collections => "Collections",
            GroupMode.LastPlayed => "Recently Played",
            GroupMode.Artist => "By Artist",
            GroupMode.Difficulty => "By Difficulty",
            _ => $"By {mode}",
        };

        private void updateInfo()
        {
            var info = working.Value.BeatmapInfo;

            starCancellation?.Cancel();
            starBindable = null;
            starText = string.Empty;

            if (info == null || working.IsDefault)
            {
                titleText.Text = string.Empty;
                mapperText.Text = string.Empty;
                lengthText.Text = string.Empty;
                countText.Text = string.Empty;
                statsText.Text = string.Empty;
                return;
            }

            var meta = info.Metadata;
            titleText.Text = $"{meta.Artist} - {meta.Title} [{info.DifficultyName}]";
            mapperText.Text = $"Mapped by {meta.Author.Username}";

            var length = TimeSpan.FromMilliseconds(info.Length);
            lengthText.Text = $"Length: {(int)length.TotalMinutes:00}:{length.Seconds:00} BPM: {Math.Round(info.BPM)} Objects: {working.Value.Beatmap.HitObjects.Count}";

            var objects = working.Value.Beatmap.HitObjects;
            int sliders = objects.Count(h => h is IHasPath);
            int spinners = objects.Count(h => h is IHasDuration && h is not IHasPath);
            countText.Text = $"Circles: {objects.Count - sliders - spinners} Sliders: {sliders} Spinners: {spinners}";

            var d = info.Difficulty;
            updateStatsLine(d, 0);

            calculationText.Text = "Difficulty calculation in progress...";
            starCancellation = new CancellationTokenSource();
            starBindable = difficultyCache.GetBindableDifficulty(info, starCancellation.Token, SongSelect.DIFFICULTY_CALCULATION_DEBOUNCE);
            starBindable.BindValueChanged(s =>
            {
                updateStatsLine(d, s.NewValue.Stars);
                calculationText.Text = string.Empty;
            }, true);
        }

        private void updateStatsLine(BeatmapDifficulty d, double stars)
        {
            statsText.Text = $"CS:{d.CircleSize:0.#} AR:{d.ApproachRate:0.#} OD:{d.OverallDifficulty:0.#} HP:{d.DrainRate:0.#} Star Rating: {stars:0.00}★";
        }

        private partial class StableTab : Container
        {
            private readonly Action action;

            public StableTab(Action action)
            {
                this.action = action;
                Masking = true;
                CornerRadius = 3;
            }

            protected override bool OnClick(ClickEvent e)
            {
                action();
                return true;
            }
        }

        /// <summary>
        /// A stable-looking dropdown: black box with chevron, clicking opens a list of all values.
        /// </summary>
        private partial class SelectorBox<T> : CompositeDrawable
            where T : struct, Enum
        {
            private readonly Bindable<T> current;
            private readonly Func<T, string> name;
            private OsuSpriteText label = null!;
            private FillFlowContainer list = null!;
            private bool open;

            public SelectorBox(Bindable<T> current, Func<T, string> name)
            {
                this.current = current;
                this.name = name;
            }

            public override bool AcceptsFocus => open;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
                => base.ReceivePositionalInputAt(screenSpacePos) || (open && list.ReceivePositionalInputAt(screenSpacePos));

            [BackgroundDependencyLoader]
            private void load()
            {
                InternalChildren = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 5,
                        BorderThickness = 2,
                        BorderColour = new Color4(210, 210, 210, 255),
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
                            label = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                X = 10,
                                Font = StableUi.GetFont(26, FontWeight.Regular),
                            },
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                X = -10,
                                Size = new Vector2(18),
                                Icon = FontAwesome.Solid.ChevronDown,
                            },
                        }
                    },
                    list = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.TopLeft,
                        Y = 2,
                        Alpha = 0,
                        Masking = true,
                        CornerRadius = 5,
                        BorderThickness = 2,
                        BorderColour = new Color4(210, 210, 210, 255),
                    },
                };

                foreach (var value in Enum.GetValues<T>())
                    list.Add(new Row(name(value), () => select(value)));
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                current.BindValueChanged(c => label.Text = name(c.NewValue), true);
            }

            private void select(T value)
            {
                current.Value = value;
                setOpen(false);
            }

            private void setOpen(bool value)
            {
                open = value;
                list.Alpha = value ? 1 : 0;

                if (value)
                    GetContainingFocusManager()?.ChangeFocus(this);
                else if (HasFocus)
                    GetContainingFocusManager()?.ChangeFocus(null);
            }

            protected override void OnFocusLost(FocusLostEvent e)
            {
                base.OnFocusLost(e);
                open = false;
                list.Alpha = 0;
            }

            protected override bool OnClick(ClickEvent e)
            {
                setOpen(!open);
                return true;
            }

            private partial class Row : Container
            {
                private readonly Action action;
                private readonly Box hover;

                public Row(string text, Action action)
                {
                    this.action = action;

                    RelativeSizeAxes = Axes.X;
                    Height = 38;

                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
                        hover = new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(255, 102, 170, 255), Alpha = 0 },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            X = 10,
                            Text = text,
                            Font = StableUi.GetFont(24, FontWeight.Regular),
                        },
                    };
                }

                protected override bool OnHover(HoverEvent e)
                {
                    hover.FadeTo(0.8f, 60);
                    return true;
                }

                protected override void OnHoverLost(HoverLostEvent e) => hover.FadeOut(60);

                protected override bool OnClick(ClickEvent e)
                {
                    action();
                    return true;
                }
            }
        }
    }
}
