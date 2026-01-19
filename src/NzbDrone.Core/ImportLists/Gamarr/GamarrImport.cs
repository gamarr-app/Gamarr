using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Gamarr
{
    public class GamarrImport : ImportListBase<GamarrSettings>
    {
        private readonly IGamarrV3Proxy _gamarrV3Proxy;
        public override string Name => "Gamarr";
        public override bool Enabled => true;
        public override bool EnableAuto => false;

        public override ImportListType ListType => ImportListType.Program;
        public override TimeSpan MinRefreshInterval => TimeSpan.FromMinutes(15);

        public GamarrImport(IGamarrV3Proxy gamarrV3Proxy,
                            IImportListStatusService importListStatusService,
                            IConfigService configService,
                            IParsingService parsingService,
                            Logger logger)
            : base(importListStatusService, configService, parsingService, logger)
        {
            _gamarrV3Proxy = gamarrV3Proxy;
        }

        public override ImportListFetchResult Fetch()
        {
            var games = new List<ImportListGame>();
            var anyFailure = false;

            try
            {
                var remoteGames = _gamarrV3Proxy.GetGames(Settings);

                foreach (var remoteGame in remoteGames)
                {
                    if (Settings.ProfileIds.Any() && !Settings.ProfileIds.Contains(remoteGame.QualityProfileId))
                    {
                        continue;
                    }

                    if (Settings.TagIds.Any() && !Settings.TagIds.Any(tagId => remoteGame.Tags.Any(itemTagId => itemTagId == tagId)))
                    {
                        continue;
                    }

                    if (Settings.RootFolderPaths.Any() && !Settings.RootFolderPaths.Any(rootFolderPath => remoteGame.Path.ContainsIgnoreCase(rootFolderPath)))
                    {
                        continue;
                    }

                    games.Add(new ImportListGame
                    {
                        IgdbId = remoteGame.IgdbId,
                        Title = remoteGame.Title,
                        Year = remoteGame.Year
                    });
                }

                _importListStatusService.RecordSuccess(Definition.Id);
            }
            catch (Exception ex)
            {
                anyFailure = true;

                _logger.Debug(ex, "Failed to fetch data for list {0} ({1})", Definition.Name, Name);

                _importListStatusService.RecordFailure(Definition.Id);
            }

            return new ImportListFetchResult { Games = CleanupListItems(games), AnyFailure = anyFailure };
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            // Return early if there is not an API key
            if (Settings.ApiKey.IsNullOrWhiteSpace())
            {
                return new
                {
                    devices = new List<object>()
                };
            }

            Settings.Validate().Filter("ApiKey").ThrowOnError();

            if (action == "getProfiles")
            {
                var devices = _gamarrV3Proxy.GetProfiles(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Name, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Name
                        })
                };
            }

            if (action == "getTags")
            {
                var devices = _gamarrV3Proxy.GetTags(Settings);

                return new
                {
                    options = devices.OrderBy(d => d.Label, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Id,
                            Name = d.Label
                        })
                };
            }

            if (action == "getRootFolders")
            {
                var remoteRootFolders = _gamarrV3Proxy.GetRootFolders(Settings);

                return new
                {
                    options = remoteRootFolders.OrderBy(d => d.Path, StringComparer.InvariantCultureIgnoreCase)
                        .Select(d => new
                        {
                            Value = d.Path,
                            Name = d.Path
                        })
                };
            }

            return new { };
        }

        protected override void Test(List<ValidationFailure> failures)
        {
            failures.AddIfNotNull(_gamarrV3Proxy.Test(Settings));
        }
    }
}
