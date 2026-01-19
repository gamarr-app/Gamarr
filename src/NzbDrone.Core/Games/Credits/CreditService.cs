using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Games.Events;

namespace NzbDrone.Core.Games.Credits
{
    public interface ICreditService
    {
        List<Credit> GetAllCreditsForGameMetadata(int gameMetadataId);
        Credit AddCredit(Credit credit, GameMetadata game);
        List<Credit> AddCredits(List<Credit> credits, GameMetadata game);
        Credit GetById(int id);
        List<Credit> GetAllCredits();
        List<Credit> UpdateCredits(List<Credit> credits, GameMetadata game);
    }

    public class CreditService : ICreditService, IHandleAsync<GamesDeletedEvent>
    {
        private readonly ICreditRepository _creditRepo;
        private readonly Logger _logger;

        public CreditService(ICreditRepository creditRepo, Logger logger)
        {
            _creditRepo = creditRepo;
            _logger = logger;
        }

        public List<Credit> GetAllCreditsForGameMetadata(int gameMetadataId)
        {
            return _creditRepo.FindByGameMetadataId(gameMetadataId).ToList();
        }

        public Credit AddCredit(Credit credit, GameMetadata game)
        {
            credit.GameMetadataId = game.Id;
            return _creditRepo.Insert(credit);
        }

        public List<Credit> AddCredits(List<Credit> credits, GameMetadata game)
        {
            credits.ForEach(t => t.GameMetadataId = game.Id);
            _creditRepo.InsertMany(credits);
            return credits;
        }

        public Credit GetById(int id)
        {
            return _creditRepo.Get(id);
        }

        public List<Credit> GetAllCredits()
        {
            return _creditRepo.All().ToList();
        }

        public void RemoveTitle(Credit credit)
        {
            _creditRepo.Delete(credit);
        }

        public List<Credit> UpdateCredits(List<Credit> credits, GameMetadata gameMetadata)
        {
            var gameMetadataId = gameMetadata.Id;

            // First update the game ids so we can correlate them later.
            credits.ForEach(t => t.GameMetadataId = gameMetadataId);

            // Should never have multiple credits with same credit_id, but check to ensure in case IGDB is on fritz
            var dupeFreeCredits = credits.DistinctBy(m => m.CreditIgdbId).ToList();

            var existingCredits = _creditRepo.FindByGameMetadataId(gameMetadataId);

            var updateList = new List<Credit>();
            var addList = new List<Credit>();
            var upToDateCount = 0;

            foreach (var credit in dupeFreeCredits)
            {
                var existingCredit = existingCredits.FirstOrDefault(x => x.CreditIgdbId == credit.CreditIgdbId);

                if (existingCredit != null)
                {
                    existingCredits.Remove(existingCredit);

                    credit.UseDbFieldsFrom(existingCredit);

                    if (!credit.Equals(existingCredit))
                    {
                        updateList.Add(credit);
                    }
                    else
                    {
                        upToDateCount++;
                    }
                }
                else
                {
                    addList.Add(credit);
                }
            }

            _creditRepo.DeleteMany(existingCredits);
            _creditRepo.UpdateMany(updateList);
            _creditRepo.InsertMany(addList);

            _logger.Debug("[{0}] {1} credits up to date; Updating {2}, Adding {3}, Deleting {4} entries.", gameMetadata.Title, upToDateCount, updateList.Count, addList.Count, existingCredits.Count);

            return credits;
        }

        public void HandleAsync(GamesDeletedEvent message)
        {
            // TODO handle metadata deletions and not game deletions
            _creditRepo.DeleteForGames(message.Games.Select(m => m.GameMetadataId).ToList());
        }
    }
}
