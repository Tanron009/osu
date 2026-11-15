// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Screens.Select.Stable;
using osu.Game.Skinning;

namespace osu.Game.Graphics.UserInterface
{
    /// <summary>
    /// Adds hover sounds to a drawable.
    /// Does not draw anything.
    /// </summary>
    public partial class HoverSounds : HoverSampleDebounceComponent
    {
        public readonly Bindable<bool> Enabled = new Bindable<bool>(true);

        private Sample sampleHover;

        [Resolved(CanBeNull = true)]
        private ISkinSource skin { get; set; }

        protected readonly HoverSampleSet SampleSet;

        public HoverSounds(HoverSampleSet sampleSet = HoverSampleSet.Default)
        {
            SampleSet = sampleSet;
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            sampleHover = audio.Samples.Get($@"UI/{SampleSet.GetResourceName()}-hover")
                          ?? audio.Samples.Get($@"UI/{HoverSampleSet.Default.GetResourceName()}-hover");
        }

        public override void PlayHoverSample()
        {
            if (!Enabled.Value)
                return;

            // osu!stable UI: use the skin's own navigation sounds when it provides them
            if (StableUi.Enabled && StableUi.PlaySkinSample(skin, StableUi.HoverSampleName))
                return;

            SamplePlaybackHelper.PlayWithRandomPitch(sampleHover, pitchVariation: 0.02);
        }
    }
}
