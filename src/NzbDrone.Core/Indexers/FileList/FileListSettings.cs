using System;
using System.Collections.Generic;
using Equ;
using FluentValidation;
using NzbDrone.Core.Annotations;
using NzbDrone.Core.Languages;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Indexers.FileList
{
    public class FileListSettingsValidator : AbstractValidator<FileListSettings>
    {
        public FileListSettingsValidator()
        {
            RuleFor(c => c.BaseUrl).ValidRootUrl();
            RuleFor(c => c.Username).NotEmpty();
            RuleFor(c => c.Passkey).NotEmpty();

            RuleFor(c => c.Categories).NotEmpty();

            RuleFor(c => c.SeedCriteria).SetValidator(_ => new SeedCriteriaSettingsValidator());
        }
    }

    public class FileListSettings : PropertywiseEquatable<FileListSettings>, ITorrentIndexerSettings
    {
        private static readonly FileListSettingsValidator Validator = new ();

        public FileListSettings()
        {
            BaseUrl = "https://filelist.io";
            MinimumSeeders = IndexerDefaults.MINIMUM_SEEDERS;

            Categories = new[]
            {
                (int)FileListCategories.Game_SD,
                (int)FileListCategories.Game_HD,
                (int)FileListCategories.Game_HDRO,
                (int)FileListCategories.Game_4K
            };

            MultiLanguages = Array.Empty<int>();
            FailDownloads = Array.Empty<int>();
            RequiredFlags = Array.Empty<int>();
        }

        [FieldDefinition(0, Label = "Username", Privacy = PrivacyLevel.UserName)]
        public string Username { get; set; }

        [FieldDefinition(1, Label = "IndexerSettingsPasskey", Privacy = PrivacyLevel.ApiKey)]
        public string Passkey { get; set; }

        [FieldDefinition(2, Label = "IndexerSettingsApiUrl", Advanced = true, HelpText = "IndexerSettingsApiUrlHelpText")]
        public string BaseUrl { get; set; }

        [FieldDefinition(3, Label = "IndexerSettingsCategories", Type = FieldType.Select, SelectOptions = typeof(FileListCategories), HelpText = "IndexerFileListSettingsCategoriesHelpText")]
        public IEnumerable<int> Categories { get; set; }

        [FieldDefinition(4, Type = FieldType.Number, Label = "IndexerSettingsMinimumSeeders", HelpText = "IndexerSettingsMinimumSeedersHelpText", Advanced = true)]
        public int MinimumSeeders { get; set; }

        [FieldDefinition(5)]
        public SeedCriteriaSettings SeedCriteria { get; set; } = new ();

        [FieldDefinition(6, Type = FieldType.Checkbox, Label = "IndexerSettingsRejectBlocklistedTorrentHashes", HelpText = "IndexerSettingsRejectBlocklistedTorrentHashesHelpText", Advanced = true)]
        public bool RejectBlocklistedTorrentHashesWhileGrabbing { get; set; }

        [FieldDefinition(7, Type = FieldType.Select, SelectOptions = typeof(RealLanguageFieldConverter), Label = "IndexerSettingsMultiLanguageRelease", HelpText = "IndexerSettingsMultiLanguageReleaseHelpText", Advanced = true)]
        public IEnumerable<int> MultiLanguages { get; set; }

        [FieldDefinition(8, Type = FieldType.Select, SelectOptions = typeof(FailDownloads), Label = "IndexerSettingsFailDownloads", HelpText = "IndexerSettingsFailDownloadsHelpText", Advanced = true)]
        public IEnumerable<int> FailDownloads { get; set; }

        [FieldDefinition(9, Type = FieldType.Select, SelectOptions = typeof(IndexerFlags), Label = "IndexerSettingsRequiredFlags", HelpText = "IndexerSettingsRequiredFlagsHelpText", HelpLink = "https://wiki.servarr.com/gamarr/settings#indexer-flags", Advanced = true)]
        public IEnumerable<int> RequiredFlags { get; set; }

        public NzbDroneValidationResult Validate()
        {
            return new NzbDroneValidationResult(Validator.Validate(this));
        }
    }

    public enum FileListCategories
    {
        [FieldOption(Label = "Anime")]
        Anime = 24,
        [FieldOption(Label = "Animation")]
        Animation = 15,
        [FieldOption("Games SD")]
        Game_SD = 1,
        [FieldOption("Games DVD")]
        Game_DVD = 2,
        [FieldOption("Games DVD-RO")]
        Game_DVDRO = 3,
        [FieldOption("Games HD")]
        Game_HD = 4,
        [FieldOption("Games HD-RO")]
        Game_HDRO = 19,
        [FieldOption("Games 4K")]
        Game_4K = 6,
        [FieldOption("Games Blu-Ray")]
        Game_BluRay = 20,
        [FieldOption("Games 4K Blu-Ray")]
        Game_BluRay4K = 26,
        [FieldOption("Games 3D")]
        Game_3D = 25,
        [FieldOption("RO Dubbed")]
        RoDubbed = 28,
        [FieldOption("XXX")]
        Xxx = 7
    }
}
