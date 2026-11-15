// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;

namespace osu.Game.Skinning
{
    /// <summary>
    /// A sprite showing a texture from the current skin, but only while "Use osu!stable UI" is enabled and the skin provides that texture.
    /// <see cref="Active"/> tells consumers whether a texture is currently being shown, so they can hide the default lazer visuals.
    /// </summary>
    public partial class StableSkinSprite : Sprite
    {
        /// <summary>
        /// Whether a skin texture is currently being displayed.
        /// </summary>
        public readonly BindableBool Active = new BindableBool();

        private readonly string textureName;
        private readonly Bindable<bool> useStableUI = new Bindable<bool>();

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        public StableSkinSprite(string textureName)
        {
            this.textureName = textureName;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.UseStableUI, useStableUI);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            useStableUI.BindValueChanged(_ => refresh());
            skin.SourceChanged += onSourceChanged;

            refresh();
        }

        private void onSourceChanged() => Schedule(refresh);

        private void refresh()
        {
            var texture = useStableUI.Value ? skin.GetTexture(textureName) : null;

            Texture = texture;
            Active.Value = texture != null;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIAnnotations
            if (skin != null)
                skin.SourceChanged -= onSourceChanged;
        }
    }
}
