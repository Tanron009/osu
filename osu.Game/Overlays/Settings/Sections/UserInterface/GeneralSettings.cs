// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Select.Stable;
using osu.Framework.Platform;

namespace osu.Game.Overlays.Settings.Sections.UserInterface
{
    public partial class GeneralSettings : SettingsSubsection
    {
        protected override LocalisableString Header => CommonStrings.General;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Storage storage)
        {
            string flagPath = storage.GetFullPath(StableUi.REAL_LAZER_FLAG);
            var useRealLazer = new Bindable<bool>(File.Exists(flagPath));

            useRealLazer.BindValueChanged(v =>
            {
                try
                {
                    if (v.NewValue)
                        File.WriteAllText(flagPath, "on");
                    else if (File.Exists(flagPath))
                        File.Delete(flagPath);
                }
                catch
                {
                }
            });

            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Use osu!stable UI",
                    HintText = "Uses osu!stable-style art from your skin on the main menu buttons, song select panels and footer buttons, and shows the stable-style results screen. Restart after changing.",
                    Current = config.GetBindable<bool>(OsuSetting.UseStableUI)
                })
                {
                    Keywords = [@"stable", @"legacy", @"classic", @"skin"],
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = "Use my real osu!lazer maps and settings",
                    HintText = "On the next start, copies your real osu!lazer database (maps, scores, skins, keybinds) and settings into this build once. After that this build keeps its own changes. Your real osu! folder is only ever read, never changed. Turn it off and on again (and restart) to copy again, which overwrites changes made here. Close the real osu! first.",
                    Current = useRealLazer
                })
                {
                    Keywords = [@"real", @"lazer", @"maps", @"beatmaps", @"directory", @"keybinds", @"settings"],
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = UserInterfaceStrings.CursorRotation,
                    Current = config.GetBindable<bool>(OsuSetting.CursorRotation)
                })
                {
                    Keywords = [@"spin"],
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.MenuCursorSize,
                    Current = config.GetBindable<float>(OsuSetting.MenuCursorSize),
                    KeyboardStep = 0.01f,
                    LabelFormat = v => $"{v:0.##}x"
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = UserInterfaceStrings.Parallax,
                    Current = config.GetBindable<float>(OsuSetting.MenuParallaxScale),
                    DisplayAsPercentage = true,
                    LabelFormat = v => v == 0 ? CommonStrings.Disabled : FormSliderBar<float>.DefaultLabelFormat(v, true),
                }),
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = UserInterfaceStrings.HoldToConfirmActivationTime,
                    Current = config.GetBindable<double>(OsuSetting.UIHoldActivationDelay),
                    KeyboardStep = 50,
                    LabelFormat = v => $"{v:N0} ms",
                })
                {
                    Keywords = [@"delay"],
                    ApplyClassicDefault = c => ((IHasCurrentValue<double>)c).Current.Value = 0,
                },
            };
        }
    }
}
