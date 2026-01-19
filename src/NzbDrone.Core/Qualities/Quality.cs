using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Qualities
{
    public class Quality : IEmbeddedDocument, IEquatable<Quality>
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public QualitySource Source { get; set; }
        public int Resolution { get; set; }
        public Modifier Modifier { get; set; }

        public Quality()
        {
        }

        private Quality(int id, string name, QualitySource source, int resolution = 0, Modifier modifier = Modifier.NONE)
        {
            Id = id;
            Name = name;
            Source = source;
            Resolution = resolution;
            Modifier = modifier;
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public bool Equals(Quality other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as Quality);
        }

        public static bool operator ==(Quality left, Quality right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Quality left, Quality right)
        {
            return !Equals(left, right);
        }

        // Unable to determine
        public static Quality Unknown => new Quality(0, "Unknown", QualitySource.UNKNOWN);

        // Scene Releases (most common game releases)
        public static Quality Scene => new Quality(1, "Scene", QualitySource.SCENE);
        public static Quality SceneCracked => new Quality(2, "Scene Cracked", QualitySource.SCENE, 0, Modifier.CRACKED);

        // Store Rips (DRM-free or extracted from stores)
        public static Quality GOG => new Quality(3, "GOG", QualitySource.GOG, 0, Modifier.DRM_FREE);
        public static Quality Steam => new Quality(4, "Steam", QualitySource.STEAM);
        public static Quality Epic => new Quality(5, "Epic", QualitySource.EPIC);
        public static Quality Origin => new Quality(6, "Origin", QualitySource.ORIGIN);
        public static Quality Uplay => new Quality(7, "Uplay", QualitySource.UPLAY);

        // Repacks (compressed releases - FitGirl, DODI, etc.)
        public static Quality Repack => new Quality(8, "Repack", QualitySource.REPACK);
        public static Quality RepackAllDLC => new Quality(9, "Repack All DLC", QualitySource.REPACK, 0, Modifier.ALL_DLC);

        // ISO/Retail (full uncompressed or physical media)
        public static Quality ISO => new Quality(10, "ISO", QualitySource.ISO);
        public static Quality Retail => new Quality(11, "Retail", QualitySource.RETAIL);

        // Portable (no-install versions)
        public static Quality Portable => new Quality(12, "Portable", QualitySource.PORTABLE);

        // Special versions
        public static Quality Preload => new Quality(13, "Preload", QualitySource.SCENE, 0, Modifier.PRELOAD);
        public static Quality UpdateOnly => new Quality(14, "Update Only", QualitySource.SCENE, 0, Modifier.UPDATE_ONLY);
        public static Quality MultiLang => new Quality(15, "Multi-Language", QualitySource.SCENE, 0, Modifier.MULTI_LANG);

        static Quality()
        {
            All = new List<Quality>
            {
                Unknown,
                Scene,
                SceneCracked,
                GOG,
                Steam,
                Epic,
                Origin,
                Uplay,
                Repack,
                RepackAllDLC,
                ISO,
                Retail,
                Portable,
                Preload,
                UpdateOnly,
                MultiLang
            };

            AllLookup = new Quality[All.Select(v => v.Id).Max() + 1];
            foreach (var quality in All)
            {
                AllLookup[quality.Id] = quality;
            }

            DefaultQualityDefinitions = new HashSet<QualityDefinition>
            {
                new QualityDefinition(Quality.Unknown)      { Weight = 1,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Preload)      { Weight = 2,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.UpdateOnly)   { Weight = 3,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Scene)        { Weight = 4,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.SceneCracked) { Weight = 5,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Repack)       { Weight = 6,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.RepackAllDLC) { Weight = 7,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.GOG)          { Weight = 8,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Steam)        { Weight = 9,  MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Epic)         { Weight = 10, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Origin)       { Weight = 11, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Uplay)        { Weight = 12, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.ISO)          { Weight = 13, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Retail)       { Weight = 14, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.Portable)     { Weight = 15, MinSize = 0, MaxSize = null, PreferredSize = null },
                new QualityDefinition(Quality.MultiLang)    { Weight = 16, MinSize = 0, MaxSize = null, PreferredSize = null }
            };
        }

        public static readonly List<Quality> All;

        public static readonly Quality[] AllLookup;

        public static readonly HashSet<QualityDefinition> DefaultQualityDefinitions;

        public static Quality FindById(int id)
        {
            if (id == 0)
            {
                return Unknown;
            }

            var quality = AllLookup[id];

            if (quality == null)
            {
                throw new ArgumentException("ID does not match a known quality", "id");
            }

            return quality;
        }

        public static explicit operator Quality(int id)
        {
            return FindById(id);
        }

        public static explicit operator int(Quality quality)
        {
            return quality.Id;
        }
    }
}
