import classNames from 'classnames';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import getProgressBarKind from 'Utilities/Game/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './CollectionGameLabel.css';

class CollectionGameLabel extends Component {
  //
  // Render

  render() {
    const {
      id,
      title,
      year,
      status,
      monitored,
      isAvailable,
      hasFile,
      onMonitorTogglePress,
      isSaving
    } = this.props;

    return (
      <div className={styles.game}>
        <div className={styles.gameTitle}>
          {
            id &&
              <MonitorToggleButton
                monitored={monitored}
                isSaving={isSaving}
                onPress={onMonitorTogglePress}
              />
          }

          <span>
            {title} {year > 0 ? `(${year})` : ''}
          </span>
        </div>

        {
          id &&
            <div
              className={classNames(
                styles.gameStatus,
                styles[getProgressBarKind(status, monitored, hasFile, isAvailable)]
              )}
            >
              {
                hasFile ? translate('Downloaded') : translate('Missing')
              }
            </div>
        }
      </div>
    );
  }
}

CollectionGameLabel.propTypes = {
  id: PropTypes.number,
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  status: PropTypes.string,
  isAvailable: PropTypes.bool,
  monitored: PropTypes.bool,
  hasFile: PropTypes.bool,
  isSaving: PropTypes.bool.isRequired,
  gameFile: PropTypes.object,
  gameFileId: PropTypes.number,
  onMonitorTogglePress: PropTypes.func.isRequired
};

CollectionGameLabel.defaultProps = {
  isSaving: false,
  statistics: {}
};

export default CollectionGameLabel;
