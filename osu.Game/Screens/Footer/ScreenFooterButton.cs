// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Footer
{
    public partial class ScreenFooterButton : OsuClickableContainer, IKeyBindingHandler<GlobalAction>, IScreenFooterButton
    {
        public const int CORNER_RADIUS = 10;

        public const int HEIGHT = 75;
        protected const int BUTTON_WIDTH = 116;

        /// <summary>
        /// If this footer button controls the visibility of an overlay, it will be bound to this bindable.
        /// </summary>
        public readonly Bindable<Visibility> OverlayState = new Bindable<Visibility>();

        /// <summary>
        /// Used to tell whether this button is currently on screen.
        /// <see cref="ScreenFooter"/> controls this to convey when buttons are hidden temporarily via <see cref="ScreenFooter.RegisterActiveOverlayContainer"/>.
        /// </summary>
        public readonly Bindable<Visibility> State = new Bindable<Visibility>(Visibility.Visible);

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private Colour4 buttonAccentColour;

        public Colour4 AccentColour
        {
            set
            {
                buttonAccentColour = value;
                bar.Colour = buttonAccentColour;
                icon.Colour = buttonAccentColour;
            }
        }

        public IconUsage Icon
        {
            set => icon.Icon = value;
        }

        public LocalisableString Text
        {
            get => text.Text;
            set => text.Text = value;
        }

        public new Action? Action
        {
            set
            {
                if (value == null)
                    base.Action = null;
                else
                {
                    base.Action = () =>
                    {
                        if (State.Value != Visibility.Hidden)
                            value.Invoke();
                    };
                }
            }
        }

        private readonly Container shearedContent;

        private readonly SpriteText text;
        private readonly SpriteIcon icon;

        protected Container TextContainer;
        private readonly Box bar;
        private readonly Box backgroundBox;
        private readonly Box glowBox;
        private readonly Box flashLayer;

        public readonly OverlayContainer? Overlay;

        /// <summary>
        /// Name of a legacy skin texture (e.g. <c>selection-mods</c>) to display in place of the default visuals when "Use osu!stable UI" is enabled.
        /// A matching <c>-over</c> texture is used while hovered.
        /// </summary>
        protected virtual string? LegacyTextureName => null;

        /// <summary>
        /// Whether the text content should stay visible on top of the legacy texture (for buttons that show live state).
        /// </summary>
        protected virtual bool KeepTextWithLegacyArt => false;

        private StableSkinSprite? legacyArt;
        private StableSkinSprite? legacyArtOver;

        public ScreenFooterButton(OverlayContainer? overlay = null)
        {
            Overlay = overlay;

            Size = new Vector2(BUTTON_WIDTH, HEIGHT);

            Children = new Drawable[]
            {
                shearedContent = new Container
                {
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Radius = 4,
                        // Figma says 50% opacity, but it does not match up visually if taken at face value, and looks bad.
                        Colour = Colour4.Black.Opacity(0.25f),
                        Offset = new Vector2(0, 2),
                    },
                    Shear = OsuGame.SHEAR,
                    Masking = true,
                    CornerRadius = CORNER_RADIUS,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        backgroundBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        glowBox = new Box
                        {
                            RelativeSizeAxes = Axes.Both
                        },
                        // For elements that should not be sheared.
                        new Container
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Shear = -OsuGame.SHEAR,
                            RelativeSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                TextContainer = new Container
                                {
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre,
                                    Y = 35,
                                    AutoSizeAxes = Axes.Both,
                                    Child = text = new OsuSpriteText
                                    {
                                        Font = OsuFont.TorusAlternate.With(size: 16),
                                        AlwaysPresent = true
                                    }
                                },
                                icon = new SpriteIcon
                                {
                                    Y = 10,
                                    Size = new Vector2(16),
                                    Anchor = Anchor.TopCentre,
                                    Origin = Anchor.TopCentre
                                },
                            }
                        },
                        new Container
                        {
                            Shear = -OsuGame.SHEAR,
                            Anchor = Anchor.BottomCentre,
                            Origin = Anchor.Centre,
                            Y = -CORNER_RADIUS,
                            Size = new Vector2(100, 5),
                            Masking = true,
                            CornerRadius = 3,
                            Child = bar = new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                            }
                        },
                        flashLayer = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = Colour4.White.Opacity(0.9f),
                            Blending = BlendingParameters.Additive,
                            Alpha = 0,
                        },
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (LegacyTextureName != null)
            {
                AddInternal(legacyArt = new StableSkinSprite(LegacyTextureName)
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Depth = 1,
                });
                AddInternal(legacyArtOver = new StableSkinSprite(LegacyTextureName + @"-over")
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Fit,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Depth = 1,
                });

                if (StableUi.Enabled)
                {
                    // stable draws these at native size (scaled to the window), top-aligned to the footer.
                    foreach (var art in new[] { legacyArt, legacyArtOver })
                    {
                        art.RelativeSizeAxes = Axes.None;
                        art.FillMode = FillMode.Stretch;
                        art.Anchor = Anchor.TopLeft;
                        art.Origin = Anchor.TopLeft;
                        art.Scale = new Vector2(StableUi.SCALE);
                    }
                }

                legacyArt.Active.BindValueChanged(_ => updateLegacyArt());
                legacyArtOver.Active.BindValueChanged(_ => updateLegacyArt());
            }

            if (Overlay != null)
                OverlayState.BindTo(Overlay.State);

            OverlayState.BindValueChanged(_ => UpdateDisplay());
            Enabled.BindValueChanged(_ => UpdateDisplay(), true);

            FinishTransforms(true);
        }

        // account for shear and buttons temporarily hidden with DisappearToBottom.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            // stable art is a plain rectangle, so use the button's own bounds rather than lazer's sheared hit area
            => StableUi.Enabled && legacyArt?.Active.Value == true
                ? base.ReceivePositionalInputAt(screenSpacePos)
                : shearedContent.ReceivePositionalInputAt(screenSpacePos);

        public GlobalAction? Hotkey;

        protected override bool OnClick(ClickEvent e)
        {
            if (Enabled.Value)
                Flash();

            return base.OnClick(e);
        }

        protected virtual void Flash() => flashLayer.FadeOutFromOne(800, Easing.OutQuint);

        protected override bool OnHover(HoverEvent e)
        {
            UpdateDisplay();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e) => UpdateDisplay();

        public virtual bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action != Hotkey || e.Repeat) return false;

            TriggerClick();
            return true;
        }

        public virtual void OnReleased(KeyBindingReleaseEvent<GlobalAction> e) { }

        public void UpdateDisplay()
        {
            Color4 backgroundColour = OverlayState.Value == Visibility.Visible ? buttonAccentColour : colourProvider.Background3;
            Color4 textColour = OverlayState.Value == Visibility.Visible ? colourProvider.Background6 : colourProvider.Content1;
            Color4 accentColour = OverlayState.Value == Visibility.Visible ? colourProvider.Background6 : buttonAccentColour;

            if (!Enabled.Value)
                backgroundColour = backgroundColour.Darken(1f);
            else if (IsHovered)
                backgroundColour = backgroundColour.Lighten(0.2f);

            backgroundBox.FadeColour(backgroundColour, 150, Easing.OutQuint);

            if (!Enabled.Value)
                textColour = textColour.Opacity(0.6f);

            text.FadeColour(textColour, 150, Easing.OutQuint);
            icon.FadeColour(accentColour, 150, Easing.OutQuint);
            bar.FadeColour(accentColour, 150, Easing.OutQuint);

            glowBox.FadeColour(ColourInfo.GradientVertical(buttonAccentColour.Opacity(0f), buttonAccentColour.Opacity(0.2f)), 150, Easing.OutQuint);

            updateLegacyArt();
        }

        private void updateLegacyArt()
        {
            if (legacyArt == null || legacyArtOver == null)
                return;

            bool active = legacyArt.Active.Value;

            if (StableUi.Enabled && active && legacyArt.Texture != null)
            {
                // art was switched away from relative sizing, so it needs an explicit native size (scale is applied separately)
                legacyArt.Size = new Vector2(legacyArt.Texture.DisplayWidth, legacyArt.Texture.DisplayHeight);

                if (legacyArtOver.Texture != null)
                    legacyArtOver.Size = new Vector2(legacyArtOver.Texture.DisplayWidth, legacyArtOver.Texture.DisplayHeight);

                Size = legacyArt.Size * StableUi.SCALE;
            }

            bool showOver = active && IsHovered && Enabled.Value && legacyArtOver.Active.Value;

            legacyArt.Alpha = active && !showOver ? 1 : 0;
            legacyArtOver.Alpha = showOver ? 1 : 0;

            if (StableUi.Enabled && active)
            {
                // lazer's sheared backgrounds / glow / shadow must not show through the (possibly transparent) stable art
                shearedContent.EdgeEffect = default;

                foreach (var box in shearedContent.ChildrenOfType<Box>())
                    box.Alpha = 0;
            }

            if (KeepTextWithLegacyArt)
            {
                float alpha = active ? 0 : 1;
                backgroundBox.Alpha = alpha;
                glowBox.Alpha = alpha;
                bar.Alpha = alpha;
                icon.Alpha = alpha;
                // the skin art already contains the label, so only keep extras (e.g. the mod display bar)
                text.Alpha = alpha;
            }
            else
                shearedContent.Alpha = active ? 0 : 1;
        }

        public void AppearFromLeft(double delay)
        {
            Content.FinishTransforms();
            Content.MoveToX(-300f)
                   .FadeOut()
                   .Delay(delay)
                   .MoveToX(0f, 240, Easing.OutCubic)
                   .FadeIn(240, Easing.OutCubic);
        }

        public void AppearFromBottom(double delay)
        {
            Content.FinishTransforms();
            Content.MoveToY(100f)
                   .FadeOut()
                   .Delay(delay)
                   .MoveToY(0f, 240, Easing.OutCubic)
                   .FadeIn(240, Easing.OutCubic);
        }

        public void DisappearToRight(double delay, bool expire)
        {
            Content.FinishTransforms();
            Content.Delay(delay)
                   .FadeOut(240, Easing.InOutCubic)
                   .MoveToX(300f, 360, Easing.InOutCubic);

            if (expire)
                this.Delay(Content.LatestTransformEndTime - Time.Current).Expire();
        }

        public void DisappearToBottom(double delay, bool expire)
        {
            Content.FinishTransforms();
            Content.Delay(delay)
                   .FadeOut(240, Easing.InOutCubic)
                   .MoveToY(100f, 240, Easing.InOutCubic);

            if (expire)
                this.Delay(Content.LatestTransformEndTime - Time.Current).Expire();
        }
    }
}
