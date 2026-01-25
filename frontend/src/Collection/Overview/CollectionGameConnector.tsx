import React, { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { GameStatus, Image } from 'Game/Game';
import { toggleGameMonitored } from 'Store/Actions/gameActions';
import createCollectionExistingGameSelector from 'Store/Selectors/createCollectionExistingGameSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import CollectionGame from './CollectionGame';

interface CollectionGameConnectorProps {
  igdbId: number;
  title: string;
  year: number;
  status: GameStatus;
  overview?: string;
  collectionId: number;
  posterWidth: number;
  posterHeight: number;
  detailedProgressBar: boolean;
  images: Image[];
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
        gameFile: existingGame?.gameFile,
        steamAppId: existingGame?.steamAppId,
      };
    }
  );
}

function CollectionGameConnector(props: CollectionGameConnectorProps) {
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
    <CollectionGame
      {...props}
      {...state}
      onMonitorTogglePress={onMonitorTogglePress}
    />
  );
}

export default CollectionGameConnector;
