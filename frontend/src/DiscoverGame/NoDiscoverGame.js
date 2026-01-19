import PropTypes from 'prop-types';
import React from 'react';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoDiscoverGame.css';

function NoDiscoverGame(props) {
  const { totalItems } = props;

  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {translate('AllGamesHiddenDueToFilter')}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>
        {translate('NoListRecommendations')}
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/add/import"
          kind={kinds.PRIMARY}
        >
          {translate('ImportExistingGames')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/add/new"
          kind={kinds.PRIMARY}
        >
          {translate('AddNewGame')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button
          to="/settings/importlists"
          kind={kinds.PRIMARY}
        >
          {translate('AddImportList')}
        </Button>
      </div>
    </div>
  );
}

NoDiscoverGame.propTypes = {
  totalItems: PropTypes.number.isRequired
};

export default NoDiscoverGame;
