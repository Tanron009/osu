// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Select.Stable;
using osuTK.Graphics;
using System;
using osuTK;
using CommonStrings = osu.Game.Localisation.CommonStrings;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// Displays beatmap metadata inside <see cref="PlayerLoader"/>
    /// </summary>
    public partial class BeatmapMetadataDisplay : Container
    {
        private readonly IWorkingBeatmap beatmap;
        private readonly Bindable<IReadOnlyList<Mod>> mods;
        private readonly Drawable logoFacade;
        private LoadingSpinner loading;
        private Drawable blockingLoadLayer;

        public IBindable<IReadOnlyList<Mod>> Mods => mods;

        public bool Loading
        {
            set
            {
                if (value)
                    loading.Show();
                else
                    loading.Hide();

                sweepLayer?.FadeTo(value ? 1 : 0, 300);
            }
        }

        private bool userBlocked;

        public bool UserBlocked
        {
            set
            {
                if (value == userBlocked)
                    return;

                userBlocked = value;

                if (userBlocked)
                {
                    using (BeginDelayedSequence(500))
                    {
                        blockingLoadLayer
                            // Slight delay to avoid this flashing briefly during multiplayer load and other scenarios where
                            // load may be blocked for a short period.
                            .FadeIn(300, Easing.Out)
                            .Then()
                            .FadeTo(0.6f, 1000, Easing.In)
                            .Loop();
                    }
                }
                else
                    blockingLoadLayer.FadeOut(500, Easing.OutQuint);
            }
        }

        public BeatmapMetadataDisplay(IWorkingBeatmap beatmap, Bindable<IReadOnlyList<Mod>> mods, Drawable logoFacade)
        {
            this.beatmap = beatmap;
            this.logoFacade = logoFacade;

            this.mods = new Bindable<IReadOnlyList<Mod>>();
            this.mods.BindTo(mods);
        }

        private IBindable<StarDifficulty> starDifficulty;

        private FillFlowContainer versionFlow;
        private StarRatingDisplay starRatingDisplay;

        [BackgroundDependencyLoader]
        private void load(BeatmapDifficultyCache difficultyCache, OsuColour colours)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;

            AutoSizeAxes = Axes.Both;

            if (StableUi.Enabled)
            {
                loadStable(difficultyCache, metadata);
                starDifficulty = difficultyCache.GetBindableDifficulty(beatmap.BeatmapInfo);
                Loading = true;
                return;
            }

            Children = new Drawable[]
            {
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Origin = Anchor.TopCentre,
                    Anchor = Anchor.TopCentre,
                    Direction = FillDirection.Vertical,
                    Children = new[]
                    {
                        logoFacade.With(d =>
                        {
                            d.Anchor = Anchor.TopCentre;
                            d.Origin = Anchor.TopCentre;
                        }),
                        new OsuSpriteText
                        {
                            Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                            Font = OsuFont.GetFont(size: 36, italics: true),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 15 },
                        },
                        new OsuSpriteText
                        {
                            Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                            Font = OsuFont.GetFont(size: 26, italics: true),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                        },
                        new Container
                        {
                            Size = new Vector2(300, 60),
                            Margin = new MarginPadding(10),
                            Origin = Anchor.TopCentre,
                            Anchor = Anchor.TopCentre,
                            CornerRadius = 10,
                            Masking = true,
                            Children = new[]
                            {
                                new Sprite
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Texture = beatmap.GetBackground(),
                                    Origin = Anchor.Centre,
                                    Anchor = Anchor.Centre,
                                    FillMode = FillMode.Fill,
                                },
                                loading = new LoadingLayer(dimBackground: true)
                                {
                                    BlockPositionalInput = false,
                                },
                                blockingLoadLayer = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Alpha = 0,
                                    Children = new Drawable[]
                                    {
                                        new Box
                                        {
                                            Colour = colours.PinkDarker,
                                            Alpha = 0.5f,
                                            RelativeSizeAxes = Axes.Both,
                                        },
                                        new OsuSpriteText
                                        {
                                            Anchor = Anchor.Centre,
                                            Origin = Anchor.Centre,
                                            Font = OsuFont.Style.Heading2,
                                            Text = PlayerLoaderStrings.LoadingPaused
                                        }
                                    }
                                },
                            }
                        },
                        versionFlow = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(5f),
                            Margin = new MarginPadding { Bottom = 40 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = beatmap.BeatmapInfo.DifficultyName,
                                    Font = OsuFont.GetFont(size: 26, italics: true),
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                },
                                starRatingDisplay = new StarRatingDisplay(default)
                                {
                                    Alpha = 0f,
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                }
                            }
                        },
                        new GridContainer
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            AutoSizeAxes = Axes.Both,
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            ColumnDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.AutoSize),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new MetadataLineLabel(BeatmapsetsStrings.ShowInfoSource),
                                    new MetadataLineInfo(metadata.Source)
                                },
                                new Drawable[]
                                {
                                    new MetadataLineLabel(CommonStrings.Mapper),
                                    new MetadataLineInfo(metadata.Author.Username)
                                }
                            }
                        },
                        new ModDisplay
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                            Margin = new MarginPadding { Top = 20 },
                            Current = mods
                        },
                    },
                }
            };

            starDifficulty = difficultyCache.GetBindableDifficulty(beatmap.BeatmapInfo);

            Loading = true;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            starDifficulty.BindValueChanged(d =>
            {
                starRatingDisplay.Current.Value = d.NewValue;

                versionFlow.AutoSizeDuration = 300;
                versionFlow.AutoSizeEasing = Easing.OutQuint;

                starRatingDisplay.FadeIn(300, Easing.InQuint);
            }, true);
        }

        private static readonly Color4 stable_accent = new Color4(255, 102, 170, 255);

        /// <summary>
        /// The redesigned loading card used when the stable UI is enabled.
        /// </summary>
        private void loadStable(BeatmapDifficultyCache difficultyCache, IBeatmapMetadataInfo metadata)
        {
            var diff = beatmap.BeatmapInfo.Difficulty;
            string length = TimeSpan.FromMilliseconds(beatmap.BeatmapInfo.Length).ToString(@"m\:ss");

            Drawable chip(string label, string value) => new Container
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 9,
                BorderThickness = 1.5f,
                BorderColour = new Color4(255, 255, 255, 40),
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(0, 0, 0, 150) },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Padding = new MarginPadding { Horizontal = 11, Vertical = 5 },
                        Children = new Drawable[]
                        {
                            new OsuSpriteText { Text = label, Font = StableUi.GetFont(14, FontWeight.Bold), Colour = stable_accent, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                            new OsuSpriteText { Text = value, Font = StableUi.GetFont(16, FontWeight.Bold), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                        }
                    }
                }
            };

            Box sweep;

            versionFlow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(10, 0),
                Anchor = Anchor.TopRight,
                Origin = Anchor.TopRight,
                Margin = new MarginPadding(16),
                Children = new Drawable[]
                {
                    starRatingDisplay = new StarRatingDisplay(default)
                    {
                        Alpha = 0f,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                    }
                }
            };

            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 14),
                Origin = Anchor.TopCentre,
                Anchor = Anchor.TopCentre,
                Children = new Drawable[]
                {
                    logoFacade.With(d =>
                    {
                        d.Anchor = Anchor.TopCentre;
                        d.Origin = Anchor.TopCentre;
                    }),
                    new Container
                    {
                        Size = new Vector2(660, 250),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Masking = true,
                        CornerRadius = 20,
                        BorderThickness = 2.5f,
                        BorderColour = new Color4(255, 102, 170, 150),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Colour = new Color4(255, 102, 170, 70),
                            Radius = 34,
                        },
                        Children = new Drawable[]
                        {
                            new Sprite
                            {
                                RelativeSizeAxes = Axes.Both,
                                Texture = beatmap.GetBackground(),
                                Origin = Anchor.Centre,
                                Anchor = Anchor.Centre,
                                FillMode = FillMode.Fill,
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = ColourInfo.GradientVertical(new Color4(0, 0, 0, 40), new Color4(0, 0, 0, 235)),
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 7,
                                Colour = stable_accent,
                            },
                            versionFlow,
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Direction = FillDirection.Vertical,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Padding = new MarginPadding { Left = 30, Right = 24, Bottom = 22 },
                                Spacing = new Vector2(0, 2),
                                Children = new Drawable[]
                                {
                                    new TruncatingSpriteText
                                    {
                                        Text = new RomanisableString(metadata.TitleUnicode, metadata.Title),
                                        Font = StableUi.GetFont(38, FontWeight.Bold),
                                        ShowTooltip = false,
                                        RelativeSizeAxes = Axes.X,
                                    },
                                    new TruncatingSpriteText
                                    {
                                        Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist),
                                        Font = StableUi.GetFont(24, FontWeight.Regular),
                                        Colour = OsuColour.Gray(0.88f),
                                        ShowTooltip = false,
                                        RelativeSizeAxes = Axes.X,
                                    },
                                    new TruncatingSpriteText
                                    {
                                        Text = beatmap.BeatmapInfo.DifficultyName,
                                        Font = StableUi.GetFont(20, FontWeight.Bold),
                                        Colour = stable_accent,
                                        Margin = new MarginPadding { Top = 6 },
                                        ShowTooltip = false,
                                        RelativeSizeAxes = Axes.X,
                                    },
                                }
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 5,
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Masking = true,
                                Child = sweepLayer = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Child = sweep = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Width = 0.3f,
                                        RelativePositionAxes = Axes.X,
                                        X = -0.3f,
                                        Colour = ColourInfo.GradientHorizontal(new Color4(255, 102, 170, 0), stable_accent),
                                    }
                                }
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(8, 0),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Children = new[]
                        {
                            chip("LENGTH", length),
                            chip("BPM", $"{beatmap.BeatmapInfo.BPM:0.#}"),
                            chip("CS", $"{diff.CircleSize:0.#}"),
                            chip("AR", $"{diff.ApproachRate:0.#}"),
                            chip("OD", $"{diff.OverallDifficulty:0.#}"),
                            chip("HP", $"{diff.DrainRate:0.#}"),
                        }
                    },
                    new OsuSpriteText
                    {
                        Text = string.IsNullOrEmpty(metadata.Source) ? $"Mapped by {metadata.Author.Username}" : $"Mapped by {metadata.Author.Username}   •   {metadata.Source}",
                        Font = StableUi.GetFont(18, FontWeight.Regular),
                        Colour = OsuColour.Gray(0.82f),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                    },
                    new Container
                    {
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Margin = new MarginPadding { Top = 6 },
                        Children = new Drawable[]
                        {
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(10, 0),
                                Children = new Drawable[]
                                {
                                    loading = new LoadingSpinner { Size = new Vector2(24), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                                    new OsuSpriteText
                                    {
                                        Text = "Loading beatmap...",
                                        Font = StableUi.GetFont(20, FontWeight.Bold),
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                    },
                                }
                            },
                            blockingLoadLayer = new Container
                            {
                                AutoSizeAxes = Axes.Both,
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Alpha = 0,
                                Masking = true,
                                CornerRadius = 15,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(30, 8, 20, 245) },
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(10, 0),
                                        Padding = new MarginPadding { Horizontal = 20, Vertical = 9 },
                                        Children = new Drawable[]
                                        {
                                            new SpriteIcon { Icon = FontAwesome.Solid.Pause, Size = new Vector2(16), Colour = stable_accent, Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft },
                                            new OsuSpriteText
                                            {
                                                Text = "Loading paused - move your mouse away from the settings to continue",
                                                Font = StableUi.GetFont(18, FontWeight.Bold),
                                                Anchor = Anchor.CentreLeft,
                                                Origin = Anchor.CentreLeft,
                                            },
                                        }
                                    }
                                }
                            },
                        }
                    },
                    new ModDisplay
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Current = mods
                    },
                }
            };

            sweep.MoveToX(1f, 1100, Easing.InOutSine).Then().MoveToX(-0.3f).Loop();
        }

        private Container sweepLayer;

        private partial class MetadataLineLabel : OsuSpriteText
        {
            public MetadataLineLabel(LocalisableString text)
            {
                Anchor = Anchor.TopRight;
                Origin = Anchor.TopRight;
                Margin = new MarginPadding { Right = 5 };
                Colour = OsuColour.Gray(0.8f);
                Text = text;
            }
        }

        private partial class MetadataLineInfo : OsuSpriteText
        {
            public MetadataLineInfo(string text)
            {
                Margin = new MarginPadding { Left = 5 };
                Text = string.IsNullOrEmpty(text) ? @"-" : text;
            }
        }
    }
}
