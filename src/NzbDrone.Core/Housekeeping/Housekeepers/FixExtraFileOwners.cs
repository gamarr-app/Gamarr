using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Extras.Files;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Extras.Others;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    /// <summary>
    /// Re-attributes extra files that the existing-extra importers stamped with the
    /// game's primary GameFileId although they sit inside an Updates/ or DLC/ unit.
    /// New imports go through ExtraFileOwner already; this heals rows written before.
    /// Only moves a row onto a component that actually contains it, so rows that are
    /// already correct (or belong to the base) are never touched.
    /// </summary>
    public class FixExtraFileOwners : IHousekeepingTask
    {
        private readonly IMediaFileRepository _mediaFileRepository;
        private readonly IExtraFileRepository<MetadataFile> _metadataRepository;
        private readonly IExtraFileRepository<OtherExtraFile> _otherRepository;
        private readonly Logger _logger;

        public FixExtraFileOwners(IMediaFileRepository mediaFileRepository,
                                  IExtraFileRepository<MetadataFile> metadataRepository,
                                  IExtraFileRepository<OtherExtraFile> otherRepository,
                                  Logger logger)
        {
            _mediaFileRepository = mediaFileRepository;
            _metadataRepository = metadataRepository;
            _otherRepository = otherRepository;
            _logger = logger;
        }

        public void Clean()
        {
            var componentsByGame = _mediaFileRepository.All()
                .Where(f => GameFileUnit.IsUpdate(f) || GameFileUnit.IsDlc(f))
                .GroupBy(f => f.GameId)
                .ToDictionary(g => g.Key, g => g.ToList());

            if (componentsByGame.Count == 0)
            {
                return;
            }

            Fix(_metadataRepository, componentsByGame);
            Fix(_otherRepository, componentsByGame);
        }

        private void Fix<TExtraFile>(IExtraFileRepository<TExtraFile> repository, Dictionary<int, List<GameFile>> componentsByGame)
            where TExtraFile : ExtraFile, new()
        {
            var changed = new List<TExtraFile>();

            foreach (var extra in repository.All().Where(e => componentsByGame.ContainsKey(e.GameId)))
            {
                var owner = ExtraFileOwner.FindComponentOwner(componentsByGame[extra.GameId], extra.RelativePath);

                if (owner != null && extra.GameFileId != owner.Id)
                {
                    extra.GameFileId = owner.Id;
                    changed.Add(extra);
                }
            }

            if (changed.Any())
            {
                _logger.Info("Re-attributed {0} {1} row(s) to the update/DLC unit that contains them", changed.Count, typeof(TExtraFile).Name);
                repository.UpdateMany(changed);
            }
        }
    }
}
