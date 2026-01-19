import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';

function createGameFileSelector() {
  return createSelector(
    (_: AppState, { gameFileId }: { gameFileId: number }) => gameFileId,
    (state: AppState) => state.gameFiles,
    (gameFileId, gameFiles) => {
      if (!gameFileId) {
        return;
      }

      return gameFiles.items.find((gameFile) => gameFile.id === gameFileId);
    }
  );
}

export default createGameFileSelector;
