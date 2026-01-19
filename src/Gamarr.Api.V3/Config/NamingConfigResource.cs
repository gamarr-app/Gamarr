using NzbDrone.Core.Organizer;
using Gamarr.Http.REST;

namespace Gamarr.Api.V3.Config
{
    public class NamingConfigResource : RestResource
    {
        public bool RenameGames { get; set; }
        public bool ReplaceIllegalCharacters { get; set; }
        public ColonReplacementFormat ColonReplacementFormat { get; set; }
        public string StandardGameFormat { get; set; }
        public string GameFolderFormat { get; set; }
    }
}
