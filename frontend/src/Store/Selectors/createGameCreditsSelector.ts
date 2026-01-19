import _ from 'lodash';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { GameCreditType } from 'typings/GameCredit';

function createGameCreditsSelector(gameCreditType: GameCreditType) {
  return createSelector(
    (state: AppState) => state.gameCredits.items,
    (gameCredits) => {
      const credits = gameCredits.filter(
        ({ type }) => type === gameCreditType
      );

      const sortedCredits = credits.sort((a, b) => a.order - b.order);

      return {
        items: _.uniqBy(sortedCredits, 'personName'),
      };
    }
  );
}

export default createGameCreditsSelector;
