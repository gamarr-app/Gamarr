import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  fetchGameCollections,
  saveGameCollections,
  setGameCollectionsFilter,
  setGameCollectionsSort
} from 'Store/Actions/gameCollectionActions';
import { clearQueueDetails, fetchQueueDetails } from 'Store/Actions/queueActions';
import scrollPositions from 'Store/scrollPositions';
import createCollectionClientSideCollectionItemsSelector from 'Store/Selectors/createCollectionClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import Collection from './Collection';

function createMapStateToProps() {
  return createSelector(
    createCollectionClientSideCollectionItemsSelector('gameCollections'),
    createCommandExecutingSelector(commandNames.REFRESH_COLLECTIONS),
    createDimensionsSelector(),
    (
      collections,
      isRefreshingCollections,
      dimensionsState
    ) => {
      return {
        ...collections,
        isRefreshingCollections,
        isSmallScreen: dimensionsState.isSmallScreen
      };
    }
  );
}

function createMapDispatchToProps(dispatch, props) {
  return {
    dispatchFetchGameCollections() {
      dispatch(fetchGameCollections());
    },
    dispatchFetchQueueDetails() {
      dispatch(fetchQueueDetails());
    },
    dispatchClearQueueDetails() {
      dispatch(clearQueueDetails());
    },
    onUpdateSelectedPress(payload) {
      dispatch(saveGameCollections(payload));
    },
    onSortSelect(sortKey) {
      dispatch(setGameCollectionsSort({ sortKey }));
    },
    onFilterSelect(selectedFilterKey) {
      dispatch(setGameCollectionsFilter({ selectedFilterKey }));
    },
    onRefreshGameCollectionsPress() {
      dispatch(executeCommand({
        name: commandNames.REFRESH_COLLECTIONS
      }));
    }
  };
}

class CollectionConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchGameCollections();
    this.props.dispatchFetchQueueDetails();
  }

  componentWillUnmount() {
    this.props.dispatchClearQueueDetails();
  }

  //
  // Listeners

  onScroll = ({ scrollTop }) => {
    scrollPositions.gameCollections = scrollTop;
  };

  onUpdateSelectedPress = (payload) => {
    this.props.onUpdateSelectedPress(payload);
  };

  //
  // Render

  render() {
    const {
      dispatchFetchGameCollections,
      dispatchFetchQueueDetails,
      dispatchClearQueueDetails,
      ...otherProps
    } = this.props;

    return (
      <Collection
        {...otherProps}
        onViewSelect={this.onViewSelect}
        onScroll={this.onScroll}
        onUpdateSelectedPress={this.onUpdateSelectedPress}
      />
    );
  }
}

CollectionConnector.propTypes = {
  isSmallScreen: PropTypes.bool.isRequired,
  view: PropTypes.string.isRequired,
  onUpdateSelectedPress: PropTypes.func.isRequired,
  dispatchFetchGameCollections: PropTypes.func.isRequired,
  dispatchFetchQueueDetails: PropTypes.func.isRequired,
  dispatchClearQueueDetails: PropTypes.func.isRequired
};

export default withScrollPosition(
  connect(createMapStateToProps, createMapDispatchToProps)(CollectionConnector),
  'gameCollections'
);
