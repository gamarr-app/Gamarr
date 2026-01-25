import React, { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import * as commandNames from 'Commands/commandNames';
import withScrollPosition from 'Components/withScrollPosition';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  fetchGameCollections,
  saveGameCollections,
  setGameCollectionsFilter,
  setGameCollectionsSort,
} from 'Store/Actions/gameCollectionActions';
import {
  clearQueueDetails,
  fetchQueueDetails,
} from 'Store/Actions/queueActions';
import scrollPositions from 'Store/scrollPositions';
import createCollectionClientSideCollectionItemsSelector from 'Store/Selectors/createCollectionClientSideCollectionItemsSelector';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import Collection from './Collection';

const createMapStateToProps = createSelector(
  createCollectionClientSideCollectionItemsSelector('gameCollections'),
  createCommandExecutingSelector(commandNames.REFRESH_COLLECTIONS),
  createDimensionsSelector(),
  (state: AppState) => state.gameCollections,
  (collections, isRefreshingCollections, dimensionsState, gameCollections) => {
    return {
      ...collections,
      totalItems: collections.totalItems ?? 0,
      selectedFilterKey: gameCollections.selectedFilterKey,
      filters: gameCollections.filters,
      customFilters: collections.customFilters ?? [],
      isFetching: gameCollections.isFetching,
      isPopulated: gameCollections.isPopulated,
      error: gameCollections.error,
      isSaving: gameCollections.isSaving,
      saveError: gameCollections.saveError,
      isAdding: gameCollections.isAdding,
      view: 'overview',
      isRefreshingCollections,
      isSmallScreen: dimensionsState.isSmallScreen,
    };
  }
);

interface CollectionConnectorProps {
  initialScrollTop?: number;
}

function CollectionConnector({ initialScrollTop }: CollectionConnectorProps) {
  const dispatch = useDispatch();
  const state = useSelector(createMapStateToProps);

  useEffect(() => {
    dispatch(fetchGameCollections());
    dispatch(fetchQueueDetails());

    return () => {
      dispatch(clearQueueDetails());
    };
  }, [dispatch]);

  const onScroll = useCallback(({ scrollTop }: { scrollTop: number }) => {
    scrollPositions.gameCollections = scrollTop;
  }, []);

  const onUpdateSelectedPress = useCallback(
    (payload: { collectionIds: number[]; [key: string]: unknown }) => {
      dispatch(saveGameCollections(payload));
    },
    [dispatch]
  );

  const onSortSelect = useCallback(
    (sortKey: string) => {
      dispatch(setGameCollectionsSort({ sortKey }));
    },
    [dispatch]
  );

  const onFilterSelect = useCallback(
    (selectedFilterKey: string | number) => {
      dispatch(setGameCollectionsFilter({ selectedFilterKey }));
    },
    [dispatch]
  );

  const onRefreshGameCollectionsPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.REFRESH_COLLECTIONS,
      })
    );
  }, [dispatch]);

  return (
    <Collection
      {...state}
      initialScrollTop={initialScrollTop}
      onScroll={onScroll}
      onUpdateSelectedPress={onUpdateSelectedPress}
      onSortSelect={onSortSelect}
      onFilterSelect={onFilterSelect}
      onRefreshGameCollectionsPress={onRefreshGameCollectionsPress}
    />
  );
}

export default withScrollPosition(CollectionConnector, 'gameCollections');
