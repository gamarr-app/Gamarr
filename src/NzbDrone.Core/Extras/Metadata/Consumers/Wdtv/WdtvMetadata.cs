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

namespace NzbDrone.Core.Extras.Metadata.Consumers.Wdtv
{
    public class WdtvMetadata : MetadataBase<WdtvMetadataSettings>
    {
        private readonly IMapCoversToLocal _mediaCoverService;
        private readonly Logger _logger;

        public WdtvMetadata(IMapCoversToLocal mediaCoverService,
                            Logger logger)
        {
            _mediaCoverService = mediaCoverService;
            _logger = logger;
        }

        public override string Name => "WDTV";

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

            var metadata = new MetadataFile
            {
                GameId = game.Id,
                Consumer = GetType().Name,
                RelativePath = game.Path.GetRelativePath(path)
            };

            if (Path.GetFileName(filename).Equals("folder.jpg", StringComparison.InvariantCultureIgnoreCase))
            {
                metadata.Type = MetadataType.GameImage;
                return metadata;
            }

            var parseResult = Parser.Parser.ParseGameTitle(filename);

            if (parseResult != null)
            {
                switch (Path.GetExtension(filename).ToLowerInvariant())
                {
                    case ".xml":
                        metadata.Type = MetadataType.GameMetadata;
                        return metadata;
                    case ".metathumb":
                        metadata.Type = MetadataType.GameImage;
                        return metadata;
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

            _logger.Debug("Generating Game File Metadata for: {0}", Path.Combine(game.Path, gameFile.RelativePath));

            var xmlResult = string.Empty;

            var sb = new StringBuilder();
            var xws = new XmlWriterSettings();
            xws.OmitXmlDeclaration = true;
            xws.Indent = false;

            using (var xw = XmlWriter.Create(sb, xws))
            {
                var doc = new XDocument();

                var details = new XElement("details");
                details.Add(new XElement("id", game.Id));
                details.Add(new XElement("title", game.Title));
                details.Add(new XElement("genre", string.Join(" / ", game.GameMetadata.Value.Genres)));
                details.Add(new XElement("overview", game.GameMetadata.Value.Overview));

                doc.Add(details);
                doc.Save(xw);

                xmlResult += doc.ToString();
                xmlResult += Environment.NewLine;
            }

            var filename = GetGameFileMetadataFilename(gameFile.RelativePath);

            return new MetadataFileResult(filename, xmlResult.Trim(Environment.NewLine.ToCharArray()));
        }

        public override List<ImageFileResult> GameImages(Game game)
        {
            if (!Settings.GameImages)
            {
                return new List<ImageFileResult>();
            }

            // Because we only support one image, attempt to get the Poster type, then if that fails grab the first
            var image = game.GameMetadata.Value.Images.SingleOrDefault(c => c.CoverType == MediaCoverTypes.Poster) ?? game.GameMetadata.Value.Images.FirstOrDefault();
            if (image == null)
            {
                _logger.Trace("Failed to find suitable Game image for game {0}.", game.Title);
                return new List<ImageFileResult>();
            }

            var source = _mediaCoverService.GetCoverPath(game.Id, image.CoverType);
            var destination = "folder" + Path.GetExtension(source);

            return new List<ImageFileResult>
                   {
                       new ImageFileResult(destination, source)
                   };
        }

        private string GetGameFileMetadataFilename(string gameFilePath)
        {
            return Path.ChangeExtension(gameFilePath, "xml");
        }

        private string GetGameFileImageFilename(string gameFilePath)
        {
            return Path.ChangeExtension(gameFilePath, "metathumb");
        }
    }
}
