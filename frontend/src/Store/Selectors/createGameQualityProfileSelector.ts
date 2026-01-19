import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import QualityProfile from 'typings/QualityProfile';
import { createGameSelectorForHook } from './createGameSelector';

function createGameQualityProfileSelector(gameId: number) {
  return createSelector(
    (state: AppState) => state.settings.qualityProfiles.items,
    createGameSelectorForHook(gameId),
    (qualityProfiles: QualityProfile[], game = {} as Game) => {
      return qualityProfiles.find(
        (profile) => profile.id === game.qualityProfileId
      );
    }
  );
}

export default createGameQualityProfileSelector;
