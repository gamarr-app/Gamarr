using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Games
{
    public class Ratings : MemberwiseEquatable<Ratings>, IEmbeddedDocument
    {
        public RatingChild Imdb { get; set; }
        public RatingChild Igdb { get; set; }
        public RatingChild Metacritic { get; set; }
        public RatingChild RottenTomatoes { get; set; }
        public RatingChild Trakt { get; set; }
    }

    public class RatingChild : MemberwiseEquatable<RatingChild>
    {
        public int Votes { get; set; }
        public decimal Value { get; set; }
        public RatingType Type { get; set; }
    }

    public enum RatingType
    {
        User,
        Critic
    }
}
