using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Games.Components;
using NzbDrone.Core.MediaFiles;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.GameComponents
{
    public class GameComponentResource : RestResource
    {
        public int GameId { get; set; }
        public GameComponentType ComponentType { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public bool Monitored { get; set; }
        public int ExternalId { get; set; }

        // 0 inherits the game's quality profile.
        public int QualityProfileId { get; set; }

        // Derived: whether any imported file belongs to this slot. A monitored
        // component without a file is "missing".
        public bool HasFile { get; set; }
        public long SizeOnDisk { get; set; }
    }

    public static class GameComponentResourceMapper
    {
        public static GameComponentResource ToResource(this GameComponent model, List<GameFile> gameFiles)
        {
            var files = gameFiles?.Where(f => f.ComponentId == model.Id).ToList() ?? new List<GameFile>();

            return new GameComponentResource
            {
                Id = model.Id,
                GameId = model.GameId,
                ComponentType = model.ComponentType,
                Key = model.Key,
                Title = model.Title,
                Monitored = model.Monitored,
                ExternalId = model.ExternalId,
                QualityProfileId = model.QualityProfileId,
                HasFile = files.Any(),
                SizeOnDisk = files.Sum(f => f.Size)
            };
        }
    }
}
