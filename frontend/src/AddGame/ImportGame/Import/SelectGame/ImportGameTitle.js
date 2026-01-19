import PropTypes from 'prop-types';
import React from 'react';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ImportGameTitle.css';

function ImportGameTitle(props) {
  const {
    title,
    year,
    studio,
    isExistingGame
  } = props;

  return (
    <div className={styles.titleContainer}>
      <div className={styles.title}>
        {title}

        {
          !title.contains(year) &&
            <span className={styles.year}>({year})</span>
        }
      </div>

      {
        !!studio &&
          <Label>{studio}</Label>
      }

      {
        isExistingGame &&
          <Label
            kind={kinds.WARNING}
          >
            {translate('Existing')}
          </Label>
      }
    </div>
  );
}

ImportGameTitle.propTypes = {
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  studio: PropTypes.string,
  isExistingGame: PropTypes.bool.isRequired
};

export default ImportGameTitle;
