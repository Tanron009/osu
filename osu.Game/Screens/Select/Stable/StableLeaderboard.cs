// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.Leaderboards;
using osu.Game.Rulesets;
using osu.Game.Scoring;
using osu.Game.Screens.Play.Leaderboards;
using osu.Game.Skinning;
using osu.Game.Users.Drawables;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select.Stable
{
    /// <summary>
    /// The osu!stable-style leaderboard shown at the left of song select: a scope dropdown and a list of score rows.
    /// </summary>
    public partial class StableLeaderboard : CompositeDrawable
    {
        public const float ROW_HEIGHT = 74;

        public readonly Bindable<BeatmapLeaderboardScope> Scope = new Bindable<BeatmapLeaderboardScope>(BeatmapLeaderboardScope.Local);

        [Resolved]
        private LeaderboardManager leaderboardManager { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> beatmap { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private FillFlowContainer rows = null!;
        private OsuSpriteText statusText = null!;
        private OsuScrollContainer scroll = null!;
        private readonly IBindable<LeaderboardScores?> fetched = new Bindable<LeaderboardScores?>();
        private bool bound;

        public StableLeaderboard()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                scroll = new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ScrollbarVisible = false,
                    Child = rows = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2),
                    }
                },
                statusText = new OsuSpriteText
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                    Position = new Vector2(20, 40),
                    Font = StableUi.GetFont(30),
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Scope.BindValueChanged(_ => Refetch());
            beatmap.BindValueChanged(_ => Refetch());
            ruleset.BindValueChanged(_ => Refetch());

            Refetch();
        }

        public void Refetch()
        {
            rows.Clear();

            if (beatmap.Value.BeatmapInfo == null || beatmap.IsDefault)
            {
                statusText.Text = "No beatmap selected";
                return;
            }

            statusText.Text = "Retrieving scores...";

            leaderboardManager.FetchWithCriteria(new LeaderboardCriteria(beatmap.Value.BeatmapInfo, ruleset.Value, Scope.Value, null, LeaderboardSortMode.Score), forceRefresh: true);

            if (!bound)
            {
                bound = true;
                fetched.BindTo(leaderboardManager.Scores);
                fetched.BindValueChanged(_ => Schedule(update), true);
            }
        }

        private void update()
        {
            var scores = fetched.Value;

            if (scores == null)
                return;

            if (leaderboardManager.CurrentCriteria?.Beatmap?.Equals(beatmap.Value.BeatmapInfo) != true)
                return;

            rows.Clear();

            if (scores.FailState != null)
            {
                statusText.Text = scores.FailState switch
                {
                    LeaderboardFailState.NotLoggedIn => "Please login to view online scores",
                    LeaderboardFailState.NetworkFailure => "Offline",
                    LeaderboardFailState.BeatmapUnavailable => "Ranking is unavailable for this beatmap",
                    _ => "No scores available",
                };
                return;
            }

            if (!scores.TopScores.Any())
            {
                statusText.Text = "No records yet!";
                return;
            }

            statusText.Text = string.Empty;

            long previous = 0;
            int rank = 1;

            foreach (var s in scores.TopScores.Take(50))
            {
                rows.Add(new Row(s, rank++, previous));
                previous = s.TotalScore;
            }
        }

        private partial class Row : CompositeDrawable
        {
            private readonly ScoreInfo score;
            private readonly int rank;
            private readonly long previousScore;

            public Row(ScoreInfo score, int rank, long previousScore)
            {
                this.score = score;
                this.rank = rank;
                this.previousScore = previousScore;

                RelativeSizeAxes = Axes.X;
                Height = ROW_HEIGHT;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                string rankName = score.Rank switch
                {
                    ScoreRank.XH => "ranking-XH-small",
                    ScoreRank.X => "ranking-X-small",
                    ScoreRank.SH => "ranking-SH-small",
                    ScoreRank.S => "ranking-S-small",
                    ScoreRank.A => "ranking-A-small",
                    ScoreRank.B => "ranking-B-small",
                    ScoreRank.C => "ranking-C-small",
                    _ => "ranking-D-small",
                };

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black.Opacity(0.5f),
                        Width = 0.975f,
                    },
                    new UpdateableAvatar(score.User, false)
                    {
                        Size = new Vector2(ROW_HEIGHT - 6),
                        Position = new Vector2(3, 3),
                        Masking = false,
                    },
                    new StableSkinSprite(rankName)
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(ROW_HEIGHT + 4, 0),
                        Scale = new Vector2(1.1f),
                    },
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Position = new Vector2(ROW_HEIGHT + 62, 0),
                        Spacing = new Vector2(0, -3),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = score.User.Username,
                                Font = StableUi.GetFont(30, FontWeight.Regular),
                            },
                            new OsuSpriteText
                            {
                                Text = $"Score: {score.TotalScore:N0} ({score.MaxCombo:N0}x)",
                                Font = StableUi.GetFont(24, FontWeight.Regular),
                            },
                        }
                    },
                    new FillFlowContainer
                    {
                        Direction = FillDirection.Vertical,
                        AutoSizeAxes = Axes.Both,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -(0.025f * 560) - 12,
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Text = $"{score.Accuracy * 100:0.00}%",
                                Font = StableUi.GetFont(24, FontWeight.Regular),
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.TopRight,
                                Origin = Anchor.TopRight,
                                Text = score.PP is double pp ? $"{pp:N0}pp" : (previousScore > 0 ? $"+{previousScore - score.TotalScore:N0}" : string.Empty),
                                Font = StableUi.GetFont(22, FontWeight.Regular),
                            },
                        }
                    },
                };
            }
        }
    }
}
