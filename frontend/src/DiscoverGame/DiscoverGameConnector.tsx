import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import { CustomFilter, Filter } from 'App/State/AppState';
import * as commandNames from 'Commands/commandNames';
import Column from 'Components/Table/Column';
import withScrollPosition from 'Components/withScrollPosition';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  addGames,
  addImportListExclusions,
  clearAddGame,
  fetchDiscoverGames,
  setListGameFilter,
  setListGameSort,
  setListGameTableOption,
  setListGameView,
} from 'Store/Actions/discoverGameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import scrollPositions from 'Store/scrollPositions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createDiscoverGameClientSideCollectionItemsSelector from 'Store/Selectors/createDiscoverGameClientSideCollectionItemsSelector';
import {
  registerPagePopulator,
  unregisterPagePopulator,
} from 'Utilities/pagePopulator';
import DiscoverGame from './DiscoverGame';

interface DiscoverGameConnectorProps {
  initialScrollTop?: number;
}

function DiscoverGameConnector({
  initialScrollTop,
}: DiscoverGameConnectorProps) {
  const dispatch = useDispatch();

  const stateSelector = useMemo(
    () =>
      createSelector(
        (state: { discoverGame: Record<string, unknown> }) =>
          state.discoverGame,
        createDiscoverGameClientSideCollectionItemsSelector('discoverGame'),
        createCommandExecutingSelector(commandNames.IMPORT_LIST_SYNC),
        createDimensionsSelector(),
        (discoverGame, games, isSyncingLists, dimensionsState) => {
          const discoverGameTyped = discoverGame as {
            options: {
              includeRecommendations: boolean;
              includeTrending: boolean;
              includePopular: boolean;
            };
            view: string;
            columns: Column[];
            sortKey: string;
            sortDirection: 'ascending' | 'descending';
            selectedFilterKey: string | number;
            filters: Filter[];
            customFilters?: CustomFilter[];
            isFetching: boolean;
            isPopulated: boolean;
            error?: object | null;
          };

          const gamesTyped = games as {
            items: Array<{ id: number; igdbId: number; sortTitle: string }>;
            totalItems: number;
            isFetching?: boolean;
            isPopulated?: boolean;
            error?: object | null;
          };

          return {
            ...discoverGameTyped.options,
            items: gamesTyped.items,
            totalItems: gamesTyped.totalItems,
            isFetching:
              gamesTyped.isFetching ?? discoverGameTyped.isFetching ?? false,
            isPopulated:
              gamesTyped.isPopulated ?? discoverGameTyped.isPopulated ?? false,
            error: gamesTyped.error ?? discoverGameTyped.error,
            view: discoverGameTyped.view,
            columns: discoverGameTyped.columns,
            sortKey: discoverGameTyped.sortKey,
            sortDirection: discoverGameTyped.sortDirection,
            selectedFilterKey: discoverGameTyped.selectedFilterKey,
            filters: discoverGameTyped.filters,
            customFilters: discoverGameTyped.customFilters || [],
            isSyncingLists,
            isSmallScreen: dimensionsState.isSmallScreen,
          };
        }
      ),
    []
  );

  const state = useSelector(stateSelector);

  const dispatchFetchRootFolders = useCallback(() => {
    dispatch(fetchRootFolders());
  }, [dispatch]);

  const dispatchClearListGame = useCallback(() => {
    dispatch(clearAddGame());
  }, [dispatch]);

  const dispatchFetchListGames = useCallback(() => {
    dispatch(fetchDiscoverGames());
  }, [dispatch]);

  const handleTableOptionChange = useCallback(
    (payload: { pageSize?: number; columns?: Column[] }) => {
      dispatch(setListGameTableOption(payload));
    },
    [dispatch]
  );

  const handleSortSelect = useCallback(
    (sortKey: string) => {
      dispatch(setListGameSort({ sortKey }));
    },
    [dispatch]
  );

  const handleFilterSelect = useCallback(
    (selectedFilterKey: string | number) => {
      dispatch(setListGameFilter({ selectedFilterKey }));
    },
    [dispatch]
  );

  const handleViewSelect = useCallback(
    (view: string) => {
      dispatch(setListGameView({ view }));
    },
    [dispatch]
  );

  const handleAddGames = useCallback(
    (ids: number[], addOptions: Record<string, unknown>) => {
      dispatch(addGames({ ids, addOptions }));
    },
    [dispatch]
  );

  const handleAddImportListExclusions = useCallback(
    (exclusions: { ids: number[] }) => {
      dispatch(addImportListExclusions(exclusions));
    },
    [dispatch]
  );

  const handleImportListSyncPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.IMPORT_LIST_SYNC,
        commandFinished: dispatchFetchListGames,
      })
    );
  }, [dispatch, dispatchFetchListGames]);

  const handleScroll = useCallback(({ scrollTop }: { scrollTop: number }) => {
    scrollPositions.discoverGame = scrollTop;
  }, []);

  const handleAddGamesPress = useCallback(
    ({
      ids,
      addOptions,
    }: {
      ids: number[];
      addOptions: Record<string, unknown>;
    }) => {
      handleAddGames(ids, addOptions);
    },
    [handleAddGames]
  );

  const handleExcludeGamesPress = useCallback(
    ({ ids }: { ids: number[] }) => {
      handleAddImportListExclusions({ ids });
    },
    [handleAddImportListExclusions]
  );

  useEffect(() => {
    registerPagePopulator(dispatchFetchListGames);
    dispatchFetchRootFolders();
    dispatchFetchListGames();

    return () => {
      dispatchClearListGame();
      unregisterPagePopulator(dispatchFetchListGames);
    };
  }, [dispatchFetchRootFolders, dispatchFetchListGames, dispatchClearListGame]);

  return (
    <DiscoverGame
      {...state}
      initialScrollTop={initialScrollTop}
      dispatchFetchListGames={dispatchFetchListGames}
      onTableOptionChange={handleTableOptionChange}
      onSortSelect={handleSortSelect}
      onFilterSelect={handleFilterSelect}
      onViewSelect={handleViewSelect}
      onScroll={handleScroll}
      onAddGamesPress={handleAddGamesPress}
      onExcludeGamesPress={handleExcludeGamesPress}
      onImportListSyncPress={handleImportListSyncPress}
    />
  );
}

export default withScrollPosition(DiscoverGameConnector, 'discoverGame');
