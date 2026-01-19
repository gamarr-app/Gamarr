using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Profiles.Qualities;

namespace NzbDrone.Core.Games
{
    public class Game : ModelBase
    {
        public Game()
        {
            Tags = new HashSet<int>();
            GameMetadata = new GameMetadata();
        }

        public int GameMetadataId { get; set; }

        public bool Monitored { get; set; }
        public GameStatusType MinimumAvailability { get; set; }
        public int QualityProfileId { get; set; }

        public string Path { get; set; }

        public LazyLoaded<GameMetadata> GameMetadata { get; set; }

        public string RootFolderPath { get; set; }
        public DateTime Added { get; set; }
        public QualityProfile QualityProfile { get; set; }
        public HashSet<int> Tags { get; set; }
        public AddGameOptions AddOptions { get; set; }
        public DateTime? LastSearchTime { get; set; }
        public GameFile GameFile { get; set; }
        public int GameFileId { get; set; }

        public bool HasFile => GameFileId > 0;

        // compatibility properties
        public string Title
        {
            get { return GameMetadata.Value.Title; }
            set { GameMetadata.Value.Title = value; }
        }

        public int IgdbId
        {
            get { return GameMetadata.Value.IgdbId; }
            set { GameMetadata.Value.IgdbId = value; }
        }

        public string ImdbId
        {
            get { return GameMetadata.Value.ImdbId; }
            set { GameMetadata.Value.ImdbId = value; }
        }

        public int Year
        {
            get { return GameMetadata.Value.Year; }
            set { GameMetadata.Value.Year = value; }
        }

        public string FolderName()
        {
            if (Path.IsNullOrWhiteSpace())
            {
                return "";
            }

            // Well what about Path = Null?
            // return new DirectoryInfo(Path).Name;
            return Path;
        }

        public bool IsAvailable(int delay = 0)
        {
            // the below line is what was used before delay was implemented, could still be used for cases when delay==0
            // return (Status >= MinimumAvailability || (MinimumAvailability == GameStatusType.PreDB && Status >= GameStatusType.Released));

            // This more complex sequence handles the delay
            DateTime minimumAvailabilityDate;

            if (MinimumAvailability is GameStatusType.TBA or GameStatusType.Announced)
            {
                minimumAvailabilityDate = DateTime.MinValue;
            }
            else if (MinimumAvailability == GameStatusType.EarlyAccess && GameMetadata.Value.EarlyAccess.HasValue)
            {
                minimumAvailabilityDate = GameMetadata.Value.EarlyAccess.Value;
            }
            else
            {
                if (GameMetadata.Value.PhysicalRelease.HasValue && GameMetadata.Value.DigitalRelease.HasValue)
                {
                    minimumAvailabilityDate = new DateTime(Math.Min(GameMetadata.Value.PhysicalRelease.Value.Ticks, GameMetadata.Value.DigitalRelease.Value.Ticks));
                }
                else if (GameMetadata.Value.PhysicalRelease.HasValue)
                {
                    minimumAvailabilityDate = GameMetadata.Value.PhysicalRelease.Value;
                }
                else if (GameMetadata.Value.DigitalRelease.HasValue)
                {
                    minimumAvailabilityDate = GameMetadata.Value.DigitalRelease.Value;
                }
                else
                {
                    minimumAvailabilityDate = GameMetadata.Value.EarlyAccess?.AddDays(90) ?? DateTime.MaxValue;
                }
            }

            if (minimumAvailabilityDate == DateTime.MinValue || minimumAvailabilityDate == DateTime.MaxValue)
            {
                return DateTime.UtcNow >= minimumAvailabilityDate;
            }

            return DateTime.UtcNow >= minimumAvailabilityDate.AddDays(delay);
        }

        public DateTime? GetReleaseDate()
        {
            if (MinimumAvailability is GameStatusType.TBA or GameStatusType.Announced)
            {
                return new[] { GameMetadata.Value.EarlyAccess, GameMetadata.Value.DigitalRelease, GameMetadata.Value.PhysicalRelease }
                    .Where(x => x.HasValue)
                    .Min();
            }

            if (MinimumAvailability == GameStatusType.EarlyAccess && GameMetadata.Value.EarlyAccess.HasValue)
            {
                return GameMetadata.Value.EarlyAccess.Value;
            }

            if (GameMetadata.Value.DigitalRelease.HasValue || GameMetadata.Value.PhysicalRelease.HasValue)
            {
                return new[] { GameMetadata.Value.DigitalRelease, GameMetadata.Value.PhysicalRelease }
                    .Where(x => x.HasValue)
                    .Min();
            }

            return GameMetadata.Value.EarlyAccess?.AddDays(90);
        }

        public override string ToString()
        {
            return string.Format("[{1} ({2})][{0}, {3}]", GameMetadata.Value.ImdbId, GameMetadata.Value.Title.NullSafe(), GameMetadata.Value.Year.NullSafe(), GameMetadata.Value.IgdbId);
        }

        public void ApplyChanges(Game otherGame)
        {
            Path = otherGame.Path;
            QualityProfileId = otherGame.QualityProfileId;

            Monitored = otherGame.Monitored;
            MinimumAvailability = otherGame.MinimumAvailability;

            RootFolderPath = otherGame.RootFolderPath;
            Tags = otherGame.Tags;
            AddOptions = otherGame.AddOptions;
        }
    }
}
