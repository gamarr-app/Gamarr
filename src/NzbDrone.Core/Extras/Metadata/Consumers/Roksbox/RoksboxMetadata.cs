using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Extras.Metadata.Files;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Games;

namespace NzbDrone.Core.Extras.Metadata.Consumers.Roksbox
{
    public class RoksboxMetadata : MetadataBase<RoksboxMetadataSettings>
    {
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public RoksboxMetadata(IMapCoversToLocal mediaCoverService,
                            Logger logger)
        {
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        // Re-enable when/if we store and use mpaa certification
        // private static List<string> ValidCertification = new List<string> { "G", "NC-17", "PG", "PG-13", "R", "UR", "UNRATED", "NR", "TV-Y", "TV-Y7", "TV-Y7-FV", "TV-G", "TV-PG", "TV-14", "TV-MA" };
        public override string Name => "Roksbox";

        public override string GetFilenameAfterMove(Game game, GameFile gameFile, MetadataFile metadataFile)
        {
            var gameFilePath = Path.Combine(game.Path, gameFile.RelativePath);

            if (metadataFile.Type == MetadataType.GameImage)
            {
                return GetGameFileImageFilename(gameFilePath);
            }

            if (metadataFile.Type == MetadataType.GameMetadata)
            {
                return GetGameFileMetadataFilename(gameFilePath);
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

            var parentdir = Directory.GetParent(path);

            var metadata = new MetadataFile
            {
                GameId = game.Id,
                Consumer = GetType().Name,
                RelativePath = game.Path.GetRelativePath(path)
            };

            var parseResult = Parser.Parser.ParseGameTitle(filename);

            if (parseResult != null)
            {
                var extension = Path.GetExtension(filename).ToLowerInvariant();

                if (extension == ".xml")
                {
                    metadata.Type = MetadataType.GameMetadata;
                    return metadata;
                }

                if (extension == ".jpg")
                {
                    if (Path.GetFileNameWithoutExtension(filename).Equals(parentdir.Name, StringComparison.InvariantCultureIgnoreCase) &&
                        !path.GetParentName().Equals("metadata", StringComparison.InvariantCultureIgnoreCase))
                    {
                        metadata.Type = MetadataType.GameImage;
                        return metadata;
                    }
                }
            }

            return null;
        }

        public override MetadataFileResult GameMetadata(Game game, GameFile gameFile)
        {
            if (!Settings.GameMetadata)
            {
                return null;
            }

            _logger.Debug("Generating Game File Metadata for: {0}", gameFile.RelativePath);

            var xmlResult = string.Empty;

            var sb = new StringBuilder();
            var xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = false;

            using (var xw = XmlWriter.Create(sb, xws))
            {
                var doc = new XDocument();

                var details = new XElement("video");
                details.Add(new XElement("title", game.Title));

                details.Add(new XElement("genre", string.Join(" / ", game.GameMetadata.Value.Genres)));
                details.Add(new XElement("description", game.GameMetadata.Value.Overview));
                details.Add(new XElement("length", game.GameMetadata.Value.Runtime));

                doc.Add(details);
                doc.Save(xw);

                xmlResult += doc.ToString();
                xmlResult += Environment.NewLine;
            }

            return new MetadataFileResult(GetGameFileMetadataFilename(gameFile.RelativePath), xmlResult.Trim(Environment.NewLine.ToCharArray()));
        }

        public override List<ImageFileResult> GameImages(Game game)
        {
            if (!Settings.GameImages)
            {
                return new List<ImageFileResult>();
            }

            var image = game.GameMetadata.Value.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? game.GameMetadata.Value.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable Game image for game {0}.", game.Title);
                return new List<ImageFileResult>();
            }

            var source = _mediaCoverService.GetCoverPath(game.Id, image.CoverType);
            var destination = Path.GetFileName(game.Path) + Path.GetExtension(source);

            return new List<ImageFileResult> { new ImageFileResult(destination, source) };
        }

        private string GetGameFileMetadataFilename(string gameFilePath)
        {
            return Path.ChangeExtension(gameFilePath, "xml");
        }

        private string GetGameFileImageFilename(string gameFilePath)
        {
            return Path.ChangeExtension(gameFilePath, "jpg");
        }
    }
}
