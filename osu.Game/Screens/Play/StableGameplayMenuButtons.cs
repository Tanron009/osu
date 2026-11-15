// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// osu!stable's pause / fail screen: skinnable <c>pause-overlay</c> backdrop and big <c>pause-continue</c> / <c>pause-retry</c> / <c>pause-back</c> images.
    /// The lazer buttons stay in the tree (hidden) and are triggered, so all existing logic keeps working.
    /// </summary>
    public partial class StableGameplayMenuButtons : Container
    {
        private readonly IReadOnlyList<DialogButton> buttons;

        public StableGameplayMenuButtons(IReadOnlyList<DialogButton> buttons)
        {
            this.buttons = buttons;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var flow = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Spacing = Vector2.Zero,
            };

            foreach (var b in buttons)
            {
                string name, click;

                if (b.Text.Equals(GameplayMenuOverlayStrings.Continue))
                {
                    name = @"pause-continue";
                    click = @"pause-continue-click";
                }
                else if (b.Text.Equals(GameplayMenuOverlayStrings.Retry))
                {
                    name = @"pause-retry";
                    click = @"pause-retry-click";
                }
                else
                {
                    name = @"pause-back";
                    click = @"pause-back-click";
                }

                flow.Add(new StableButton(name, click, b));
            }

            AddRange(new Drawable[]
            {
                new StableSkinSprite(@"pause-overlay")
                {
                    RelativeSizeAxes = Axes.Both,
                    FillMode = FillMode.Stretch,
                },
                flow,
            });
        }

        private partial class StableButton : Container
        {
            private readonly string textureName;
            private readonly string clickName;
            private readonly DialogButton target;

            private StableSkinSprite sprite = null!;

            [Resolved]
            private ISkinSource skin { get; set; } = null!;

            public StableButton(string textureName, string clickName, DialogButton target)
            {
                this.textureName = textureName;
                this.clickName = clickName;
                this.target = target;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Origin = Anchor.TopCentre;
                Anchor = Anchor.TopCentre;

                Child = sprite = new StableSkinSprite(textureName)
                {
                    Scale = new Vector2(StableUi.SCALE),
                };

                sprite.Active.BindValueChanged(_ => updateSize(), true);
            }

            protected override void Update()
            {
                base.Update();
                updateSize();
            }

            private void updateSize()
            {
                if (sprite.Texture != null)
                    Size = new Vector2(sprite.Texture.DisplayWidth, sprite.Texture.DisplayHeight) * StableUi.SCALE;
            }

            protected override bool OnHover(HoverEvent e)
            {
                // stable highlights the hovered pause button by growing it slightly
                this.ScaleTo(1.03f, 150, Easing.OutQuint);
                play(@"pause-hover");
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.ScaleTo(1f, 150, Easing.OutQuint);
            }

            protected override bool OnClick(ClickEvent e)
            {
                play(clickName);
                target.TriggerClick();
                return true;
            }

            private void play(string sample) => StableUi.PlaySkinSample(skin, sample);
        }
    }
}
