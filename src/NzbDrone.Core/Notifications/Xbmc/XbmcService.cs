using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NLog;
using NzbDrone.Core.Localization;
using NzbDrone.Core.Games;

#pragma warning disable CS0618 // Disable obsolete warnings for ImdbId (kept for backward compatibility with Xbmc)

namespace NzbDrone.Core.Notifications.Xbmc
{
    public interface IXbmcService
    {
        void Notify(XbmcSettings settings, string title, string message);
        void Update(XbmcSettings settings, Game game);
        void Clean(XbmcSettings settings);
        ValidationFailure Test(XbmcSettings settings, string message);
    }

    public class XbmcService : IXbmcService
    {
        private readonly IXbmcJsonApiProxy _proxy;
        private readonly ILocalizationService _localizationService;
        private readonly Logger _logger;

        public XbmcService(IXbmcJsonApiProxy proxy, ILocalizationService localizationService, Logger logger)
        {
            _proxy = proxy;
            _localizationService = localizationService;
            _logger = logger;
        }

        public void Notify(XbmcSettings settings, string title, string message)
        {
            _proxy.Notify(settings, title, message);
        }

        public void Update(XbmcSettings settings, Game game)
        {
            if (CheckIfVideoPlayerOpen(settings))
            {
                _logger.Debug("Video is currently playing, skipping library update");

                return;
            }

            UpdateLibrary(settings, game);
        }

        public void Clean(XbmcSettings settings)
        {
            if (CheckIfVideoPlayerOpen(settings))
            {
                _logger.Debug("Video is currently playing, skipping library clean");

                return;
            }

            _proxy.CleanLibrary(settings);
        }

        public string GetGamePath(XbmcSettings settings, Game game)
        {
            var allGames = _proxy.GetGames(settings);

            if (!allGames.Any())
            {
                _logger.Debug("No Games returned from Kodi");
                return null;
            }

            var matchingGames = allGames.FirstOrDefault(s =>
            {
                return s.ImdbNumber == game.ImdbId;
            });

            if (matchingGames != null)
            {
                return matchingGames.File;
            }

            return null;
        }

        private void UpdateLibrary(XbmcSettings settings, Game game)
        {
            try
            {
                var gamePath = GetGamePath(settings, game);

                if (gamePath != null)
                {
                    _logger.Debug("Updating game {0} (Kodi path: {1}) on Kodi host: {2}", game, gamePath, settings.Address);
                }
                else
                {
                    _logger.Debug("Game {0} doesn't exist on Kodi host: {1}, Updating Entire Library", game, settings.Address);
                }

                var response = _proxy.UpdateLibrary(settings, gamePath);

                if (!response.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                {
                    _logger.Debug("Failed to update library for: {0}", settings.Address);
                }
            }
            catch (Exception ex)
            {
                _logger.Debug(ex, ex.Message);
            }
        }

        private bool CheckIfVideoPlayerOpen(XbmcSettings settings)
        {
            if (settings.AlwaysUpdate)
            {
                return false;
            }

            _logger.Debug("Determining if there are any active players on Kodi host: {0}", settings.Address);
            var activePlayers = _proxy.GetActivePlayers(settings);

            return activePlayers.Any(a => a.Type.Equals("video"));
        }

        public ValidationFailure Test(XbmcSettings settings, string message)
        {
            try
            {
                Notify(settings, "Test Notification", message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test message");
                return new ValidationFailure("Host", _localizationService.GetLocalizedString("NotificationsValidationUnableToSendTestMessage", new Dictionary<string, object> { { "exceptionMessage", ex.Message } }));
            }

            return null;
        }
    }
}
