import _ from 'lodash';
import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ImportGameItem } from 'App/State/ImportGameAppState';
import { setImportGameValue } from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { InputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import ImportGameRow from './ImportGameRow';

interface ImportGameRowConnectorProps {
  rootFolderId: number;
  id: string;
  isSelected?: boolean;
  onSelectedChange: (payload: SelectStateInputProps) => void;
}

function createImportGameItemSelector(id: string) {
  return createSelector(
    (state: AppState) => state.importGame.items,
    (items): Omit<Partial<ImportGameItem>, 'id'> => {
      const found = _.find(items, { id });
      if (!found) return {};
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { id: _id, ...rest } = found;
      return rest;
    }
  );
}

function createImportGameRowSelector(id: string) {
  return createSelector(
    createImportGameItemSelector(id),
    createAllGamesSelector(),
    (item, games) => {
      const selectedGame = item && item.selectedGame;
      const isExistingGame =
        !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

      return {
        ...item,
        isExistingGame,
      };
    }
  );
}

function ImportGameRowConnector(props: ImportGameRowConnectorProps) {
  const { id, isSelected, onSelectedChange } = props;

  const dispatch = useDispatch();

  const selector = createImportGameRowSelector(id);
  const {
    monitor,
    qualityProfileId,
    minimumAvailability,
    relativePath,
    isExistingGame,
    selectedGame,
  } = useSelector(selector);

  const onInputChange = useCallback(
    ({ name, value }: InputChanged) => {
      dispatch(
        setImportGameValue({
          id,
          [name]: value,
        })
      );
    },
    [dispatch, id]
  );

  // Don't show the row until we have the information we require for it.
  if (!monitor) {
    return null;
  }

  return (
    <ImportGameRow
      id={id}
      isSelected={isSelected}
      monitor={monitor || ''}
      qualityProfileId={qualityProfileId || 0}
      minimumAvailability={minimumAvailability || ''}
      relativePath={relativePath || ''}
      selectedGame={selectedGame}
      isExistingGame={isExistingGame || false}
      onSelectedChange={onSelectedChange}
      onInputChange={onInputChange}
    />
  );
}

export default ImportGameRowConnector;
