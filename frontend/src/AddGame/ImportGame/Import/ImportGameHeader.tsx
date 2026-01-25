import GameMinimumAvailabilityPopoverContent from 'AddGame/GameMinimumAvailabilityPopoverContent';
import Icon from 'Components/Icon';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import VirtualTableSelectAllHeaderCell from 'Components/Table/VirtualTableSelectAllHeaderCell';
import Popover from 'Components/Tooltip/Popover';
import { icons, tooltipPositions } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ImportGameHeader.css';

interface ImportGameHeaderProps {
  allSelected: boolean;
  allUnselected: boolean;
  onSelectAllChange: (payload: { value: boolean }) => void;
}

function ImportGameHeader(props: ImportGameHeaderProps) {
  const { allSelected, allUnselected, onSelectAllChange } = props;

  return (
    <VirtualTableHeader>
      <VirtualTableSelectAllHeaderCell
        allSelected={allSelected}
        allUnselected={allUnselected}
        onSelectAllChange={onSelectAllChange}
      />

      <VirtualTableHeaderCell className={styles.folder} name="folder">
        {translate('Folder')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell className={styles.game} name="game">
        {translate('Game')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell className={styles.monitor} name="monitor">
        {translate('Monitor')}
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell
        className={styles.minimumAvailability}
        name="minimumAvailability"
      >
        {translate('MinimumAvailability')}

        <Popover
          anchor={<Icon className={styles.detailsIcon} name={icons.INFO} />}
          title={translate('MinimumAvailability')}
          body={<GameMinimumAvailabilityPopoverContent />}
          position={tooltipPositions.LEFT}
        />
      </VirtualTableHeaderCell>

      <VirtualTableHeaderCell
        className={styles.qualityProfile}
        name="qualityProfileId"
      >
        {translate('QualityProfile')}
      </VirtualTableHeaderCell>
    </VirtualTableHeader>
  );
}

export default ImportGameHeader;
