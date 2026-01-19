import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createGameFileSelector(gameFileId?: number) {
  return createSelector(
    (state: AppState) => state.gameFiles.items,
    (gameFiles) => {
      return gameFiles.find(({ id }) => id === gameFileId);
    }
  );
}

function useGameFile(gameFileId: number | undefined) {
  return useSelector(createGameFileSelector(gameFileId));
}

export default useGameFile;
