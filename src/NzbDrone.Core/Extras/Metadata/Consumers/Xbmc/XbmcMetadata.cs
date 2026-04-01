using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.Languages;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Translations;
using NzbDrone.Core.Tags;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Xbmc
{
    public class XbmcMetadata : MetadataBase<XbmcMetadataSettings>
    {
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;
        private readonly IDetectXbmcNfo _detectNfo;
        private readonly IDiskProvider _diskProvider;
        private readonly ITagRepository _tagRepository;
        private readonly IGameTranslationService _gameTranslationsService;

        public XbmcMetadata(IDetectXbmcNfo detectNfo,
                            IDiskProvider diskProvider,
                            IMapCoversToLocal mediaCoverService,
                            ITagRepository tagRepository,
                            IGameTranslationService gameTranslationsService,
                            Logger logger)
        {
            _logger = logger;
            _mediaCoverService = mediaCoverService;
            _diskProvider = diskProvider;
            _detectNfo = detectNfo;
            _tagRepository = tagRepository;
            _gameTranslationsService = gameTranslationsService;
        }

        private static readonly Regex GameImagesRegex = new Regex(@"^(?<type>poster|banner|fanart|clearart|discart|keyart|landscape|logo|backdrop|clearlogo)\.(?:png|jpe?g)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex GameFileImageRegex = new Regex(@"(?<type>-thumb|-poster|-banner|-fanart|-clearart|-discart|-keyart|-landscape|-logo|-backdrop|-clearlogo)\.(?:png|jpe?g)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public override string Name => "Kodi (XBMC) / Emby";

        public override string GetFilenameAfterMove(Game game, GameFile gameFile, MetadataFile metadataFile)
        {
            var gameFilePath = Path.Combine(game.Path, gameFile.RelativePath);

            if (metadataFile.Type == MetadataType.GameMetadata)
            {
                return GetGameMetadataFilename(gameFilePath);
            }

            _logger.Debug("Unknown game file metadata: {0}", metadataFile.RelativePath);
            return Path.Combine(game.Path, metadataFile.RelativePath);
        }

        public override MetadataFile FindMetadataFile(Game game, string path)
        {
            var filename = Path.GetFileName(path);

            if (filename == null)
            {
                return null;
            }

            var metadata = new MetadataFile
            {
                GameId = game.Id,
                Consumer = GetType().Name,
                RelativePath = game.Path.GetRelativePath(path)
            };

            if (GameImagesRegex.IsMatch(filename))
            {
                metadata.Type = MetadataType.GameImage;
                return metadata;
            }

            if (GameFileImageRegex.IsMatch(filename))
            {
                metadata.Type = MetadataType.GameImage;
                return metadata;
            }

            if (filename.Equals("game.nfo", StringComparison.OrdinalIgnoreCase) &&
                _detectNfo.IsXbmcNfoFile(path))
            {
                metadata.Type = MetadataType.GameMetadata;
                return metadata;
            }

            var parseResult = Parser.Parser.ParseGameTitle(filename);

            if (parseResult != null &&
                Path.GetExtension(filename).Equals(".nfo", StringComparison.OrdinalIgnoreCase) &&
                _detectNfo.IsXbmcNfoFile(path))
            {
                metadata.Type = MetadataType.GameMetadata;
                return metadata;
            }

            return null;
        }

        public override MetadataFileResult GameMetadata(Game game, GameFile gameFile)
        {
            var xmlResult = string.Empty;

            if (Settings.GameMetadata)
            {
                _logger.Debug("Generating Game Metadata for: {0}", Path.Combine(game.Path, gameFile.RelativePath));

                var gameMetadataLanguage = Settings.GameMetadataLanguage == (int)Language.Original ?
                    (int)game.GameMetadata.Value.OriginalLanguage :
                    Settings.GameMetadataLanguage;

                var gameTranslations = _gameTranslationsService.GetAllTranslationsForGameMetadata(game.GameMetadataId);
                var selectedSettingsLanguage = Language.FindById(gameMetadataLanguage);
                var gameTranslation = gameTranslations.FirstOrDefault(mt => mt.Language == selectedSettingsLanguage);

                var thumbnail = game.GameMetadata.Value.Images.SingleOrDefault(i => i.CoverType == MediaCoverTypes.Screenshot);
                var posters = game.GameMetadata.Value.Images.Where(i => i.CoverType == MediaCoverTypes.Poster).ToList();
                var fanarts = game.GameMetadata.Value.Images.Where(i => i.CoverType == MediaCoverTypes.Fanart).ToList();

                var details = new XElement("game");

                var metadataTitle = gameTranslation?.Title ?? game.Title;

                details.Add(new XElement("title", metadataTitle));

                details.Add(new XElement("originaltitle", game.GameMetadata.Value.OriginalTitle));

                details.Add(new XElement("sorttitle", Parser.Parser.NormalizeTitle(metadataTitle)));

                if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0 || game.GameMetadata.Value.Ratings?.Metacritic?.Value > 0)
                {
                    var setRating = new XElement("ratings");

                    var defaultRatingSet = false;

                    if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0)
                    {
                        var setRateIgdb = new XElement("rating", new XAttribute("name", "igdb"), new XAttribute("max", "10"), new XAttribute("default", "true"));

                        // Convert from 0-100 scale to 0-10 for XBMC/Kodi
                        setRateIgdb.Add(new XElement("value", game.GameMetadata.Value.Ratings.Igdb.Value / 10));
                        setRateIgdb.Add(new XElement("votes", game.GameMetadata.Value.Ratings.Igdb.Votes));

                        defaultRatingSet = true;
                        setRating.Add(setRateIgdb);
                    }

                    if (game.GameMetadata.Value.Ratings?.Metacritic?.Value > 0)
                    {
                        var setRateMetacritic = new XElement("rating", new XAttribute("name", "metacritic"), new XAttribute("max", "100"));
                        setRateMetacritic.Add(new XElement("value", game.GameMetadata.Value.Ratings.Metacritic.Value));

                        if (!defaultRatingSet)
                        {
                            setRateMetacritic.SetAttributeValue("default", "true");
                        }

                        setRating.Add(setRateMetacritic);
                    }

                    details.Add(setRating);
                }

                if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0)
                {
                    // Convert from 0-100 scale to 0-10 for XBMC/Kodi
                    details.Add(new XElement("rating", game.GameMetadata.Value.Ratings.Igdb.Value / 10));
                }

                if (game.GameMetadata.Value.Ratings?.Metacritic?.Value > 0)
                {
                    details.Add(new XElement("criticrating", game.GameMetadata.Value.Ratings.Metacritic.Value));
                }

                details.Add(new XElement("userrating"));

                details.Add(new XElement("outline"));

                details.Add(new XElement("plot", gameTranslation?.Overview ?? game.GameMetadata.Value.Overview));

                details.Add(new XElement("tagline"));

                details.Add(new XElement("runtime", game.GameMetadata.Value.Runtime));

                if (thumbnail != null)
                {
                    details.Add(new XElement("thumb", thumbnail.RemoteUrl));
                }

                foreach (var poster in posters)
                {
                    if (poster != null && poster.RemoteUrl != null)
                    {
                        details.Add(new XElement("thumb", new XAttribute("aspect", "poster"), new XAttribute("preview", poster.RemoteUrl), poster.RemoteUrl));
                    }
                }

                if (fanarts.Any())
                {
                    var fanartElement = new XElement("fanart");

                    foreach (var fanart in fanarts)
                    {
                        if (fanart != null && fanart.RemoteUrl != null)
                        {
                            fanartElement.Add(new XElement("thumb", new XAttribute("preview", fanart.RemoteUrl), fanart.RemoteUrl));
                        }
                    }

                    details.Add(fanartElement);
                }

                if (game.GameMetadata.Value.Certification.IsNotNullOrWhiteSpace())
                {
                    details.Add(new XElement("mpaa", game.GameMetadata.Value.Certification));
                }

                details.Add(new XElement("id", game.IgdbId));

                var uniqueId = new XElement("uniqueid", game.IgdbId);
                uniqueId.SetAttributeValue("type", "igdb");
                uniqueId.SetAttributeValue("default", true);
                details.Add(uniqueId);

                foreach (var genre in game.GameMetadata.Value.Genres)
                {
                    details.Add(new XElement("genre", genre));
                }

                details.Add(new XElement("country"));

                if (Settings.AddCollectionName && game.GameMetadata.Value.CollectionTitle.IsNotNullOrWhiteSpace())
                {
                    var setElement = new XElement("set");

                    setElement.SetAttributeValue("igdbcolid", game.GameMetadata.Value.CollectionIgdbId);
                    setElement.Add(new XElement("name", game.GameMetadata.Value.CollectionTitle));
                    setElement.Add(new XElement("overview"));

                    details.Add(setElement);
                }

                if (game.Tags.Any())
                {
                    var tags = _tagRepository.GetTags(game.Tags);

                    foreach (var tag in tags)
                    {
                        details.Add(new XElement("tag", tag.Label));
                    }
                }

                details.Add(new XElement("status", game.GameMetadata.Value.Status));

                if (game.GameMetadata.Value.EarlyAccess.HasValue)
                {
                    details.Add(new XElement("premiered", game.GameMetadata.Value.EarlyAccess.Value.ToString("yyyy-MM-dd")));
                }

                details.Add(new XElement("year", game.Year));

                details.Add(new XElement("studio", game.GameMetadata.Value.Studio));

                details.Add(new XElement("trailer", "plugin://plugin.video.youtube/play/?video_id=" + game.GameMetadata.Value.YouTubeTrailerId));

                var doc = new XDocument(details)
                {
                    Declaration = new XDeclaration("1.0", "UTF-8", "yes"),
                };

                using var sw = new Utf8StringWriter();
                using var xw = XmlWriter.Create(sw, new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,
                    Indent = true
                });

                doc.Save(xw);
                xw.Flush();

                xmlResult += sw.ToString();
                xmlResult += Environment.NewLine;
            }

            if (Settings.GameMetadataURL)
            {
                // IGDB - Internet Game Database (primary metadata source for games)
                var igdbSlug = game.GameMetadata.Value.IgdbSlug;
                if (!string.IsNullOrEmpty(igdbSlug))
                {
                    xmlResult += "https://www.igdb.com/games/" + igdbSlug;
                    xmlResult += Environment.NewLine;
                }

                // IMDb URL removed - IMDb is a movie database, not applicable for games
            }

            var metadataFileName = GetGameMetadataFilename(gameFile.RelativePath);

            return string.IsNullOrEmpty(xmlResult) ? null : new MetadataFileResult(metadataFileName, xmlResult.Trim(Environment.NewLine.ToCharArray()));
        }

        public override List<ImageFileResult> GameImages(Game game)
        {
            if (!Settings.GameImages)
            {
                return new List<ImageFileResult>();
            }

            return ProcessGameImages(game).ToList();
        }

        private IEnumerable<ImageFileResult> ProcessGameImages(Game game)
        {
            foreach (var image in game.GameMetadata.Value.Images)
            {
                var source = _mediaCoverService.GetCoverPath(game.Id, image.CoverType);
                var destination = image.CoverType.ToString().ToLowerInvariant() + Path.GetExtension(source);

                yield return new ImageFileResult(destination, source);
            }
        }

        private string GetGameMetadataFilename(string gameFilePath)
        {
            if (Settings.UseGameNfo)
            {
                return Path.Combine(Path.GetDirectoryName(gameFilePath), "game.nfo");
            }
            else
            {
                return Path.ChangeExtension(gameFilePath, "nfo");
            }
        }
    }
}
