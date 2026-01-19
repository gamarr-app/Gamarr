import PropTypes from 'prop-types';
import React from 'react';
import Icon from 'Components/Icon';
import ImdbRating from 'Components/ImdbRating';
import RottenTomatoRating from 'Components/RottenTomatoRating';
import IgdbRating from 'Components/IgdbRating';
import TraktRating from 'Components/TraktRating';
import { icons } from 'Helpers/Props';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import formatRuntime from 'Utilities/Date/formatRuntime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import styles from './DiscoverGamePosterInfo.css';

function DiscoverGamePosterInfo(props) {
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
    showImdbRating,
    showRottenTomatoesRating,
    showTraktRating
  } = props;

  if (sortKey === 'status' && status) {
    return (
      <div className={styles.info} title={translate('Status')}>
        {getGameStatusDetails(status).title}
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
      timeForToday: false
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
      timeForToday: false
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
      timeForToday: false
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

  if (!showIgdbRating && sortKey === 'igdbRating' && !!ratings.igdb) {
    return (
      <div className={styles.info}>
        <IgdbRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  if (!showImdbRating && sortKey === 'imdbRating' && !!ratings.imdb) {
    return (
      <div className={styles.info}>
        <ImdbRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  if (!showRottenTomatoesRating && sortKey === 'rottenTomatoesRating' && !!ratings.rottenTomatoes) {
    return (
      <div className={styles.info}>
        <RottenTomatoRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  if (!showTraktRating && sortKey === 'traktRating' && !!ratings.trakt) {
    return (
      <div className={styles.info}>
        <TraktRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  return null;
}

DiscoverGamePosterInfo.propTypes = {
  status: PropTypes.string,
  studio: PropTypes.string,
  inCinemas: PropTypes.string,
  certification: PropTypes.string,
  digitalRelease: PropTypes.string,
  physicalRelease: PropTypes.string,
  runtime: PropTypes.number,
  ratings: PropTypes.object.isRequired,
  sortKey: PropTypes.string.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  gameRuntimeFormat: PropTypes.string.isRequired,
  showIgdbRating: PropTypes.bool.isRequired,
  showImdbRating: PropTypes.bool.isRequired,
  showRottenTomatoesRating: PropTypes.bool.isRequired,
  showTraktRating: PropTypes.bool.isRequired
};

export default DiscoverGamePosterInfo;
