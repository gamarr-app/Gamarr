using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.GameImport
{
    public class ImportDecision
    {
        public LocalGame LocalGame { get; private set; }
        public IEnumerable<ImportRejection> Rejections { get; private set; }

        public bool Approved => Rejections.Empty();

        public ImportDecision(LocalGame localGame, params ImportRejection[] rejections)
        {
            LocalGame = localGame;
            Rejections = rejections.ToList();
        }
    }
}
