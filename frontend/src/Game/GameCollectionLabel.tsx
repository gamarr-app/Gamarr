import { useCallback } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import { toggleCollectionMonitored } from 'Store/Actions/gameCollectionActions';
import { createCollectionSelectorForHook } from 'Store/Selectors/createCollectionSelector';
import GameCollection from 'typings/GameCollection';
import translate from 'Utilities/String/translate';
import styles from './GameCollectionLabel.css';

interface GameCollectionLabelProps {
  igdbId: number;
}

function GameCollectionLabel({ igdbId }: GameCollectionLabelProps) {
  const {
    id,
    monitored,
    title,
    isSaving = false,
  } = useSelector(createCollectionSelectorForHook(igdbId)) ||
  ({} as GameCollection);

  const dispatch = useDispatch();

  const handleMonitorTogglePress = useCallback(
    (value: boolean) => {
      dispatch(
        toggleCollectionMonitored({ collectionId: id, monitored: value })
      );
    },
    [id, dispatch]
  );

  if (!id) {
    return translate('Unknown');
  }

  return (
    <div>
      <MonitorToggleButton
        className={styles.monitorToggleButton}
        monitored={monitored}
        isSaving={isSaving}
        size={15}
        onPress={handleMonitorTogglePress}
      />
      {title}
    </div>
  );
}

export default GameCollectionLabel;
