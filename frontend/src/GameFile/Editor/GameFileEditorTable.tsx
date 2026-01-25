import React from 'react';
import GameFileEditorTableContentConnector from './GameFileEditorTableContentConnector';
import styles from './GameFileEditorTable.css';

interface GameFileEditorTableProps {
  gameId: number;
}

function GameFileEditorTable(props: GameFileEditorTableProps) {
  const { gameId } = props;

  return (
    <div className={styles.container}>
      <GameFileEditorTableContentConnector gameId={gameId} />
    </div>
  );
}

export default GameFileEditorTable;
