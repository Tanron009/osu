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

        private CancellationTokenSource? starCancellation;
        private IBindable<StarDifficulty>? starBindable;
        private string starText = string.Empty;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

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

            InternalChildren = new Drawable[]
            {
                // stable draws this at its native size (scaled to the window), top-aligned; skins may make it taller than the default
                new StableSkinSprite(@"songselect-top")
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
                        titleText = new OsuSpriteText { X = 40, Y = -4, Font = OsuFont.GetFont(Typeface.Torus, 34, FontWeight.Regular) },
                        mapperText = new OsuSpriteText { X = 40, Y = 28, Font = OsuFont.GetFont(Typeface.Torus, 26, FontWeight.Regular) },
                        lengthText = new OsuSpriteText { Y = 56, Font = OsuFont.GetFont(Typeface.Torus, 24, FontWeight.Bold) },
                        countText = new OsuSpriteText { Y = 82, Font = OsuFont.GetFont(Typeface.Torus, 24, FontWeight.Regular) },
                        statsText = new OsuSpriteText { Y = 108, Font = OsuFont.GetFont(Typeface.Torus, 16, FontWeight.Regular) },
                    }
                },
                calculationText = new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    X = -16,
                    Y = 2,
                    Font = OsuFont.GetFont(Typeface.Torus, 22, FontWeight.Regular),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1243) - 0,
                    Y = 25,
                    Text = "Group",
                    Font = OsuFont.GetFont(Typeface.Torus, 44, FontWeight.Light),
                    Colour = colourDim,
                },
                new SelectorBox<GroupMode>(groupMode, v => groupName(v))
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1523),
                    Y = 22,
                    Size = new Vector2(270, 34),
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -(1920 - 1625),
                    Y = 25,
                    Text = "Sort",
                    Font = OsuFont.GetFont(Typeface.Torus, 44, FontWeight.Light),
                    Colour = new Color4(160, 230, 120, 255),
                },
                new SelectorBox<SortMode>(sortMode, v => $"By {v}")
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.CentreRight,
                    X = -16,
                    Y = 22,
                    Size = new Vector2(268, 34),
                },
                createTabs(),
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
                            Font = OsuFont.GetFont(Typeface.Torus, 30, FontWeight.Bold),
                        },
                        searchValue = new OsuSpriteText
                        {
                            Text = "Type to search!",
                            Font = OsuFont.GetFont(Typeface.Torus, 30, FontWeight.Bold),
                        },
                    }
                },
                new Container
                {
                    Padding = new MarginPadding { Left = 14, Top = 196, Bottom = StableUi.FOOTER_HEIGHT },
                    RelativeSizeAxes = Axes.Y,
                    Width = 570,
                    Child = new StableLeaderboard()
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

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
                            Font = OsuFont.GetFont(Typeface.Torus, 22, FontWeight.SemiBold),
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
        /// A stable-looking dropdown. Clicking cycles through the available values (right click goes back).
        /// </summary>
        private partial class SelectorBox<T> : CompositeDrawable
            where T : struct, Enum
        {
            private readonly Bindable<T> current;
            private readonly Func<T, string> name;
            private OsuSpriteText label = null!;

            public SelectorBox(Bindable<T> current, Func<T, string> name)
            {
                this.current = current;
                this.name = name;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Masking = true;
                CornerRadius = 6;
                BorderThickness = 2;
                BorderColour = new Color4(210, 210, 210, 255);

                InternalChildren = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
                    label = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 8,
                        Font = OsuFont.GetFont(Typeface.Torus, 28, FontWeight.Regular),
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -10,
                        Size = new Vector2(20),
                        Icon = FontAwesome.Solid.ChevronDown,
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                current.BindValueChanged(c => label.Text = name(c.NewValue), true);
            }

            protected override bool OnClick(ClickEvent e)
            {
                step(1);
                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button == osuTK.Input.MouseButton.Right)
                {
                    step(-1);
                    return true;
                }

                return base.OnMouseDown(e);
            }

            private void step(int delta)
            {
                var values = Enum.GetValues<T>();
                int index = Array.IndexOf(values, current.Value);
                current.Value = values[(index + delta + values.Length) % values.Length];
            }
        }
    }
}
