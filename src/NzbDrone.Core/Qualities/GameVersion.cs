using System;
using System.Text.RegularExpressions;

namespace NzbDrone.Core.Qualities
{
    /// <summary>
    /// Represents a game version for comparison (e.g., v1.0.1, Build 12345)
    /// </summary>
    public class GameVersion : IEquatable<GameVersion>, IComparable<GameVersion>
    {
        private static readonly Regex VersionRegex = new Regex(
            @"^v?(?<major>\d+)(?:\.(?<minor>\d+))?(?:\.(?<patch>\d+))?(?:\.(?<build>\d+))?$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex BuildRegex = new Regex(
            @"^(?:build|b)\.?(?<build>\d+)$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public GameVersion()
        {
            Major = 0;
            Minor = 0;
            Patch = 0;
            Build = 0;
        }

        public GameVersion(int major, int minor = 0, int patch = 0, int build = 0)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
            Build = build;
        }

        public int Major { get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }
        public int Build { get; set; }

        /// <summary>
        /// Returns true if this version has any meaningful value
        /// </summary>
        public bool HasValue => Major > 0 || Minor > 0 || Patch > 0 || Build > 0;

        /// <summary>
        /// Parse a version string like "v1.0.1", "1.2", "Build 12345"
        /// </summary>
        public static GameVersion Parse(string versionString)
        {
            if (string.IsNullOrWhiteSpace(versionString))
            {
                return new GameVersion();
            }

            versionString = versionString.Trim();

            // Try semantic version format first (v1.0.1, 1.2.3.4)
            var versionMatch = VersionRegex.Match(versionString);
            if (versionMatch.Success)
            {
                var major = int.Parse(versionMatch.Groups["major"].Value);
                var minor = versionMatch.Groups["minor"].Success ? int.Parse(versionMatch.Groups["minor"].Value) : 0;
                var patch = versionMatch.Groups["patch"].Success ? int.Parse(versionMatch.Groups["patch"].Value) : 0;
                var build = versionMatch.Groups["build"].Success ? int.Parse(versionMatch.Groups["build"].Value) : 0;

                return new GameVersion(major, minor, patch, build);
            }

            // Try build number format (Build 12345, B12345)
            var buildMatch = BuildRegex.Match(versionString);
            if (buildMatch.Success)
            {
                var build = int.Parse(buildMatch.Groups["build"].Value);
                return new GameVersion(0, 0, 0, build);
            }

            return new GameVersion();
        }

        /// <summary>
        /// Try to parse a version string, returns false if parsing fails
        /// </summary>
        public static bool TryParse(string versionString, out GameVersion version)
        {
            version = Parse(versionString);
            return version.HasValue;
        }

        public bool Equals(GameVersion other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            return Major == other.Major &&
                   Minor == other.Minor &&
                   Patch == other.Patch &&
                   Build == other.Build;
        }

        public int CompareTo(GameVersion other)
        {
            if (ReferenceEquals(null, other))
            {
                return HasValue ? 1 : 0;
            }

            // Compare major version
            if (Major != other.Major)
            {
                return Major.CompareTo(other.Major);
            }

            // Compare minor version
            if (Minor != other.Minor)
            {
                return Minor.CompareTo(other.Minor);
            }

            // Compare patch version
            if (Patch != other.Patch)
            {
                return Patch.CompareTo(other.Patch);
            }

            // Compare build number
            return Build.CompareTo(other.Build);
        }

        public override string ToString()
        {
            if (!HasValue)
            {
                return string.Empty;
            }

            if (Major == 0 && Minor == 0 && Patch == 0 && Build > 0)
            {
                return $"Build {Build}";
            }

            if (Patch == 0 && Build == 0)
            {
                return $"v{Major}.{Minor}";
            }

            if (Build == 0)
            {
                return $"v{Major}.{Minor}.{Patch}";
            }

            return $"v{Major}.{Minor}.{Patch}.{Build}";
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Major, Minor, Patch, Build);
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

            return Equals(obj as GameVersion);
        }

        public static bool operator ==(GameVersion left, GameVersion right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null);
            }

            return left.Equals(right);
        }

        public static bool operator !=(GameVersion left, GameVersion right)
        {
            return !(left == right);
        }

        public static bool operator >(GameVersion left, GameVersion right)
        {
            if (ReferenceEquals(left, null))
            {
                return false;
            }

            if (ReferenceEquals(right, null))
            {
                return left.HasValue;
            }

            return left.CompareTo(right) > 0;
        }

        public static bool operator <(GameVersion left, GameVersion right)
        {
            if (ReferenceEquals(left, null))
            {
                return right?.HasValue ?? false;
            }

            if (ReferenceEquals(right, null))
            {
                return false;
            }

            return left.CompareTo(right) < 0;
        }

        public static bool operator >=(GameVersion left, GameVersion right)
        {
            if (ReferenceEquals(left, null))
            {
                return ReferenceEquals(right, null) || !right.HasValue;
            }

            if (ReferenceEquals(right, null))
            {
                return true;
            }

            return left.CompareTo(right) >= 0;
        }

        public static bool operator <=(GameVersion left, GameVersion right)
        {
            if (ReferenceEquals(left, null))
            {
                return true;
            }

            if (ReferenceEquals(right, null))
            {
                return !left.HasValue;
            }

            return left.CompareTo(right) <= 0;
        }
    }
}
