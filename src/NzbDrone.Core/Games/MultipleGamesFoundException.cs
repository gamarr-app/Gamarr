using System.Collections.Generic;
using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Games
{
    public class MultipleGamesFoundException : NzbDroneException
    {
        public List<Game> Games { get; set; }

        public MultipleGamesFoundException(List<Game> games, string message, params object[] args)
            : base(message, args)
        {
            Games = games;
        }
    }
}
