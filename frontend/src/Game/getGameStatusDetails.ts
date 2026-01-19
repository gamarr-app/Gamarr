import { icons } from 'Helpers/Props';
import { GameStatus } from 'Game/Game';
import translate from 'Utilities/String/translate';

export default function getGameStatusDetails(status: GameStatus) {
  let statusDetails = {
    icon: icons.ANNOUNCED,
    title: translate('Announced'),
    message: translate('AnnouncedGameDescription'),
  };

  if (status === 'deleted') {
    statusDetails = {
      icon: icons.GAME_DELETED,
      title: translate('Deleted'),
      message: translate('DeletedGameDescription'),
    };
  } else if (status === 'inCinemas') {
    statusDetails = {
      icon: icons.IN_CINEMAS,
      title: translate('InDevelopment'),
      message: translate('InDevelopmentGameDescription'),
    };
  } else if (status === 'released') {
    statusDetails = {
      icon: icons.GAME_FILE,
      title: translate('Released'),
      message: translate('ReleasedGameDescription'),
    };
  }

  return statusDetails;
}
