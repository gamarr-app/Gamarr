import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import MetacriticRating from 'Components/MetacriticRating';
import { GameStatus, Ratings } from 'Game/Game';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import { icons } from 'Helpers/Props';
import formatRuntime from 'Utilities/Date/formatRuntime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import styles from './DiscoverGamePosterInfo.css';

interface DiscoverGamePosterInfoProps {
  status?: string;
  studio?: string;
  inCinemas?: string;
  digitalRelease?: string;
  physicalRelease?: string;
  certification?: string;
  runtime?: number;
  ratings: Ratings;
  sortKey: string;
  showRelativeDates: boolean;
  shortDateFormat: string;
  timeFormat: string;
  gameRuntimeFormat: string;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
}

function DiscoverGamePosterInfo(props: DiscoverGamePosterInfoProps) {
  const {
    status,
    studio,
    inCinemas,
    digitalRelease,
    physicalRelease,
    certification,
    runtime,
    ratings,
    sortKey,
    showRelativeDates,
    shortDateFormat,
    timeFormat,
    gameRuntimeFormat,
    showIgdbRating,
    showMetacriticRating,
  } = props;

  if (sortKey === 'status' && status) {
    return (
      <div className={styles.info} title={translate('Status')}>
        {getGameStatusDetails(status as GameStatus).title}
      </div>
    );
  }

  if (sortKey === 'studio' && studio) {
    return (
      <div className={styles.info} title={translate('Studio')}>
        {studio}
      </div>
    );
  }

  if (sortKey === 'inCinemas' && inCinemas) {
    const inCinemasDate = getRelativeDate({
      date: inCinemas,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div className={styles.info} title={translate('InDevelopment')}>
        <Icon name={icons.IN_CINEMAS} /> {inCinemasDate}
      </div>
    );
  }

  if (sortKey === 'digitalRelease' && digitalRelease) {
    const digitalReleaseDate = getRelativeDate({
      date: digitalRelease,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div className={styles.info} title={translate('DigitalRelease')}>
        <Icon name={icons.GAME_FILE} /> {digitalReleaseDate}
      </div>
    );
  }

  if (sortKey === 'physicalRelease' && physicalRelease) {
    const physicalReleaseDate = getRelativeDate({
      date: physicalRelease,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div className={styles.info} title={translate('PhysicalRelease')}>
        <Icon name={icons.DISC} /> {physicalReleaseDate}
      </div>
    );
  }

  if (sortKey === 'certification' && certification) {
    return (
      <div className={styles.info} title={translate('Certification')}>
        {certification}
      </div>
    );
  }

  if (sortKey === 'runtime' && runtime) {
    return (
      <div className={styles.info} title={translate('Runtime')}>
        {formatRuntime(runtime, gameRuntimeFormat)}
      </div>
    );
  }

  if (!showIgdbRating && sortKey === 'igdbRating' && !!ratings.igdb?.value) {
    return (
      <div className={styles.info}>
        <IgdbRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  if (
    !showMetacriticRating &&
    sortKey === 'metacriticRating' &&
    !!ratings.metacritic?.value
  ) {
    return (
      <div className={styles.info}>
        <MetacriticRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  return null;
}

export default DiscoverGamePosterInfo;
