import React from 'react';
import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoGameCollections.css';

interface NoGameCollectionsProps {
  totalItems: number;
}

function NoGameCollections({ totalItems }: NoGameCollectionsProps) {
  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {translate('AllCollectionsHiddenDueToFilter')}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>{translate('NoCollections')}</div>

      <div className={styles.buttonContainer}>
        <Button to="/add/import" kind={kinds.PRIMARY}>
          {translate('ImportExistingGames')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button to="/add/new" kind={kinds.PRIMARY}>
          {translate('AddNewGame')}
        </Button>
      </div>
    </div>
  );
}

export default NoGameCollections;
