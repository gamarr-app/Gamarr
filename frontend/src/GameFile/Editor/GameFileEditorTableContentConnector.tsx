import React, { useCallback, useEffect, useMemo } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Column from 'Components/Table/Column';
import { GameFile } from 'GameFile/GameFile';
import { SortDirection } from 'Helpers/Props/sortDirections';
import {
  deleteGameFile,
  setGameFilesSort,
  setGameFilesTableOption,
} from 'Store/Actions/gameFileActions';
import {
  fetchLanguages,
  fetchQualityProfileSchema,
} from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import { TableOptionsChangePayload } from 'typings/Table';
import getQualities from 'Utilities/Quality/getQualities';
import GameFileEditorTableContent from './GameFileEditorTableContent';

interface GameFileEditorTableContentConnectorProps {
  gameId: number;
}

interface GameFilesCollectionResult {
  items: GameFile[];
  columns: Column[];
  sortKey: string;
  sortDirection: SortDirection;
  isDeleting: boolean;
  isSaving: boolean;
}

function createMapStateToProps(gameId: number) {
  return createSelector(
    createClientSideCollectionSelector('gameFiles'),
    (state: AppState) => state.settings.languages,
    (state: AppState) => state.settings.qualityProfiles,
    (gameFilesResult, languageProfiles, qualityProfiles) => {
      const gameFiles = gameFilesResult as unknown as GameFilesCollectionResult;
      const languages = languageProfiles.items;
      const qualities = getQualities(qualityProfiles.schema?.items ?? []);
      const filesForGame = gameFiles.items.filter(
        (file) => file.gameId === gameId
      );

      return {
        items: filesForGame,
        columns: gameFiles.columns,
        sortKey: gameFiles.sortKey,
        sortDirection: gameFiles.sortDirection,
        isDeleting: gameFiles.isDeleting,
        isSaving: gameFiles.isSaving,
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
