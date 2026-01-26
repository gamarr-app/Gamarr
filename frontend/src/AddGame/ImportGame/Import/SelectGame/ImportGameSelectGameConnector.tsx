import _ from 'lodash';
import { useCallback, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Game from 'Game/Game';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import ImportGameSelectGame from './ImportGameSelectGame';

interface SelectedGame {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
  [key: string]: unknown;
}

interface GameItem {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
}

interface ImportGameItem {
  id: string;
  selectedGame?: SelectedGame;
  items?: GameItem[];
  isFetching?: boolean;
  isPopulated?: boolean;
  isQueued?: boolean;
  error?: { responseJSON?: { message?: string } };
  [key: string]: unknown;
}

interface ImportGameSelectGameConnectorProps {
  id: string;
}

function ImportGameSelectGameConnector(
  props: ImportGameSelectGameConnectorProps
) {
  const { id } = props;

  const dispatch = useDispatch();

  const selector = useMemo(
    () =>
      createSelector(
        (state: AppState) => state.importGame.isLookingUpGame,
        (state: AppState) => state.addGame,
        (state: AppState) => state.importGame,
        createAllGamesSelector(),
        (isLookingUpGame, addGame, importGame, games: Game[]) => {
          const item =
            (_.find(importGame.items, { id }) as unknown as ImportGameItem) ||
            {};
          const selectedGame = item.selectedGame;
          const isExistingGame =
            !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

          return {
            isLookingUpGame,
            defaultMonitor: addGame.defaults.monitor,
            defaultQualityProfileId: addGame.defaults.qualityProfileId,
            ...item,
            isExistingGame,
          };
        }
      ),
    [id]
  );

  const {
    isLookingUpGame,
    items,
    selectedGame,
    isExistingGame,
    isFetching,
    isPopulated,
    error,
    isQueued,
  } = useSelector(selector) as {
    isLookingUpGame: boolean;
    items?: GameItem[];
    selectedGame?: SelectedGame;
    isExistingGame: boolean;
    isFetching?: boolean;
    isPopulated?: boolean;
    error?: { responseJSON?: { message?: string } };
    isQueued?: boolean;
  };

  const onSearchInputChange = useCallback(
    (term: string) => {
      dispatch(
        queueLookupGame({
          name: id,
          term,
          topOfQueue: true,
        })
      );
    },
    [dispatch, id]
  );

  const onGameSelect = useCallback(
    (igdbId: number) => {
      dispatch(
        setImportGameValue({
          id,
          selectedGame: _.find(items, { igdbId }),
        })
      );
    },
    [dispatch, id, items]
  );

  return (
    <ImportGameSelectGame
      id={id}
      isLookingUpGame={isLookingUpGame || false}
      items={items || []}
      selectedGame={selectedGame}
      isExistingGame={isExistingGame || false}
      isFetching={isFetching ?? true}
      isPopulated={isPopulated ?? false}
      error={error}
      isQueued={isQueued ?? true}
      onSearchInputChange={onSearchInputChange}
      onGameSelect={onGameSelect}
    />
  );
}

export default ImportGameSelectGameConnector;
