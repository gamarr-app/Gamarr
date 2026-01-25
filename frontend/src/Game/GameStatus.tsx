import React from 'react';
import { useSelector } from 'react-redux';
import QueueDetails from 'Activity/Queue/QueueDetails';
import Icon from 'Components/Icon';
import ProgressBar from 'Components/ProgressBar';
import Game from 'Game/Game';
import useGame, { GameEntity } from 'Game/useGame';
import useGameFile from 'GameFile/useGameFile';
import { icons, kinds } from 'Helpers/Props';
import { createQueueItemSelectorForHook } from 'Store/Selectors/createQueueItemSelector';
import translate from 'Utilities/String/translate';
import GameQuality from './GameQuality';
import styles from './GameStatus.css';

interface GameStatusProps {
  gameId: number;
  gameEntity?: GameEntity;
  gameFileId: number | undefined;
  showMissingStatus?: boolean;
}

function GameStatus({
  gameId,
  gameFileId,
  showMissingStatus = true,
}: GameStatusProps) {
  const { isAvailable, monitored, grabbed = false } = useGame(gameId) as Game;

  const queueItem = useSelector(createQueueItemSelectorForHook(gameId));
  const gameFile = useGameFile(gameFileId);

  const hasGameFile = !!gameFile;
  const isQueued = !!queueItem;

  if (isQueued) {
    const { sizeLeft, size } = queueItem;

    const progress = size ? 100 - (sizeLeft / size) * 100 : 0;

    return (
      <div className={styles.center}>
        <QueueDetails
          {...queueItem}
          progressBar={
            <ProgressBar
              progress={progress}
              title={`${progress.toFixed(1)}%`}
            />
          }
        />
      </div>
    );
  }

  if (grabbed && showMissingStatus) {
    return (
      <div className={styles.center}>
        <Icon name={icons.DOWNLOADING} title={translate('GameIsDownloading')} />
      </div>
    );
  }

  if (hasGameFile && showMissingStatus) {
    const quality = gameFile.quality;
    const isCutoffNotMet = gameFile.qualityCutoffNotMet;

    return (
      <div className={styles.center}>
        <GameQuality
          quality={quality}
          size={gameFile.size}
          isCutoffNotMet={isCutoffNotMet}
          title={translate('GameDownloaded')}
        />
      </div>
    );
  }

  if (!showMissingStatus) {
    return null;
  }

  if (!monitored) {
    return (
      <div className={styles.center}>
        <Icon
          name={icons.UNMONITORED}
          kind={kinds.DISABLED}
          title={translate('GameIsNotMonitored')}
        />
      </div>
    );
  }

  if (isAvailable) {
    return (
      <div className={styles.center}>
        <Icon name={icons.MISSING} title={translate('GameMissingFromDisk')} />
      </div>
    );
  }

  return (
    <div className={styles.center}>
      <Icon name={icons.NOT_AIRED} title={translate('GameIsNotAvailable')} />
    </div>
  );
}

export default GameStatus;
