import classNames from 'classnames';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import { GameStatus } from 'Game/Game';
import { GameFile } from 'GameFile/GameFile';
import getProgressBarKind from 'Utilities/Game/getProgressBarKind';
import translate from 'Utilities/String/translate';
import styles from './CollectionGameLabel.css';

interface CollectionGameLabelProps {
  id?: number;
  title: string;
  year: number;
  status?: GameStatus;
  isAvailable?: boolean;
  monitored?: boolean;
  hasFile?: boolean;
  isSaving?: boolean;
  gameFile?: GameFile;
  gameFileId?: number;
  onMonitorTogglePress: (monitored: boolean) => void;
}

function CollectionGameLabel(props: CollectionGameLabelProps) {
  const {
    id,
    title,
    year,
    status,
    monitored,
    isAvailable,
    hasFile,
    onMonitorTogglePress,
    isSaving = false,
  } = props;

  return (
    <div className={styles.game}>
      <div className={styles.gameTitle}>
        {id && monitored !== undefined && (
          <MonitorToggleButton
            monitored={monitored}
            isSaving={isSaving}
            onPress={onMonitorTogglePress}
          />
        )}

        <span>
          {title} {year > 0 ? `(${year})` : ''}
        </span>
      </div>

      {id && status && (
        <div
          className={classNames(
            styles.gameStatus,
            styles[
              getProgressBarKind(
                status,
                monitored ?? false,
                hasFile ?? false,
                isAvailable ?? false
              ) as keyof typeof styles
            ]
          )}
        >
          {hasFile ? translate('Downloaded') : translate('Missing')}
        </div>
      )}
    </div>
  );
}

export default CollectionGameLabel;
