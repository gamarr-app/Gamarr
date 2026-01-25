import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { GameStatus } from 'Game/Game';
import { toggleGameMonitored } from 'Store/Actions/gameActions';
import createCollectionExistingGameSelector from 'Store/Selectors/createCollectionExistingGameSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import CollectionGameLabel from './CollectionGameLabel';

interface CollectionGameLabelConnectorProps {
  igdbId: number;
  title: string;
  year: number;
  status?: GameStatus;
  collectionId: number;
}

function createMapStateToProps(igdbId: number) {
  return createSelector(
    createDimensionsSelector(),
    (state: AppState) => {
      const existingGameSelector = createCollectionExistingGameSelector();
      return existingGameSelector(state, { igdbId });
    },
    (dimensions, existingGame) => {
      return {
        isSmallScreen: dimensions.isSmallScreen,
        isExistingGame: !!existingGame,
        id: existingGame?.id,
        monitored: existingGame?.monitored,
        hasFile: existingGame?.hasFile,
        isAvailable: existingGame?.isAvailable,
        isSaving: existingGame?.isSaving ?? false,
      };
    }
  );
}

function CollectionGameLabelConnector(
  props: CollectionGameLabelConnectorProps
) {
  const dispatch = useDispatch();
  const mapStateSelector = createMapStateToProps(props.igdbId);
  const state = useSelector(mapStateSelector);

  const onMonitorTogglePress = useCallback(
    (monitored: boolean) => {
      if (state.id) {
        dispatch(
          toggleGameMonitored({
            gameId: state.id,
            monitored,
          })
        );
      }
    },
    [dispatch, state.id]
  );

  return (
    <CollectionGameLabel
      {...props}
      {...state}
      onMonitorTogglePress={onMonitorTogglePress}
    />
  );
}

export default CollectionGameLabelConnector;
