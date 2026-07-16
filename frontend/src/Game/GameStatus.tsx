import { useMemo } from 'react';
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
  className?: string;
}

function GameStatus({
  gameId,
  gameFileId,
  showMissingStatus = true,
  className = styles.center,
}: GameStatusProps) {
  const game = useGame(gameId) as Game | undefined;

  const queueItem = useSelector(
    useMemo(() => createQueueItemSelectorForHook(gameId), [gameId])
  );
  const gameFile = useGameFile(gameFileId);

  // A Wanted/CutoffUnmet row can outlive its game: a SignalR delete event
  // removes the game from the store without refreshing those collections.
  if (!game) {
    return null;
  }

  const { isAvailable, monitored, grabbed = false } = game;

  const hasGameFile = !!gameFile;
  const isQueued = !!queueItem;

  if (isQueued) {
    const { sizeLeft, size } = queueItem;

    const progress = size ? 100 - (sizeLeft / size) * 100 : 0;

    return (
      <div className={className}>
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
      <div className={className}>
        <Icon name={icons.DOWNLOADING} title={translate('GameIsDownloading')} />
      </div>
    );
  }

  if (hasGameFile && showMissingStatus) {
    const quality = gameFile.quality;
    const isCutoffNotMet = gameFile.qualityCutoffNotMet;

    return (
      <div className={className}>
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
      <div className={className}>
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
      <div className={className}>
        <Icon name={icons.MISSING} title={translate('GameMissingFromDisk')} />
      </div>
    );
  }

  return (
    <div className={className}>
      <Icon name={icons.NOT_AIRED} title={translate('GameIsNotAvailable')} />
    </div>
  );
}

export default GameStatus;
