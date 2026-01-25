using System.Collections.Generic;
using System.Text.Json.Serialization;
using NzbDrone.Core.Messaging.Commands;

namespace NzbDrone.Core.IndexerSearch
{
    /// <summary>
    /// Radarr-compatible alias for GamesSearchCommand.
    /// Allows tools like Decluttarr to trigger searches using Radarr's API format.
    /// </summary>
    public class MoviesSearchCommand : Command
    {
        // Radarr uses movieIds, we map it to gameIds
        [JsonPropertyName("movieIds")]
        public List<int> MovieIds { get; set; }

        public override bool SendUpdatesToClient => true;
    }
}
