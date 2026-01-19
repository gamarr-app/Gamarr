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
using NzbDrone.Core.MediaFiles.MediaInfo;
using NzbDrone.Core.Games;
using NzbDrone.Core.Games.Credits;
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
        private readonly ICreditService _creditService;
        private readonly ITagRepository _tagRepository;
        private readonly IGameTranslationService _gameTranslationsService;

        public XbmcMetadata(IDetectXbmcNfo detectNfo,
                            IDiskProvider diskProvider,
                            IMapCoversToLocal mediaCoverService,
                            ICreditService creditService,
                            ITagRepository tagRepository,
                            IGameTranslationService gameTranslationsService,
                            Logger logger)
        {
            _logger = logger;
            _mediaCoverService = mediaCoverService;
            _diskProvider = diskProvider;
            _detectNfo = detectNfo;
            _creditService = creditService;
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

                var credits = _creditService.GetAllCreditsForGameMetadata(game.GameMetadataId);

                var watched = GetExistingWatchedStatus(game, gameFile.RelativePath);

                var thumbnail = game.GameMetadata.Value.Images.SingleOrDefault(i => i.CoverType == MediaCoverTypes.Screenshot);
                var posters = game.GameMetadata.Value.Images.Where(i => i.CoverType == MediaCoverTypes.Poster).ToList();
                var fanarts = game.GameMetadata.Value.Images.Where(i => i.CoverType == MediaCoverTypes.Fanart).ToList();

                var details = new XElement("game");

                var metadataTitle = gameTranslation?.Title ?? game.Title;

                details.Add(new XElement("title", metadataTitle));

                details.Add(new XElement("originaltitle", game.GameMetadata.Value.OriginalTitle));

                details.Add(new XElement("sorttitle", Parser.Parser.NormalizeTitle(metadataTitle)));

                if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0 || game.GameMetadata.Value.Ratings?.Imdb?.Votes > 0 || game.GameMetadata.Value.Ratings?.RottenTomatoes?.Value > 0)
                {
                    var setRating = new XElement("ratings");

                    var defaultRatingSet = false;

                    if (game.GameMetadata.Value.Ratings?.Imdb?.Votes > 0)
                    {
                        var setRateImdb = new XElement("rating", new XAttribute("name", "imdb"), new XAttribute("max", "10"), new XAttribute("default", "true"));
                        setRateImdb.Add(new XElement("value", game.GameMetadata.Value.Ratings.Imdb.Value));
                        setRateImdb.Add(new XElement("votes", game.GameMetadata.Value.Ratings.Imdb.Votes));

                        defaultRatingSet = true;
                        setRating.Add(setRateImdb);
                    }

                    if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0)
                    {
                        var setRateTheGameDb = new XElement("rating", new XAttribute("name", "thegamedb"), new XAttribute("max", "10"));
                        setRateTheGameDb.Add(new XElement("value", game.GameMetadata.Value.Ratings.Igdb.Value));
                        setRateTheGameDb.Add(new XElement("votes", game.GameMetadata.Value.Ratings.Igdb.Votes));

                        if (!defaultRatingSet)
                        {
                            defaultRatingSet = true;
                            setRateTheGameDb.SetAttributeValue("default", "true");
                        }

                        setRating.Add(setRateTheGameDb);
                    }

                    if (game.GameMetadata.Value.Ratings?.RottenTomatoes?.Value > 0)
                    {
                        var setRateRottenTomatoes = new XElement("rating", new XAttribute("name", "tomatometerallcritics"), new XAttribute("max", "100"));
                        setRateRottenTomatoes.Add(new XElement("value", game.GameMetadata.Value.Ratings.RottenTomatoes.Value));

                        if (!defaultRatingSet)
                        {
                            setRateRottenTomatoes.SetAttributeValue("default", "true");
                        }

                        setRating.Add(setRateRottenTomatoes);
                    }

                    details.Add(setRating);
                }

                if (game.GameMetadata.Value.Ratings?.Igdb?.Votes > 0)
                {
                    details.Add(new XElement("rating", game.GameMetadata.Value.Ratings.Igdb.Value));
                }

                if (game.GameMetadata.Value.Ratings?.RottenTomatoes?.Value > 0)
                {
                    details.Add(new XElement("criticrating", game.GameMetadata.Value.Ratings.RottenTomatoes.Value));
                }

                details.Add(new XElement("userrating"));

                details.Add(new XElement("top250"));

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

                details.Add(new XElement("playcount"));

                details.Add(new XElement("lastplayed"));

                details.Add(new XElement("id", game.IgdbId));

                var uniqueId = new XElement("uniqueid", game.IgdbId);
                uniqueId.SetAttributeValue("type", "igdb");
                uniqueId.SetAttributeValue("default", true);
                details.Add(uniqueId);

                if (game.GameMetadata.Value.ImdbId.IsNotNullOrWhiteSpace())
                {
                    var imdbId = new XElement("uniqueid", game.GameMetadata.Value.ImdbId);
                    imdbId.SetAttributeValue("type", "imdb");
                    details.Add(imdbId);
                }

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

                foreach (var credit in credits)
                {
                    if (credit.Name != null && credit.Job == "Screenplay")
                    {
                        details.Add(new XElement("credits", credit.Name));
                    }
                }

                foreach (var credit in credits)
                {
                    if (credit.Name != null && credit.Job == "Director")
                    {
                        details.Add(new XElement("director", credit.Name));
                    }
                }

                if (game.GameMetadata.Value.EarlyAccess.HasValue)
                {
                    details.Add(new XElement("premiered", game.GameMetadata.Value.EarlyAccess.Value.ToString("yyyy-MM-dd")));
                }

                details.Add(new XElement("year", game.Year));

                details.Add(new XElement("studio", game.GameMetadata.Value.Studio));

                details.Add(new XElement("trailer", "plugin://plugin.video.youtube/play/?video_id=" + game.GameMetadata.Value.YouTubeTrailerId));

                details.Add(new XElement("watched", watched));

                if (gameFile.MediaInfo != null)
                {
                    var sceneName = gameFile.GetSceneOrFileName();

                    var fileInfo = new XElement("fileinfo");
                    var streamDetails = new XElement("streamdetails");

                    var video = new XElement("video");
                    video.Add(new XElement("aspect", (float)gameFile.MediaInfo.Width / (float)gameFile.MediaInfo.Height));
                    video.Add(new XElement("bitrate", gameFile.MediaInfo.VideoBitrate));
                    video.Add(new XElement("codec", MediaInfoFormatter.FormatVideoCodec(gameFile.MediaInfo, sceneName)));
                    video.Add(new XElement("framerate", gameFile.MediaInfo.VideoFps));
                    video.Add(new XElement("height", gameFile.MediaInfo.Height));
                    video.Add(new XElement("scantype", gameFile.MediaInfo.ScanType));
                    video.Add(new XElement("width", gameFile.MediaInfo.Width));

                    if (gameFile.MediaInfo.RunTime != TimeSpan.Zero)
                    {
                        video.Add(new XElement("duration", gameFile.MediaInfo.RunTime.TotalMinutes));
                        video.Add(new XElement("durationinseconds", Math.Round(gameFile.MediaInfo.RunTime.TotalSeconds)));
                    }

                    if (gameFile.MediaInfo.VideoHdrFormat is HdrFormat.DolbyVision or HdrFormat.DolbyVisionHdr10 or HdrFormat.DolbyVisionHdr10Plus or HdrFormat.DolbyVisionHlg or HdrFormat.DolbyVisionSdr)
                    {
                        video.Add(new XElement("hdrtype", "dolbyvision"));
                    }
                    else if (gameFile.MediaInfo.VideoHdrFormat is HdrFormat.Hdr10 or HdrFormat.Hdr10Plus or HdrFormat.Pq10)
                    {
                        video.Add(new XElement("hdrtype", "hdr10"));
                    }
                    else if (gameFile.MediaInfo.VideoHdrFormat == HdrFormat.Hlg10)
                    {
                        video.Add(new XElement("hdrtype", "hlg"));
                    }
                    else if (gameFile.MediaInfo.VideoHdrFormat == HdrFormat.None)
                    {
                        video.Add(new XElement("hdrtype", ""));
                    }

                    streamDetails.Add(video);

                    var audio = new XElement("audio");
                    var audioChannelCount = gameFile.MediaInfo.AudioChannels;
                    audio.Add(new XElement("bitrate", gameFile.MediaInfo.AudioBitrate));
                    audio.Add(new XElement("channels", audioChannelCount));
                    audio.Add(new XElement("codec", MediaInfoFormatter.FormatAudioCodec(gameFile.MediaInfo, sceneName)));
                    audio.Add(new XElement("language", gameFile.MediaInfo.AudioLanguages));
                    streamDetails.Add(audio);

                    if (gameFile.MediaInfo.Subtitles is { Count: > 0 })
                    {
                        foreach (var s in gameFile.MediaInfo.Subtitles)
                        {
                            var subtitle = new XElement("subtitle");
                            subtitle.Add(new XElement("language", s));
                            streamDetails.Add(subtitle);
                        }
                    }

                    fileInfo.Add(streamDetails);
                    details.Add(fileInfo);

                    foreach (var credit in credits)
                    {
                        if (credit.Name != null && credit.Character != null)
                        {
                            var actorElement = new XElement("actor");

                            actorElement.Add(new XElement("name", credit.Name));
                            actorElement.Add(new XElement("role", credit.Character));
                            actorElement.Add(new XElement("order", credit.Order));

                            var headshot = credit.Images.FirstOrDefault(m => m.CoverType == MediaCoverTypes.Headshot);

                            if (headshot != null && headshot.RemoteUrl != null)
                            {
                                actorElement.Add(new XElement("thumb", headshot.RemoteUrl));
                            }

                            details.Add(actorElement);
                        }
                    }
                }

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
                xmlResult += "https://www.thegamedb.org/game/" + game.GameMetadata.Value.IgdbId;
                xmlResult += Environment.NewLine;

                xmlResult += "https://www.imdb.com/title/" + game.GameMetadata.Value.ImdbId;
                xmlResult += Environment.NewLine;
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

        private bool GetExistingWatchedStatus(Game game, string gameFilePath)
        {
            var fullPath = Path.Combine(game.Path, GetGameMetadataFilename(gameFilePath));

            if (!_diskProvider.FileExists(fullPath))
            {
                return false;
            }

            var fileContent = _diskProvider.ReadAllText(fullPath);

            return Regex.IsMatch(fileContent, "<watched>true</watched>");
        }
    }
}
