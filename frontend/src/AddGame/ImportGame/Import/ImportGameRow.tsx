import { ImportGameSelectedGame } from 'App/State/ImportGameAppState';
import FormInputGroup from 'Components/Form/FormInputGroup';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import { inputTypes } from 'Helpers/Props';
import { InputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import ImportGameSelectGameConnector from './SelectGame/ImportGameSelectGameConnector';
import styles from './ImportGameRow.css';

interface ImportGameRowProps {
  id: string;
  relativePath: string;
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  selectedGame?: ImportGameSelectedGame;
  isExistingGame: boolean;
  items?: ImportGameSelectedGame[];
  isSelected?: boolean;
  onSelectedChange: (payload: SelectStateInputProps) => void;
  onInputChange: (change: InputChanged<string | number>) => void;
}

function ImportGameRow(props: ImportGameRowProps) {
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
    onInputChange,
    items: _items = [],
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
        <ImportGameSelectGameConnector id={id} />
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

export default ImportGameRow;
