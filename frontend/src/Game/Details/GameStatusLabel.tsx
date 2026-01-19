import React from 'react';
import { useSelector } from 'react-redux';
import Label from 'Components/Label';
import { kinds, sizes } from 'Helpers/Props';
import { Kind } from 'Helpers/Props/kinds';
import { GameStatus } from 'Game/Game';
import { createQueueItemSelectorForHook } from 'Store/Selectors/createQueueItemSelector';
import Queue from 'typings/Queue';
import getQueueStatusText from 'Utilities/Game/getQueueStatusText';
import firstCharToUpper from 'Utilities/String/firstCharToUpper';
import translate from 'Utilities/String/translate';
import styles from './GameStatusLabel.css';

function getGameStatus(
  status: GameStatus,
  isMonitored: boolean,
  isAvailable: boolean,
  hasFiles: boolean,
  queueItem: Queue | null = null
) {
  if (queueItem) {
    const queueStatus = queueItem.status;
    const queueState = queueItem.trackedDownloadStatus;
    const queueStatusText = getQueueStatusText(queueStatus, queueState);

    if (queueStatusText) {
      return queueStatusText;
    }
  }

  if (hasFiles && !isMonitored) {
    return 'availNotMonitored';
  }

  if (hasFiles) {
    return 'ended';
  }

  if (status === 'deleted') {
    return 'deleted';
  }

  if (isAvailable && !isMonitored && !hasFiles) {
    return 'missingUnmonitored';
  }

  if (isAvailable && !hasFiles) {
    return 'missingMonitored';
  }

  return 'continuing';
}

interface GameStatusLabelProps {
  gameId: number;
  monitored: boolean;
  isAvailable: boolean;
  hasGameFiles: boolean;
  status: GameStatus;
  useLabel?: boolean;
}

function GameStatusLabel({
  gameId,
  monitored,
  isAvailable,
  hasGameFiles,
  status,
  useLabel = false,
}: GameStatusLabelProps) {
  const queueItem = useSelector(createQueueItemSelectorForHook(gameId));

  let gameStatus = getGameStatus(
    status,
    monitored,
    isAvailable,
    hasGameFiles,
    queueItem
  );

  let statusClass = gameStatus;

  if (gameStatus === 'availNotMonitored' || gameStatus === 'ended') {
    gameStatus = 'downloaded';
  } else if (
    gameStatus === 'missingMonitored' ||
    gameStatus === 'missingUnmonitored'
  ) {
    gameStatus = 'missing';
  } else if (gameStatus === 'continuing') {
    gameStatus = 'notAvailable';
  }

  if (queueItem) {
    statusClass = 'queue';
  }

  if (useLabel) {
    let kind: Kind = kinds.SUCCESS;

    switch (statusClass) {
      case 'queue':
        kind = kinds.QUEUE;
        break;
      case 'missingMonitored':
        kind = kinds.DANGER;
        break;
      case 'continuing':
        kind = kinds.INFO;
        break;
      case 'availNotMonitored':
        kind = kinds.DEFAULT;
        break;
      case 'missingUnmonitored':
        kind = kinds.WARNING;
        break;
      case 'deleted':
        kind = kinds.INVERSE;
        break;
      default:
    }

    return (
      <Label kind={kind} size={sizes.LARGE}>
        {translate(firstCharToUpper(gameStatus))}
      </Label>
    );
  }

  return (
    <span
      // eslint-disable-next-line @typescript-eslint/ban-ts-comment
      // @ts-ignore
      className={styles[statusClass]}
    >
      {translate(firstCharToUpper(gameStatus))}
    </span>
  );
}

export default GameStatusLabel;
