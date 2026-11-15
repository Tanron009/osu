// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Game.Skinning
{
    /// <summary>
    /// Provides stable's default skin elements from the "stable-assets" folder inside the game's data folder.
    /// The assets are intentionally not part of the repository; the user supplies them from their own osu!stable install.
    /// Files are looked up as "name.png" (SD) and "name@2x.png" (HD), like in a stable skin.
    /// </summary>
    public class StableAssetStore
    {
        private readonly TextureStore sdTextures;
        private readonly TextureStore hdTextures;

        public StableAssetStore(GameHost host, Storage storage)
        {
            var folder = storage.GetStorageForDirectory("stable-assets");
            var files = new StorageBackedResourceStore(folder);

            sdTextures = new TextureStore(host.Renderer, host.CreateTextureLoaderStore(files), false, scaleAdjust: 1);
            hdTextures = new TextureStore(host.Renderer, host.CreateTextureLoaderStore(files), false, scaleAdjust: 2);
        }

        /// <summary>
        /// Fetches a stable default element, preferring the HD version.
        /// </summary>
        public Texture? Get(string name)
        {
            try
            {
                return hdTextures.Get(name + @"@2x") ?? sdTextures.Get(name);
            }
            catch
            {
                return null;
            }
        }
    }
}
