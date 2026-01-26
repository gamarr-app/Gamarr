import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useLocation } from 'react-router-dom';
import { createSelector } from 'reselect';
import { Error } from 'App/State/AppSectionState';
import AppState from 'App/State/AppState';
import { clearAddGame, lookupGame } from 'Store/Actions/addGameActions';
import { clearGameFiles, fetchGameFiles } from 'Store/Actions/gameFileActions';
import {
  clearQueueDetails,
  fetchQueueDetails,
} from 'Store/Actions/queueActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import hasDifferentItems from 'Utilities/Object/hasDifferentItems';
import parseUrl from 'Utilities/String/parseUrl';
import AddNewGame, { AddNewGameItem } from './AddNewGame';

function createAddNewGameSelector(searchParams: string) {
  return createSelector(
    (state: AppState) => state.addGame,
    (state: AppState) => state.games.items.length,
    (addGame, existingGamesCount) => {
      const { params } = parseUrl(searchParams);

      return {
        ...addGame,
        term: params.term as string | undefined,
        hasExistingGames: existingGamesCount > 0,
      };
    }
  );
}

function AddNewGameConnector() {
  const location = useLocation();
  const dispatch = useDispatch();

  const gameLookupTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(
    null
  );

  const selector = createAddNewGameSelector(location.search);
  const {
    term,
    isFetching,
    isAdding,
    error,
    addError,
    items,
    hasExistingGames,
  } = useSelector(selector);

  const prevItemsRef = useRef<AddNewGameItem[]>(items);

  // Mount effect
  useEffect(() => {
    dispatch(fetchRootFolders());
    dispatch(fetchQueueDetails());

    return () => {
      if (gameLookupTimeoutRef.current) {
        clearTimeout(gameLookupTimeoutRef.current);
      }

      dispatch(clearAddGame());
      dispatch(clearQueueDetails());
      dispatch(clearGameFiles());
    };
    // Only run on mount/unmount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Update effect - fetch game files when items change
  useEffect(() => {
    if (hasDifferentItems(prevItemsRef.current, items)) {
      const gameIds = items
        .filter((item) => item.internalId != null)
        .map((item) => item.internalId as number)
        .filter((id, index, self) => self.indexOf(id) === index);

      if (gameIds.length) {
        dispatch(fetchGameFiles({ gameId: gameIds }));
      }
    }

    prevItemsRef.current = items;
  }, [items, dispatch]);

  const onGameLookupChange = useCallback(
    (searchTerm: string) => {
      if (gameLookupTimeoutRef.current) {
        clearTimeout(gameLookupTimeoutRef.current);
      }

      if (searchTerm.trim() === '') {
        dispatch(clearAddGame());
      } else {
        gameLookupTimeoutRef.current = setTimeout(() => {
          dispatch(lookupGame({ term: searchTerm }));
        }, 300);
      }
    },
    [dispatch]
  );

  const onClearGameLookup = useCallback(() => {
    dispatch(clearAddGame());
  }, [dispatch]);

  return (
    <AddNewGame
      term={term}
      isFetching={isFetching}
      isAdding={isAdding}
      error={error as Error | undefined}
      addError={addError as Error | undefined}
      items={items}
      hasExistingGames={hasExistingGames}
      onGameLookupChange={onGameLookupChange}
      onClearGameLookup={onClearGameLookup}
    />
  );
}

export default AddNewGameConnector;
