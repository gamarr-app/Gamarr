import React, { useMemo } from 'react';
import { useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ExtraFile } from 'GameFile/ExtraFile';
import ExtraFileTableContent from './ExtraFileTableContent';

interface ExtraFileTableContentConnectorProps {
  gameId: number;
}

interface ExtraFileWithDetails extends ExtraFile {
  title?: string;
  languageTags?: string[];
}

function createMapStateToProps(gameId: number) {
  return createSelector(
    (state: AppState) => state.extraFiles,
    (extraFiles) => {
      const filesForGame = extraFiles.items.filter(
        (file) => file.gameId === gameId
      ) as ExtraFileWithDetails[];

      return {
        items: filesForGame,
        error: null,
      };
    }
  );
}

function ExtraFileTableContentConnector(
  props: ExtraFileTableContentConnectorProps
) {
  const { gameId } = props;

  const selector = useMemo(() => createMapStateToProps(gameId), [gameId]);
  const { items } = useSelector(selector);

  return <ExtraFileTableContent gameId={gameId} items={items} />;
}

export default ExtraFileTableContentConnector;
