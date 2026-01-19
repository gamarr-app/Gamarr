import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { setGameCollectionsOption, setGameCollectionsOverviewOption } from 'Store/Actions/gameCollectionActions';
import CollectionOverviewOptionsModalContent from './CollectionOverviewOptionsModalContent';

function createMapStateToProps() {
  return createSelector(
    (state) => state.gameCollections,
    (gameCollections) => {
      return {
        ...gameCollections.options,
        ...gameCollections.overviewOptions
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    onChangeOverviewOption(payload) {
      dispatch(setGameCollectionsOverviewOption(payload));
    },
    onChangeOption(payload) {
      dispatch(setGameCollectionsOption(payload));
    }
  };
}

export default connect(createMapStateToProps, createMapDispatchToProps)(CollectionOverviewOptionsModalContent);
