using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Delay;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.DecisionEngine
{
    public class DownloadDecisionComparer : IComparer<DownloadDecision>
    {
        private readonly IConfigService _configService;
        private readonly IDelayProfileService _delayProfileService;
        private readonly IQualityDefinitionService _qualityDefinitionService;

        public delegate int CompareDelegate(DownloadDecision x, DownloadDecision y);
        public delegate int CompareDelegate<TSubject, TValue>(DownloadDecision x, DownloadDecision y);

        public DownloadDecisionComparer(IConfigService configService, IDelayProfileService delayProfileService, IQualityDefinitionService qualityDefinitionService)
        {
            _configService = configService;
            _delayProfileService = delayProfileService;
            _qualityDefinitionService = qualityDefinitionService;
        }

        public int Compare(DownloadDecision x, DownloadDecision y)
        {
            var comparers = new List<CompareDelegate>
            {
                CompareQuality,
                CompareCustomFormatScore,
                CompareContentType,
                CompareProtocol,
                CompareIndexerPriority,
                CompareIndexerFlags,
                ComparePeersIfTorrent,
                CompareAgeIfUsenet,
                CompareSize
            };

            return comparers.Select(comparer => comparer(x, y)).FirstOrDefault(result => result != 0);
        }

        private int CompareBy<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            var leftValue = funcValue(left);
            var rightValue = funcValue(right);

            return leftValue.CompareTo(rightValue);
        }

        private int CompareByReverse<TSubject, TValue>(TSubject left, TSubject right, Func<TSubject, TValue> funcValue)
            where TValue : IComparable<TValue>
        {
            return CompareBy(left, right, funcValue) * -1;
        }

        private int CompareAll(params int[] comparers)
        {
            return comparers.Select(comparer => comparer).FirstOrDefault(result => result != 0);
        }

        private int CompareIndexerPriority(DownloadDecision x, DownloadDecision y)
        {
            return CompareByReverse(x.RemoteGame.Release, y.RemoteGame.Release, release => release.IndexerPriority);
        }

        private int CompareQuality(DownloadDecision x, DownloadDecision y)
        {
            if (_configService.DownloadPropersAndRepacks == ProperDownloadTypes.DoNotPrefer)
            {
                return CompareBy(x.RemoteGame, y.RemoteGame, remoteGame => remoteGame.Game.QualityProfile.GetIndex(remoteGame.ParsedGameInfo.Quality.Quality));
            }

            return CompareAll(CompareBy(x.RemoteGame, y.RemoteGame, remoteGame => remoteGame.Game.QualityProfile.GetIndex(remoteGame.ParsedGameInfo.Quality.Quality)),
                              CompareBy(x.RemoteGame, y.RemoteGame, remoteGame => remoteGame.ParsedGameInfo.Quality.Revision));
        }

        private int CompareCustomFormatScore(DownloadDecision x, DownloadDecision y)
        {
            return CompareBy(x.RemoteGame, y.RemoteGame, remoteGame => remoteGame.CustomFormatScore);
        }

        private int CompareContentType(DownloadDecision x, DownloadDecision y)
        {
            // Prefer releases that include DLC over base game only
            // Higher score = better: BaseGameWithAllDlc (2) > BaseGame/Unknown (1) > Others (0)
            return CompareBy(x.RemoteGame, y.RemoteGame, remoteGame => ScoreContentType(remoteGame.ParsedGameInfo.ContentType));
        }

        private int ScoreContentType(ReleaseContentType contentType)
        {
            return contentType switch
            {
                ReleaseContentType.BaseGameWithAllDlc => 2,
                ReleaseContentType.BaseGame => 1,
                ReleaseContentType.Unknown => 1,
                ReleaseContentType.Expansion => 1,
                _ => 0
            };
        }

        private int CompareIndexerFlags(DownloadDecision x, DownloadDecision y)
        {
            if (!_configService.PreferIndexerFlags)
            {
                return 0;
            }

            return CompareBy(x.RemoteGame.Release, y.RemoteGame.Release, release => ScoreFlags(release.IndexerFlags));
        }

        private int CompareProtocol(DownloadDecision x, DownloadDecision y)
        {
            var result = CompareBy(x.RemoteGame, y.RemoteGame, remoteGame =>
            {
                var delayProfile = _delayProfileService.BestForTags(remoteGame.Game.Tags);
                var downloadProtocol = remoteGame.Release.DownloadProtocol;
                return downloadProtocol == delayProfile.PreferredProtocol;
            });

            return result;
        }

        private int ComparePeersIfTorrent(DownloadDecision x, DownloadDecision y)
        {
            // Different protocols should get caught when checking the preferred protocol,
            // since we're dealing with the same game in our comparisons
            if (x.RemoteGame.Release.DownloadProtocol != DownloadProtocol.Torrent ||
                y.RemoteGame.Release.DownloadProtocol != DownloadProtocol.Torrent)
            {
                return 0;
            }

            return CompareAll(
                CompareBy(x.RemoteGame, y.RemoteGame, remoteGame =>
                {
                    var seeders = TorrentInfo.GetSeeders(remoteGame.Release);

                    return seeders.HasValue && seeders.Value > 0 ? Math.Round(Math.Log10(seeders.Value)) : 0;
                }),
                CompareBy(x.RemoteGame, y.RemoteGame, remoteGame =>
                {
                    var peers = TorrentInfo.GetPeers(remoteGame.Release);

                    return peers.HasValue && peers.Value > 0 ? Math.Round(Math.Log10(peers.Value)) : 0;
                }));
        }

        private int CompareAgeIfUsenet(DownloadDecision x, DownloadDecision y)
        {
            if (x.RemoteGame.Release.DownloadProtocol != DownloadProtocol.Usenet ||
                y.RemoteGame.Release.DownloadProtocol != DownloadProtocol.Usenet)
            {
                return 0;
            }

            return CompareBy(x.RemoteGame, y.RemoteGame, remoteGame =>
            {
                var ageHours = remoteGame.Release.AgeHours;
                var age = remoteGame.Release.Age;

                if (ageHours < 1)
                {
                    return 1000;
                }

                if (ageHours <= 24)
                {
                    return 100;
                }

                if (age <= 7)
                {
                    return 10;
                }

                return Math.Round(Math.Log10(age)) * -1;
            });
        }

        private int CompareSize(DownloadDecision x, DownloadDecision y)
        {
            var sizeCompare =  CompareBy(x.RemoteGame, y.RemoteGame, remoteGame =>
            {
                var preferredSize = _qualityDefinitionService.Get(remoteGame.ParsedGameInfo.Quality.Quality).PreferredSize;

                // If no value for preferred it means unlimited so fallback to sort largest is best
                if (preferredSize.HasValue && remoteGame.Game.GameMetadata.Value.Runtime > 0)
                {
                    var preferredGameSize = remoteGame.Game.GameMetadata.Value.Runtime * preferredSize.Value.Megabytes();

                    // Calculate closest to the preferred size
                    return Math.Abs((remoteGame.Release.Size - preferredGameSize).Round(200.Megabytes())) * (-1);
                }
                else
                {
                    return remoteGame.Release.Size.Round(200.Megabytes());
                }
            });

            return sizeCompare;
        }

        private int ScoreFlags(IndexerFlags flags)
        {
            var flagValues = Enum.GetValues(typeof(IndexerFlags));

            var score = 0;

            foreach (IndexerFlags value in flagValues)
            {
                if ((flags & value) == value)
                {
                    switch (value)
                    {
                        case IndexerFlags.G_DoubleUpload:
                        case IndexerFlags.G_Freeleech:
                        case IndexerFlags.PTP_Approved:
                        case IndexerFlags.PTP_Golden:
                        case IndexerFlags.G_Internal:
                            score += 2;
                            break;
                        case IndexerFlags.G_Halfleech:
                            score += 1;
                            break;
                    }
                }
            }

            return score;
        }
    }
}
