using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Games
{
    public interface IGameCutoffService
    {
        PagingSpec<Game> GamesWhereCutoffUnmet(PagingSpec<Game> pagingSpec);
    }

    public class GameCutoffService : IGameCutoffService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IQualityProfileService _qualityProfileService;

        public GameCutoffService(IGameRepository gameRepository, IQualityProfileService qualityProfileService, Logger logger)
        {
            _gameRepository = gameRepository;
            _qualityProfileService = qualityProfileService;
        }

        public PagingSpec<Game> GamesWhereCutoffUnmet(PagingSpec<Game> pagingSpec)
        {
            var qualitiesBelowCutoff = new List<QualitiesBelowCutoff>();
            var profiles = _qualityProfileService.All();

            // Get all items less than the cutoff
            foreach (var profile in profiles)
            {
                var cutoff = profile.UpgradeAllowed ? profile.Cutoff : profile.FirststAllowedQuality().Id;
                var cutoffIndex = profile.GetIndex(cutoff);
                var belowCutoff = profile.Items.Take(cutoffIndex.Index).ToList();

                if (belowCutoff.Any())
                {
                    qualitiesBelowCutoff.Add(new QualitiesBelowCutoff(profile.Id, belowCutoff.SelectMany(i => i.GetQualities().Select(q => q.Id))));
                }
            }

            if (qualitiesBelowCutoff.Empty())
            {
                pagingSpec.Records = new List<Game>();

                return pagingSpec;
            }

            return _gameRepository.GamesWhereCutoffUnmet(pagingSpec, qualitiesBelowCutoff);
        }
    }
}
