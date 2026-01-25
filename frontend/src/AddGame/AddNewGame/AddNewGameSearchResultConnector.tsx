import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { GameFile } from 'GameFile/GameFile';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createExistingGameSelector from 'Store/Selectors/createExistingGameSelector';
import AddNewGameSearchResult, {
  AddNewGameSearchResultProps,
} from './AddNewGameSearchResult';

// Props provided by the parent component (own props)
export type OwnProps = Omit<
  AddNewGameSearchResultProps,
  | 'existingGameId'
  | 'isExistingGame'
  | 'isSmallScreen'
  | 'gameFile'
  | 'gameRuntimeFormat'
> & {
  internalId?: number;
};

interface StateProps {
  existingGameId?: number;
  isExistingGame: boolean;
  isSmallScreen: boolean;
  gameFile?: GameFile;
  gameRuntimeFormat: string;
}

function createMapStateToProps() {
  return createSelector(
    createExistingGameSelector(),
    createDimensionsSelector(),
    (state: AppState) => state.gameFiles.items,
    (_state: AppState, { internalId }: OwnProps) => internalId,
    (state: AppState) => state.settings.ui.item.gameRuntimeFormat,
    (
      isExistingGame,
      dimensions,
      gameFiles,
      internalId,
      gameRuntimeFormat
    ): StateProps => {
      const gameFile = gameFiles.find(
        (item) => internalId && internalId > 0 && item.gameId === internalId
      );

      return {
        existingGameId: internalId,
        isExistingGame,
        isSmallScreen: dimensions.isSmallScreen,
        gameFile,
        gameRuntimeFormat,
      };
    }
  );
}

export default connect<StateProps, Record<string, never>, OwnProps, AppState>(
  createMapStateToProps
)(AddNewGameSearchResult);
