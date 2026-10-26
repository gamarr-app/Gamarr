import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createExistingGameSelector from 'Store/Selectors/createExistingGameSelector';
import ImportGameSearchResult from './ImportGameSearchResult';

function createMapStateToProps() {
  // Note: do not default `steamAppId` here. Overriding the ownProp with 0
  // pushes every lookup result onto the IGDB id path, and Steam-sourced
  // results all carry `igdbId: 0` — so every result matched every library
  // game and showed up as "Existing".
  return createSelector(createExistingGameSelector(), (isExistingGame) => {
    return {
      isExistingGame,
    };
  });
}

export default connect(createMapStateToProps)(ImportGameSearchResult);
