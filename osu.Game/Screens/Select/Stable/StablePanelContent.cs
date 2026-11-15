// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// The osu!stable look of a song select carousel panel (beatmap set, difficulty, or group header).
    /// Sits on top of the regular lazer panel visuals and fully covers them.
    /// </summary>
    public partial class StablePanelContent : CompositeDrawable
    {
        private const float thumbnail_width = 164f;
        private const int star_slots = 10;

        private Box body = null!;
        private Container thumbnailContainer = null!;
        private UpdateableBeatmapBackgroundSprite thumbnail = null!;
        private OsuSpriteText titleText = null!;
        private OsuSpriteText artistText = null!;
        private OsuSpriteText difficultyText = null!;
        private FillFlowContainer starsFlow = null!;
        private OsuSpriteText groupText = null!;

        private PanelKind kind;
        private bool highlighted;
        private double stars;

        private CancellationTokenSource? starCancellation;
        private IBindable<StarDifficulty>? starBindable;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        public enum PanelKind
        {
            Set,
            Difficulty,
            Group,
        }

        public StablePanelContent()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                body = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = StableUi.SET_PANEL_COLOUR,
                },
                thumbnailContainer = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = thumbnail_width,
                    Masking = true,
                    Child = thumbnail = new UpdateableBeatmapBackgroundSprite
                    {
                        RelativeSizeAxes = Axes.Both,
                        FillMode = FillMode.Fill,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    },
                },
                new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Padding = new MarginPadding { Left = thumbnail_width + 14, Top = 6 },
                    Spacing = new Vector2(0, -2),
                    Children = new Drawable[]
                    {
                        titleText = new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(Typeface.Torus, 30, FontWeight.Regular),
                            Truncate = true,
                            RelativeSizeAxes = Axes.X,
                        },
                        artistText = new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(Typeface.Torus, 24, FontWeight.Regular),
                            Truncate = true,
                            RelativeSizeAxes = Axes.X,
                        },
                        difficultyText = new OsuSpriteText
                        {
                            Font = OsuFont.GetFont(Typeface.Torus, 26, FontWeight.Bold),
                            Truncate = true,
                            RelativeSizeAxes = Axes.X,
                        },
                        starsFlow = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(2, 0),
                            Margin = new MarginPadding { Top = 2 },
                        },
                    }
                },
                groupText = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    X = 20,
                    Font = OsuFont.GetFont(Typeface.Torus, 32, FontWeight.Bold),
                    Alpha = 0,
                },
            };

            applyKindLayout();
            applyColours();
        }

        public void SetHighlighted(bool value)
        {
            highlighted = value;

            if (IsLoaded)
                applyColours();
        }

        public void Clear()
        {
            starCancellation?.Cancel();
            starBindable = null;
            thumbnail.Beatmap.Value = null;
        }

        public void SetGroup(LocalisableString title)
        {
            kind = PanelKind.Group;
            groupText.Text = title;
            applyKindLayout();
            applyColours();
        }

        public void SetBeatmapSet(BeatmapSetInfo set)
        {
            kind = PanelKind.Set;
            starCancellation?.Cancel();

            titleText.Text = new RomanisableString(set.Metadata.TitleUnicode, set.Metadata.Title);
            artistText.Text = new RomanisableString(set.Metadata.ArtistUnicode, set.Metadata.Artist);
            thumbnail.Beatmap.Value = set.Beatmaps.Count > 0 ? set.Beatmaps[0] : null;

            applyKindLayout();
            applyColours();
        }

        public void SetBeatmap(BeatmapInfo beatmap)
        {
            kind = PanelKind.Difficulty;

            var metadata = beatmap.BeatmapSet?.Metadata ?? beatmap.Metadata;

            titleText.Text = new RomanisableString(metadata.TitleUnicode, metadata.Title);
            artistText.Text = new RomanisableString(metadata.ArtistUnicode, metadata.Artist);
            difficultyText.Text = beatmap.DifficultyName;
            thumbnail.Beatmap.Value = beatmap;

            applyKindLayout();
            applyColours();

            starCancellation?.Cancel();
            starCancellation = new CancellationTokenSource();

            stars = 0;
            rebuildStars();

            starBindable = difficultyCache.GetBindableDifficulty(beatmap, starCancellation.Token, SongSelect.DIFFICULTY_CALCULATION_DEBOUNCE);
            starBindable.BindValueChanged(s =>
            {
                stars = s.NewValue.Stars;
                rebuildStars();
            }, true);
        }

        private void applyKindLayout()
        {
            bool group = kind == PanelKind.Group;
            bool difficulty = kind == PanelKind.Difficulty;

            thumbnailContainer.Alpha = group ? 0 : 1;
            groupText.Alpha = group ? 1 : 0;
            titleText.Alpha = group ? 0 : 1;
            artistText.Alpha = group ? 0 : 1;
            difficultyText.Alpha = difficulty ? 1 : 0;
            starsFlow.Alpha = difficulty ? 1 : 0;
        }

        private void applyColours()
        {
            if (!IsLoaded)
                return;

            Color4 bodyColour;

            switch (kind)
            {
                case PanelKind.Group:
                    bodyColour = StableUi.GROUP_PANEL_COLOUR;
                    break;

                case PanelKind.Difficulty:
                    bodyColour = highlighted ? StableUi.SELECTED_PANEL_COLOUR : StableUi.DIFFICULTY_PANEL_COLOUR;
                    break;

                default:
                    bodyColour = highlighted ? StableUi.SELECTED_PANEL_COLOUR : StableUi.SET_PANEL_COLOUR;
                    break;
            }

            body.Colour = bodyColour;

            bool darkText = highlighted && kind != PanelKind.Group;
            Color4 text = darkText ? Color4.Black : Color4.White;

            // for difficulty rows, stable fades the set title/artist into the background to emphasise the difficulty name
            float setTextAlpha = kind == PanelKind.Difficulty && !darkText ? 0.35f : 1f;

            titleText.Colour = text.Opacity(setTextAlpha);
            artistText.Colour = text.Opacity(setTextAlpha);
            difficultyText.Colour = text;
            groupText.Colour = Color4.White;

            titleText.Font = OsuFont.GetFont(Typeface.Torus, 30, kind == PanelKind.Set && !darkText ? FontWeight.SemiBold : FontWeight.Regular);

            rebuildStars();
        }

        private void rebuildStars()
        {
            if (!IsLoaded)
                return;

            starsFlow.Clear();

            if (kind != PanelKind.Difficulty)
                return;

            Color4 colour = highlighted ? Color4.Black : Color4.White;
            int whole = (int)Math.Floor(stars);
            double fraction = stars - whole;

            for (int i = 0; i < star_slots; i++)
            {
                float scale;
                float alpha = 1;

                if (i < whole)
                    scale = 1f;
                else if (i == whole && fraction > 0.05)
                {
                    scale = (float)(0.35 + 0.65 * fraction);
                    alpha = 1;
                }
                else
                {
                    scale = 0.3f;
                    alpha = 0.55f;
                }

                starsFlow.Add(new Container
                {
                    Size = new Vector2(30, 28),
                    Child = new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Icon = FontAwesome.Solid.Star,
                        Size = new Vector2(26 * scale),
                        Colour = i < whole || (i == whole && fraction > 0.05) ? colour : (highlighted ? Color4.Black : DifficultyColourDim).Opacity(alpha),
                    }
                });
            }
        }

        private static readonly Color4 DifficultyColourDim = new Color4(70, 170, 255, 255);
    }
}
