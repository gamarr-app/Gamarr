using System;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Games.Components
{
    public enum GameComponentType
    {
        Base = 1,
        Update = 2,
        Dlc = 3
    }

    /// <summary>
    /// A slot within a game's library entry (#149 phase 1): the base game, a
    /// specific update, or a DLC. Modeled on Sonarr's Episode — DLC components
    /// come from metadata (DlcReferences), update components from imports.
    /// GameFiles link back via ComponentId; a monitored component without a
    /// file is "missing" and eligible for search.
    /// </summary>
    public class GameComponent : ModelBase
    {
        public int GameId { get; set; }
        public GameComponentType ComponentType { get; set; }

        // Stable identity within (GameId, ComponentType): "base" for the base
        // game, the version string for updates ("v1.5"), the external DLC id
        // for DLC ("igdb:12345" / "steam:67890").
        public string Key { get; set; }

        public string Title { get; set; }
        public bool Monitored { get; set; }

        // External id for metadata-driven components (IGDB/Steam DLC id).
        public int ExternalId { get; set; }

        public DateTime Added { get; set; }
    }
}
