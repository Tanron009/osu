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

        private readonly List<(GroupMode mode, StableSkinSprite art, Box fill, OsuSpriteText text)> tabs = new List<(GroupMode, StableSkinSprite, Box, OsuSpriteText)>();

        private readonly StableLeaderboard leaderboard = new StableLeaderboard();

        private CancellationTokenSource? starCancellation;
        private IBindable<StarDifficulty>? starBindable;
        private string starText = string.Empty;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        private StableSkinSprite headerArt = null!;
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
        private Drawable tabsRoot = null!;

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
                createTabs().With(t =>
                {
                    t.Depth = 1;
                    tabsRoot = t;
                }),
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

            if (footerArtLayer != null)
                footerArtLayer.FadeTo(show ? 1 : 0, duration, easing);

            if (footer != null)
            {
                if (show)
                    footer.FadeTo(1, duration, easing);
                else if (restoreFooterAfterwards)
                    footer.FadeOut(duration, easing).Then().FadeIn();
                else
                    footer.FadeOut(duration, easing);
            }

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
            bool big = modeArt.FromSkin && modeArt.Texture != null && modeArt.Texture.DisplayWidth > 600;
            modeArt.Alpha = big ? 1 : 0;
            StableUi.HideLogo = big;

            // the skin art already draws the group tabs
            tabsRoot.Alpha = big || headerArt.FromSkin ? 0 : 1;
        }

        protected override void Dispose(bool isDisposing)
        {
            StableUi.HideLogo = false;

            if (footerArtLayer != null && modeArt != null)
            {
                footerArtLayer.Remove(modeArt, false);

                if (profileStats != null)
                    footerArtLayer.Remove(profileStats, false);
            }

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
                footerArtLayer.Add(modeArt);
                footerArtLayer.Add(profileStats);
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
            groupMode.BindValueChanged(_ => updateTabs(), true);
            working.BindValueChanged(_ => updateInfo(), true);
        }

        private void onCriteria(string text)
        {
            bool empty = string.IsNullOrEmpty(text);

            searchValue.Text = empty ? "Type to search!" : text;
            searchValue.Colour = empty ? Color4.White : new Color4(255, 255, 120, 255);
        }

        private Drawable createTabs()
        {
            var flow = new FillFlowContainer
            {
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                X = -16,
                Y = 72,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
            };

            (string, GroupMode)[] items =
            {
                ("Collections", GroupMode.Collections),
                ("Recently Played", GroupMode.LastPlayed),
                ("By Artist", GroupMode.Artist),
                ("By Difficulty", GroupMode.Difficulty),
                ("No Grouping", GroupMode.None),
            };

            foreach (var (name, mode) in items)
            {
                Box fill;
                StableSkinSprite art;
                OsuSpriteText text;

                var tab = new StableTab(() => groupMode.Value = mode)
                {
                    Size = new Vector2(171, 28),
                    Children = new Drawable[]
                    {
                        fill = new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(220, 20, 60, 255) },
                        art = new StableSkinSprite(@"selection-tab") { RelativeSizeAxes = Axes.Both, FillMode = FillMode.Stretch },
                        text = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = name,
                            Font = StableUi.GetFont(22, FontWeight.SemiBold),
                        },
                    }
                };

                art.Active.BindValueChanged(a => fill.Alpha = a.NewValue ? 0 : 1, true);
                tabs.Add((mode, art, fill, text));
                flow.Add(tab);
            }

            return flow;
        }

        private void updateTabs()
        {
            foreach (var t in tabs)
            {
                bool active = t.mode == groupMode.Value;
                var tint = active ? Color4.White : new Color4(220, 20, 60, 255);
                t.fill.Colour = tint;
                t.art.Colour = tint;
                t.text.Colour = active ? Color4.Black : Color4.White;
            }
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
