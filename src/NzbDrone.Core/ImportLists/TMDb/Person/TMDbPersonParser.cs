using System.Collections.Generic;
using Newtonsoft.Json;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.ImportLists.ImportListGames;

namespace NzbDrone.Core.ImportLists.TMDb.Person
{
    public class TMDbPersonParser : TMDbParser
    {
        private readonly TMDbPersonSettings _settings;

        public TMDbPersonParser(TMDbPersonSettings settings)
        {
            _settings = settings;
        }

        public override IList<ImportListGame> ParseResponse(ImportListResponse importResponse)
        {
            var games = new List<ImportListGame>();

            if (!PreProcess(importResponse))
            {
                return games;
            }

            var jsonResponse = JsonConvert.DeserializeObject<PersonCreditsResource>(importResponse.Content);

            // no games were return
            if (jsonResponse == null)
            {
                return games;
            }

            var crewTypes = GetCrewDepartments();

            if (_settings.PersonCast)
            {
                foreach (var game in jsonResponse.Cast)
                {
                    // Games with no Year Fix
                    if (string.IsNullOrWhiteSpace(game.ReleaseDate))
                    {
                        continue;
                    }

                    games.AddIfNotNull(new ImportListGame { IgdbId = game.Id });
                }
            }

            if (crewTypes.Count > 0)
            {
                foreach (var game in jsonResponse.Crew)
                {
                    // Games with no Year Fix
                    if (string.IsNullOrWhiteSpace(game.ReleaseDate))
                    {
                        continue;
                    }

                    if (crewTypes.Contains(game.Department))
                    {
                        games.AddIfNotNull(new ImportListGame { IgdbId = game.Id });
                    }
                }
            }

            return games;
        }

        private List<string> GetCrewDepartments()
        {
            var creditsDepartment = new List<string>();

            if (_settings.PersonCastDirector)
            {
                creditsDepartment.Add("Directing");
            }

            if (_settings.PersonCastProducer)
            {
                creditsDepartment.Add("Production");
            }

            if (_settings.PersonCastSound)
            {
                creditsDepartment.Add("Sound");
            }

            if (_settings.PersonCastWriting)
            {
                creditsDepartment.Add("Writing");
            }

            return creditsDepartment;
        }
    }
}
