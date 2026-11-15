// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Footer
{
    public partial class ScreenBackButton : ShearedButton
    {
        public const float BUTTON_WIDTH = 240;

        private StableSkinSprite legacyArt = null!;

        public sealed override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            // Ensure clicks in the corner of the screen still trigger the back button.
            // Need to apply more than 1x inflation due to shear.
            var inputRectangle = DrawRectangle.Inflate(new MarginPadding
            {
                Left = OsuGame.SCREEN_EDGE_MARGIN * 2,
                Bottom = OsuGame.SCREEN_EDGE_MARGIN * 2,
            });

            return inputRectangle.Contains(ToLocalSpace(screenSpacePos));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Width = BUTTON_WIDTH;

            ButtonContent.Child = new FillFlowContainer
            {
                X = -10f,
                RelativeSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(20f, 0f),
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(17f),
                        Icon = FontAwesome.Solid.ChevronLeft,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Font = OsuFont.TorusAlternate.With(size: 17),
                        Text = CommonStrings.Back,
                        UseFullGlyphHeight = false,
                    }
                }
            };

            AddInternal(legacyArt = new StableSkinSprite(@"menu-back")
            {
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                // stable art is bottom-left aligned and hugs the screen corner, so undo the margin the footer gives us
                Position = new Vector2(-OsuGame.SCREEN_EDGE_MARGIN, OsuGame.SCREEN_EDGE_MARGIN),
            });

            StableSkinSprite? layer = null;
            Drawable? stableLabel = null;

            if (StableUi.Enabled)
            {
                // stable's default back button is a tinted layer with an arrow and text on top; skins override it via menu-back.
                AddInternal(layer = new StableSkinSprite(@"back-button-layer")
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Position = new Vector2(-OsuGame.SCREEN_EDGE_MARGIN, OsuGame.SCREEN_EDGE_MARGIN),
                    Scale = new Vector2(StableUi.DesignScale),
                    Colour = new Color4(235, 69, 153, 255),
                });

                AddInternal(stableLabel = new FillFlowContainer
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Position = new Vector2(24, -14),
                    Direction = FillDirection.Horizontal,
                    AutoSizeAxes = Axes.Both,
                    Spacing = new Vector2(14, 0),
                    Depth = -1,
                    Children = new Drawable[]
                    {
                        new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(24), Icon = FontAwesome.Solid.ChevronLeft },
                        new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Font = StableUi.GetFont(34, FontWeight.Regular), Text = "back" },
                    }
                });
            }

            void refreshBack()
            {
                bool art = legacyArt.Active.Value;
                bool layerOn = !art && layer?.Active.Value == true;

                foreach (var d in InternalChildren)
                {
                    if (d == legacyArt || d == layer || d == stableLabel)
                        continue;

                    d.Alpha = art || layerOn ? 0 : 1;
                }

                if (layer != null) layer.Alpha = art ? 0 : 1;
                if (stableLabel != null) stableLabel.Alpha = layerOn ? 1 : 0;
            }

            legacyArt.Active.BindValueChanged(_ => refreshBack());
            layer?.Active.BindValueChanged(_ => refreshBack());
            refreshBack();

            DarkerColour = Color4Extensions.FromHex("#DE31AE");
            LighterColour = Color4Extensions.FromHex("#FF86DD");
            TextColour = Color4.White;
        }
    }
}
