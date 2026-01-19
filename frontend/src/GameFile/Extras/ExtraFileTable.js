import PropTypes from 'prop-types';
import React from 'react';
import ExtraFileTableContentConnector from './ExtraFileTableContentConnector';
import styles from './ExtraFileTable.css';

function ExtraFileTable(props) {
  const {
    gameId
  } = props;

  return (
    <div className={styles.container}>
      <ExtraFileTableContentConnector
        gameId={gameId}
      />
    </div>

  );
}

ExtraFileTable.propTypes = {
  gameId: PropTypes.number.isRequired
};

export default ExtraFileTable;
