import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  setGameCollectionsOption,
  setGameCollectionsOverviewOption,
} from 'Store/Actions/gameCollectionActions';
import CollectionOverviewOptionsModalContent from './CollectionOverviewOptionsModalContent';

const defaultOverviewOptions = {
  detailedProgressBar: false,
  size: 'medium',
  showDetails: true,
  showOverview: true,
  showPosters: true,
};

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.gameCollections,
    (gameCollections) => {
      const overviewOptions =
        gameCollections.overviewOptions ?? defaultOverviewOptions;

      return {
        detailedProgressBar: overviewOptions.detailedProgressBar,
        size: overviewOptions.size,
        showDetails: overviewOptions.showDetails,
        showOverview: overviewOptions.showOverview,
        showPosters: overviewOptions.showPosters,
      };
    }
  );
}

const mapDispatchToProps = {
  onChangeOverviewOption: setGameCollectionsOverviewOption,
  onChangeOption: setGameCollectionsOption,
};

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(CollectionOverviewOptionsModalContent);
