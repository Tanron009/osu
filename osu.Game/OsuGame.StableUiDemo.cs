// Throwaway demo hook for headless screenshots. Not part of the real feature.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Menu;
using osu.Game.Screens.RankingV2;

namespace osu.Game
{
    public partial class OsuGame
    {
        private void runStableUiDemo()
        {
            Logger.Log("[demo] OSU_DEMO_RESULTS set, will open the V2 results screen", LoggingTarget.Runtime, LogLevel.Important);

            LocalConfig.SetValue(OsuSetting.ShowFirstRunSetup, false);

            Scheduler.AddDelayed(() =>
            {
                try
                {
                    SkinManager.CurrentSkinInfo.Value = SkinManager.DefaultClassicSkin.SkinInfo;

                    var ruleset = RulesetStore.AvailableRulesets.First();

                    var score = new ScoreInfo
                    {
                        User = new APIUser { Id = 2, Username = "peppy" },
                        BeatmapInfo = Beatmap.Value.BeatmapInfo,
                        BeatmapHash = Beatmap.Value.BeatmapInfo.Hash,
                        Ruleset = ruleset,
                        TotalScore = 1_234_567,
                        Accuracy = 0.9712,
                        MaxCombo = 456,
                        Rank = ScoreRank.A,
                        Date = DateTimeOffset.Now,
                        PP = 138.4,
                        Statistics = new Dictionary<HitResult, int>
                        {
                            [HitResult.Miss] = 2,
                            [HitResult.Meh] = 3,
                            [HitResult.Ok] = 20,
                            [HitResult.Great] = 300,
                        },
                        MaximumStatistics = new Dictionary<HitResult, int> { [HitResult.Great] = 325 },
                    };

                    for (int i = 0; i < 200; i++)
                        score.HealthGraph.Add(new HealthValue(1500 * i, 0.5 + 0.4 * Math.Sin(i / 12.0)));

                    PerformFromScreen(s => s.Push(new SoloResultsScreenV2(score)), new[] { typeof(MainMenu) });
                    Logger.Log("[demo] pushed results screen", LoggingTarget.Runtime, LogLevel.Important);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "[demo] failed to open results screen");
                }
            }, 15000);
        }
    }
}
