import { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { GameFile } from 'GameFile/GameFile';
import {
  ASCENDING as SORT_ASCENDING,
  SortDirection,
} from 'Helpers/Props/sortDirections';
import {
  deleteGameFile,
  setGameFilesSort,
  setGameFilesTableOption,
} from 'Store/Actions/gameFileActions';
import {
  fetchLanguages,
  fetchQualityProfileSchema,
} from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector, {
  CollectionResult,
} from 'Store/Selectors/createClientSideCollectionSelector';
import { TableOptionsChangePayload } from 'typings/Table';
import getQualities from 'Utilities/Quality/getQualities';
import GameFileEditorTableContent from './GameFileEditorTableContent';

interface GameFileEditorTableContentConnectorProps {
  gameId: number;
}

function createMapStateToProps(gameId: number) {
  return createSelector(
    createClientSideCollectionSelector<GameFile>('gameFiles'),
    (state: AppState) => state.settings.languages,
    (state: AppState) => state.settings.qualityProfiles,
    (
      gameFilesResult: CollectionResult<GameFile>,
      languageProfiles,
      qualityProfiles
    ) => {
      const languages = languageProfiles.items;
      const qualities = getQualities(qualityProfiles.schema?.items ?? []);
      const filesForGame = gameFilesResult.items.filter(
        (file) => file.gameId === gameId
      );

      return {
        items: filesForGame,
        columns: gameFilesResult.columns ?? [],
        sortKey: gameFilesResult.sortKey ?? 'relativePath',
        sortDirection: gameFilesResult.sortDirection ?? SORT_ASCENDING,
        isDeleting: gameFilesResult.isDeleting ?? false,
        isSaving: gameFilesResult.isSaving ?? false,
        error: null,
        languages,
        qualities,
      };
    }
  );
}

function GameFileEditorTableContentConnector(
  props: GameFileEditorTableContentConnectorProps
) {
  const { gameId } = props;
  const dispatch = useDispatch();

  const selector = useMemo(() => createMapStateToProps(gameId), [gameId]);
  const { items, columns, sortKey, sortDirection, isDeleting } =
    useSelector(selector);

  useEffect(() => {
    dispatch(fetchLanguages());
    dispatch(fetchQualityProfileSchema());
  }, [dispatch]);

  const onDeletePress = useCallback(
    (gameFileId: number) => {
      dispatch(
        deleteGameFile({
          id: gameFileId,
        })
      );
    },
    [dispatch]
  );

  const onTableOptionChange = useCallback(
    (payload: TableOptionsChangePayload) => {
      dispatch(setGameFilesTableOption(payload));
    },
    [dispatch]
  );

  const onSortPress = useCallback(
    (newSortKey: string, newSortDirection?: SortDirection) => {
      dispatch(
        setGameFilesSort({
          sortKey: newSortKey,
          sortDirection: newSortDirection,
        })
      );
    },
    [dispatch]
  );

  return (
    <GameFileEditorTableContent
      gameId={gameId}
      items={items}
      columns={columns}
      sortKey={sortKey}
      sortDirection={sortDirection}
      isDeleting={isDeleting}
      onDeletePress={onDeletePress}
      onTableOptionChange={onTableOptionChange}
      onSortPress={onSortPress}
    />
  );
}

export default GameFileEditorTableContentConnector;
