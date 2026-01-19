import PropTypes from 'prop-types';
import React from 'react';
import FormInputGroup from 'Components/Form/FormInputGroup';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import { inputTypes } from 'Helpers/Props';
import ImportGameSelectGameConnector from './SelectGame/ImportGameSelectGameConnector';
import styles from './ImportGameRow.css';

function ImportGameRow(props) {
  const {
    id,
    relativePath,
    monitor,
    qualityProfileId,
    minimumAvailability,
    selectedGame,
    isExistingGame,
    isSelected,
    onSelectedChange,
    onInputChange
  } = props;

  return (
    <>
      <VirtualTableSelectCell
        inputClassName={styles.selectInput}
        id={id}
        isSelected={isSelected}
        isDisabled={!selectedGame || isExistingGame}
        onSelectedChange={onSelectedChange}
      />

      <VirtualTableRowCell className={styles.folder}>
        {relativePath}
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.game}>
        <ImportGameSelectGameConnector
          id={id}
          isExistingGame={isExistingGame}
        />
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.monitor}>
        <FormInputGroup
          type={inputTypes.MONITOR_GAMES_SELECT}
          name="monitor"
          value={monitor}
          onChange={onInputChange}
        />
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.minimumAvailability}>
        <FormInputGroup
          type={inputTypes.AVAILABILITY_SELECT}
          name="minimumAvailability"
          value={minimumAvailability}
          onChange={onInputChange}
        />
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.qualityProfile}>
        <FormInputGroup
          type={inputTypes.QUALITY_PROFILE_SELECT}
          name="qualityProfileId"
          value={qualityProfileId}
          onChange={onInputChange}
        />
      </VirtualTableRowCell>
    </>
  );
}

ImportGameRow.propTypes = {
  id: PropTypes.string.isRequired,
  relativePath: PropTypes.string.isRequired,
  monitor: PropTypes.string.isRequired,
  qualityProfileId: PropTypes.number.isRequired,
  minimumAvailability: PropTypes.string.isRequired,
  selectedGame: PropTypes.object,
  isExistingGame: PropTypes.bool.isRequired,
  items: PropTypes.arrayOf(PropTypes.object).isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired,
  onInputChange: PropTypes.func.isRequired
};

ImportGameRow.defaultsProps = {
  items: []
};

export default ImportGameRow;
