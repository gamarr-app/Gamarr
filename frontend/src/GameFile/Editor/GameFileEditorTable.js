import PropTypes from 'prop-types';
import React from 'react';
import GameFileEditorTableContentConnector from './GameFileEditorTableContentConnector';
import styles from './GameFileEditorTable.css';

function GameFileEditorTable(props) {
  const {
    gameId
  } = props;

  return (
    <div className={styles.container}>
      <GameFileEditorTableContentConnector
        gameId={gameId}
      />
    </div>
  );
}

GameFileEditorTable.propTypes = {
  gameId: PropTypes.number.isRequired
};

export default GameFileEditorTable;
