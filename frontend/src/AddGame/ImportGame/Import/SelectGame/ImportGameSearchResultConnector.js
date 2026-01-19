import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createExistingGameSelector from 'Store/Selectors/createExistingGameSelector';
import ImportGameSearchResult from './ImportGameSearchResult';

function createMapStateToProps() {
  return createSelector(
    createExistingGameSelector(),
    (isExistingGame) => {
      return {
        isExistingGame
      };
    }
  );
}

export default connect(createMapStateToProps)(ImportGameSearchResult);
