using NzbDrone.Core.Games;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class UpdateCleanTitleForGames : IHousekeepingTask
    {
        private readonly IGameRepository _gameRepository;

        public UpdateCleanTitleForGames(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }

        public void Clean()
        {
            /*var games = _gameRepository.All().ToList();

            games.ForEach(m =>
            {
                m.CleanTitle = m.CleanTitle.CleanSeriesTitle();
                _gameRepository.Update(m);
            });*/
        }
    }
}
