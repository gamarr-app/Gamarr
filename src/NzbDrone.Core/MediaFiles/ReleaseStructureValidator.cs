using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Instrumentation;

namespace NzbDrone.Core.MediaFiles
{
    public interface IReleaseStructureValidator
    {
        ReleaseStructureValidationResult ValidateReleaseStructure(string path, string releaseGroup, string releaseTitle);
    }

    public class ReleaseStructureValidationResult
    {
        public bool IsValid { get; set; }
        public string DetectedGroup { get; set; }
        public string ClaimedGroup { get; set; }
        public string Message { get; set; }
        public ReleaseStructureConfidence Confidence { get; set; }
        public List<string> SuspiciousFiles { get; set; } = new List<string>();
    }

    public enum ReleaseStructureConfidence
    {
        Unknown,
        Low,
        Medium,
        High
    }

    public class ReleaseStructureValidator : IReleaseStructureValidator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(ReleaseStructureValidator));
        private readonly IDiskProvider _diskProvider;

        // Known release group patterns with their expected file structures
        private static readonly Dictionary<string, ReleaseGroupPattern> KnownGroups = new Dictionary<string, ReleaseGroupPattern>(StringComparer.OrdinalIgnoreCase)
        {
            // FitGirl Repacks
            ["FitGirl"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup.*\.exe$", @"^fg-.*\.bin$" },
                OptionalPatterns = new[] { @"^MD5$", @"perfecthash\.md5$", @"^Verify BIN files before installation\.bat$" },
                ForbiddenPatterns = new[] { @"^crack[\\/]", @"^codex\.", @"^plaza\." },
                MinBinFiles = 1,
                Description = "FitGirl Repack"
            },

            // DODI Repacks
            ["DODI"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup.*\.exe$" },
                OptionalPatterns = new[] { @"^dodi-.*\.bin$", @"DODi Repacks\.url$", @"^Data[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-.*\.bin$" },
                Description = "DODI Repack"
            },

            // CODEX
            ["CODEX"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"codex\.nfo$" },
                OptionalPatterns = new[] { @"^codex-.*\.(iso|bin|r\d{2})$", @"^Crack[\\/]", @"^crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^dodi-", @"^setup.*\.exe$" },
                Description = "CODEX Scene Release"
            },

            // PLAZA
            ["PLAZA"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"plaza\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]", @"^crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^codex\." },
                Description = "PLAZA Scene Release"
            },

            // SKIDROW
            ["SKIDROW"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"skidrow\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]", @"^crack[\\/]", @"^skidrow-.*\.(iso|bin|r\d{2})$" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "SKIDROW Scene Release"
            },

            // EMPRESS
            ["EMPRESS"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"empress\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]", @"^crack[\\/]", @"EMPRESS" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "EMPRESS Crack Release"
            },

            // GOG
            ["GOG"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup_.*\.exe$|^gog.*\.exe$" },
                OptionalPatterns = new[] { @"goggame-\d+\.info$", @"\.bin$" },
                ForbiddenPatterns = new[] { @"^crack[\\/]", @"\.nfo$" },
                Description = "GOG Installer"
            },

            // RUNE (scene group)
            ["RUNE"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"rune\.nfo$" },
                OptionalPatterns = new[] { @"^rune-.*\.(iso|bin|r\d{2})$" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "RUNE Scene Release"
            },

            // DARKSiDERS
            ["DARKSiDERS"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"darksiders\.nfo$|dks\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "DARKSiDERS Scene Release"
            },

            // TiNYiSO
            ["TiNYiSO"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"tinyiso\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "TiNYiSO Scene Release"
            },

            // CHRONOS
            ["CHRONOS"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"chronos\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "CHRONOS Scene Release"
            },

            // ElAmigos
            ["ELAMIGOS"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup.*\.exe$" },
                OptionalPatterns = new[] { @"ElAmigos", @"elamigos", @"\.bin$" },
                ForbiddenPatterns = new[] { @"^fg-", @"codex\.nfo$" },
                Description = "ElAmigos Repack"
            },

            // XATAB
            ["XATAB"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup.*\.exe$|^autorun\.exe$" },
                OptionalPatterns = new[] { @"XATAB", @"xatab" },
                ForbiddenPatterns = new[] { @"^fg-", @"codex\.nfo$" },
                Description = "XATAB Repack"
            },

            // R.G. Mechanics
            ["RG.MECHANICS"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"^setup.*\.exe$" },
                OptionalPatterns = new[] { @"R\.G\. ?Mechanics", @"rg.mechanics" },
                ForbiddenPatterns = new[] { @"^fg-", @"codex\.nfo$" },
                Description = "R.G. Mechanics Repack"
            },

            // CPY
            ["CPY"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"cpy\.nfo$" },
                OptionalPatterns = new[] { @"^cpy-.*\.(iso|bin|r\d{2})$", @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "CPY Scene Release"
            },

            // HOODLUM
            ["HOODLUM"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"hoodlum\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "HOODLUM Scene Release"
            },

            // RAZOR1911
            ["RAZOR1911"] = new ReleaseGroupPattern
            {
                RequiredPatterns = new[] { @"razor1911\.nfo$|rzr\.nfo$" },
                OptionalPatterns = new[] { @"^Crack[\\/]" },
                ForbiddenPatterns = new[] { @"^fg-", @"^setup.*\.exe$" },
                Description = "RAZOR1911 Scene Release"
            },
        };

        // Suspicious file patterns - only checked for releases that don't match known group patterns
        private static readonly string[] SuspiciousPatterns = new[]
        {
            @"\.scr$",           // Screen saver (no legitimate game use)
            @"\.pif$",           // Program Information File (obsolete, can execute code)
            @"\.hta$",           // HTML Application (can run arbitrary code)
            @"\.cpl$",           // Control Panel extension
            @"readme.*\.exe$",   // Readme as executable is always suspicious
        };

        public ReleaseStructureValidator(IDiskProvider diskProvider)
        {
            _diskProvider = diskProvider;
        }

        public ReleaseStructureValidationResult ValidateReleaseStructure(string path, string releaseGroup, string releaseTitle)
        {
            var result = new ReleaseStructureValidationResult
            {
                ClaimedGroup = releaseGroup,
                IsValid = true,
                Confidence = ReleaseStructureConfidence.Unknown
            };

            if (!_diskProvider.FolderExists(path))
            {
                result.Message = "Path does not exist";
                result.IsValid = false;
                return result;
            }

            var files = GetAllFiles(path);
            var relativeFiles = files.Select(f => GetRelativePath(path, f)).ToList();

            // Try to detect the actual release group from file structure
            result.DetectedGroup = DetectReleaseGroup(relativeFiles, releaseTitle);

            // If we detected a known group structure, trust it
            if (result.DetectedGroup != null)
            {
                result.IsValid = true;
                result.Confidence = ReleaseStructureConfidence.High;
                result.Message = $"Release structure matches known {result.DetectedGroup} pattern";

                // Only concern: if it claims to be a different group than detected
                if (!string.IsNullOrWhiteSpace(releaseGroup))
                {
                    var normalizedClaimed = NormalizeGroupName(releaseGroup);
                    var normalizedDetected = NormalizeGroupName(result.DetectedGroup);

                    if (!string.Equals(normalizedDetected, normalizedClaimed, StringComparison.OrdinalIgnoreCase))
                    {
                        // Claims to be one group but matches another - suspicious
                        result.IsValid = false;
                        result.Message = $"Claimed group '{releaseGroup}' but structure matches '{result.DetectedGroup}'";
                        Logger.Warn("Release claims to be {0} but structure matches {1}", releaseGroup, result.DetectedGroup);
                    }
                }

                return result;
            }

            // No known group detected - check if it claims to be from a known group
            if (!string.IsNullOrWhiteSpace(releaseGroup))
            {
                var normalizedClaimed = NormalizeGroupName(releaseGroup);

                if (KnownGroups.TryGetValue(normalizedClaimed, out var pattern))
                {
                    // Claims to be from a known group but doesn't match the structure
                    var validationResult = ValidateAgainstPattern(relativeFiles, pattern, normalizedClaimed);
                    result.IsValid = validationResult.isValid;
                    result.Message = validationResult.message;
                    result.Confidence = validationResult.isValid ? ReleaseStructureConfidence.High : ReleaseStructureConfidence.Low;

                    if (!validationResult.isValid)
                    {
                        Logger.Warn("Release claims to be {0} but doesn't match expected structure: {1}", releaseGroup, validationResult.message);
                    }

                    return result;
                }
            }

            // Unknown release group - check for suspicious files since we can't verify by structure
            result.SuspiciousFiles = FindSuspiciousFiles(relativeFiles);

            if (result.SuspiciousFiles.Any())
            {
                result.IsValid = false;
                result.Confidence = ReleaseStructureConfidence.Low;
                result.Message = $"Unknown release with suspicious files: {string.Join(", ", result.SuspiciousFiles.Take(5))}";
                Logger.Warn("Unknown release contains suspicious files: {0}", string.Join(", ", result.SuspiciousFiles));
                return result;
            }

            // No suspicious files - allow with low confidence
            result.IsValid = true;
            result.Confidence = ReleaseStructureConfidence.Low;
            result.Message = string.IsNullOrWhiteSpace(releaseGroup)
                ? "Unknown release structure (no group detected)"
                : $"Unknown release group '{releaseGroup}' (cannot verify structure)";

            return result;
        }

        private List<string> FindSuspiciousFiles(List<string> files)
        {
            var suspicious = new List<string>();

            foreach (var file in files)
            {
                foreach (var pattern in SuspiciousPatterns)
                {
                    if (Regex.IsMatch(file, pattern, RegexOptions.IgnoreCase))
                    {
                        suspicious.Add(file);
                        break;
                    }
                }
            }

            return suspicious;
        }

        private List<string> GetAllFiles(string path)
        {
            try
            {
                return _diskProvider.GetFiles(path, true).ToList();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Failed to enumerate files in {0}", path);
                return new List<string>();
            }
        }

        private string GetRelativePath(string basePath, string fullPath)
        {
            if (fullPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
            {
                var relative = fullPath.Substring(basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return relative;
            }

            return fullPath;
        }

        private string DetectReleaseGroup(List<string> files, string releaseTitle)
        {
            // Check each known group's required patterns
            foreach (var kvp in KnownGroups)
            {
                var pattern = kvp.Value;
                var hasAllRequired = pattern.RequiredPatterns.All(p =>
                    files.Any(f => Regex.IsMatch(f, p, RegexOptions.IgnoreCase)));

                if (hasAllRequired)
                {
                    // Verify no forbidden patterns
                    var hasForbidden = pattern.ForbiddenPatterns?.Any(p =>
                        files.Any(f => Regex.IsMatch(f, p, RegexOptions.IgnoreCase))) ?? false;

                    if (!hasForbidden)
                    {
                        return kvp.Key;
                    }
                }
            }

            // Also check the release title for group indicators
            if (!string.IsNullOrEmpty(releaseTitle))
            {
                foreach (var kvp in KnownGroups)
                {
                    if (releaseTitle.IndexOf(kvp.Key, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        // Found group name in title, but structure didn't match
                        // This is suspicious but not definitive
                        return null;
                    }
                }
            }

            return null;
        }

        private (bool isValid, string message) ValidateAgainstPattern(List<string> files, ReleaseGroupPattern pattern, string groupName)
        {
            // Check required patterns
            var missingRequired = new List<string>();
            foreach (var requiredPattern in pattern.RequiredPatterns)
            {
                if (!files.Any(f => Regex.IsMatch(f, requiredPattern, RegexOptions.IgnoreCase)))
                {
                    missingRequired.Add(requiredPattern);
                }
            }

            if (missingRequired.Any())
            {
                return (false, $"Missing expected files for {groupName}: structure does not match known {pattern.Description} pattern");
            }

            // Check forbidden patterns
            if (pattern.ForbiddenPatterns != null)
            {
                foreach (var forbiddenPattern in pattern.ForbiddenPatterns)
                {
                    var matchedForbidden = files.FirstOrDefault(f => Regex.IsMatch(f, forbiddenPattern, RegexOptions.IgnoreCase));
                    if (matchedForbidden != null)
                    {
                        return (false, $"Found unexpected file '{matchedForbidden}' that should not be in a {pattern.Description}");
                    }
                }
            }

            // Check minimum bin files if specified
            if (pattern.MinBinFiles > 0)
            {
                var binCount = files.Count(f => f.EndsWith(".bin", StringComparison.OrdinalIgnoreCase));
                if (binCount < pattern.MinBinFiles)
                {
                    return (false, $"Expected at least {pattern.MinBinFiles} .bin files for {pattern.Description}, found {binCount}");
                }
            }

            return (true, $"Release structure matches expected {pattern.Description} pattern");
        }

        private string NormalizeGroupName(string groupName)
        {
            if (string.IsNullOrEmpty(groupName))
            {
                return groupName;
            }

            // Remove common prefixes/suffixes and normalize
            var normalized = groupName
                .Replace(".", "")
                .Replace("-", "")
                .Replace("_", "")
                .Replace(" ", "")
                .ToUpperInvariant();

            // Map common variations
            return normalized switch
            {
                "FITGIRLREPACK" or "FITGIRLREPACKS" => "FITGIRL",
                "RGMECHANICS" => "RG.MECHANICS",
                "DARKSIDERS" => "DARKSiDERS",
                "TINYISO" => "TiNYiSO",
                _ => groupName // Return original if no mapping
            };
        }
    }

    public class ReleaseGroupPattern
    {
        public string[] RequiredPatterns { get; set; } = Array.Empty<string>();
        public string[] OptionalPatterns { get; set; } = Array.Empty<string>();
        public string[] ForbiddenPatterns { get; set; } = Array.Empty<string>();
        public int MinBinFiles { get; set; }
        public string Description { get; set; }
    }
}
