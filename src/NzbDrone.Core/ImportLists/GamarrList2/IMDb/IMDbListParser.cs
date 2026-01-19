using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.ImportListGames;
using NzbDrone.Core.MetadataSource.SkyHook.Resource;

namespace NzbDrone.Core.ImportLists.GamarrList2.IMDbList
{
    public class IMDbListParser : GamarrList2Parser
    {
        private readonly IMDbListSettings _settings;
        private readonly Logger _logger;

        public IMDbListParser(IMDbListSettings settings, Logger logger)
        {
            _settings = settings;
            _logger = logger;
        }

        public override IList<ImportListGame> ParseResponse(ImportListResponse importListResponse)
        {
            var importResponse = importListResponse;

            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                _logger.Debug("IMDb List {0}: Found {1} games", _settings.ListId, games.Count);
                return games;
            }

            if (_settings.ListId.StartsWith("ls", StringComparison.OrdinalIgnoreCase))
            {
                // Parse TSV response from IMDB export
                var rows = importResponse.Content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                games = rows.Skip(1).SelectList(m => m.Split(',')).Where(m => m.Length > 5).SelectList(i => new ImportListGame { ImdbId = i[1], Title = i[5] });
            }
            else
            {
                var jsonResponse = JsonConvert.DeserializeObject<List<GameResource>>(importResponse.Content);

                if (jsonResponse != null)
                {
                    games = jsonResponse.SelectList(m => new ImportListGame { IgdbId = m.IgdbId });
                }
            }

            _logger.Debug("IMDb List {0}: Found {1} games", _settings.ListId, games.Count);
            return games;
        }
    }
}
