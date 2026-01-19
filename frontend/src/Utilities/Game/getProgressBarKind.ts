import { GameStatus } from 'Game/Game';
import { kinds } from 'Helpers/Props';

function getProgressBarKind(
  status: GameStatus,
  monitored: boolean,
  hasFile: boolean,
  isAvailable: boolean,
  isDownloading: boolean = false
) {
  if (isDownloading) {
    return kinds.PURPLE;
  }

  if (hasFile && monitored) {
    return kinds.SUCCESS;
  }

  if (hasFile && !monitored) {
    return kinds.DEFAULT;
  }

  if (status === 'deleted') {
    return kinds.INVERSE;
  }

  if (isAvailable && monitored) {
    return kinds.DANGER;
  }

  if (!monitored) {
    return kinds.WARNING;
  }

  return kinds.PRIMARY;
}

export default getProgressBarKind;
