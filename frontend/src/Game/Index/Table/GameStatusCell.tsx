import { useCallback } from 'react';
import { useDispatch } from 'react-redux';
import Icon from 'Components/Icon';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import VirtualTableRowCell from 'Components/Table/Cells/TableRowCell';
import { GameStatus } from 'Game/Game';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import { icons } from 'Helpers/Props';
import { toggleGameMonitored } from 'Store/Actions/gameActions';
import translate from 'Utilities/String/translate';
import styles from './GameStatusCell.css';

interface GameStatusCellProps {
  className: string;
  gameId: number;
  monitored: boolean;
  status: GameStatus;
  isSelectMode: boolean;
  isSaving: boolean;
  component?: React.ElementType;
}

function GameStatusCell(props: GameStatusCellProps) {
  const {
    className,
    gameId,
    monitored,
    status,
    isSelectMode,
    isSaving,
    component: Component = VirtualTableRowCell,
    ...otherProps
  } = props;

  const statusDetails = getGameStatusDetails(status);

  const dispatch = useDispatch();

  const onMonitoredPress = useCallback(() => {
    dispatch(toggleGameMonitored({ gameId, monitored: !monitored }));
  }, [gameId, monitored, dispatch]);

  return (
    <Component className={className} {...otherProps}>
      {isSelectMode ? (
        <MonitorToggleButton
          className={styles.statusIcon}
          monitored={monitored}
          isSaving={isSaving}
          onPress={onMonitoredPress}
        />
      ) : (
        <Icon
          className={styles.statusIcon}
          name={monitored ? icons.MONITORED : icons.UNMONITORED}
          title={
            monitored
              ? translate('GameIsMonitored')
              : translate('GameIsUnmonitored')
          }
        />
      )}

      <Icon
        className={styles.statusIcon}
        name={statusDetails.icon}
        title={`${statusDetails.title}: ${statusDetails.message}`}
      />
    </Component>
  );
}

export default GameStatusCell;
