import React from 'react';
import GameTagList from 'Components/GameTagList';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import MetacriticRating from 'Components/MetacriticRating';
import { Ratings } from 'Game/Game';
import { icons } from 'Helpers/Props';
import Language from 'Language/Language';
import QualityProfile from 'typings/QualityProfile';
import formatDate from 'Utilities/Date/formatDate';
import formatDateTime from 'Utilities/Date/formatDateTime';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';
import styles from './GameIndexPosterInfo.css';

interface GameIndexPosterInfoProps {
  studio?: string;
  showQualityProfile: boolean;
  qualityProfile?: QualityProfile;
  added?: string;
  year: number;
  inCinemas?: string;
  digitalRelease?: string;
  physicalRelease?: string;
  releaseDate?: string;
  path: string;
  ratings: Ratings;
  certification: string;
  originalTitle: string;
  originalLanguage: Language;
  sizeOnDisk?: number;
  tags: number[];
  sortKey: string;
  showRelativeDates: boolean;
  showCinemaRelease: boolean;
  showDigitalRelease: boolean;
  showPhysicalRelease: boolean;
  showReleaseDate: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showTags: boolean;
}

function GameIndexPosterInfo(props: GameIndexPosterInfoProps) {
  const {
    studio,
    showQualityProfile,
    qualityProfile,
    added,
    year,
    inCinemas,
    digitalRelease,
    physicalRelease,
    releaseDate,
    path,
    ratings,
    certification,
    originalTitle,
    originalLanguage,
    sizeOnDisk = 0,
    tags = [],
    sortKey,
    showRelativeDates,
    showCinemaRelease,
    showDigitalRelease,
    showPhysicalRelease,
    showReleaseDate,
    shortDateFormat,
    longDateFormat,
    timeFormat,
    showIgdbRating,
    showMetacriticRating,
    showTags,
  } = props;

  if (sortKey === 'studio' && studio) {
    return (
      <div className={styles.info} title={translate('Studio')}>
        {studio}
      </div>
    );
  }

  if (
    sortKey === 'qualityProfileId' &&
    !showQualityProfile &&
    !!qualityProfile?.name
  ) {
    return (
      <div className={styles.info} title={translate('QualityProfile')}>
        {qualityProfile.name}
      </div>
    );
  }

  if (sortKey === 'added' && added) {
    const addedDate = getRelativeDate({
      date: added,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div
        className={styles.info}
        title={formatDateTime(added, longDateFormat, timeFormat)}
      >
        {translate('Added')}: {addedDate}
      </div>
    );
  }

  if (sortKey === 'year' && year) {
    return (
      <div className={styles.info} title={translate('Year')}>
        {year}
      </div>
    );
  }

  if (sortKey === 'inCinemas' && inCinemas && !showCinemaRelease) {
    const inCinemasDate = getRelativeDate({
      date: inCinemas,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div
        className={styles.info}
        title={`${translate('InDevelopment')}: ${formatDate(
          inCinemas,
          longDateFormat
        )}`}
      >
        <Icon name={icons.IN_CINEMAS} /> {inCinemasDate}
      </div>
    );
  }

  if (sortKey === 'digitalRelease' && digitalRelease && !showDigitalRelease) {
    const digitalReleaseDate = getRelativeDate({
      date: digitalRelease,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div
        className={styles.info}
        title={`${translate('DigitalRelease')}: ${formatDate(
          digitalRelease,
          longDateFormat
        )}`}
      >
        <Icon name={icons.GAME_FILE} /> {digitalReleaseDate}
      </div>
    );
  }

  if (
    sortKey === 'physicalRelease' &&
    physicalRelease &&
    !showPhysicalRelease
  ) {
    const physicalReleaseDate = getRelativeDate({
      date: physicalRelease,
      shortDateFormat,
      showRelativeDates,
      timeFormat,
      timeForToday: false,
    });

    return (
      <div
        className={styles.info}
        title={`${translate('PhysicalRelease')}: ${formatDate(
          physicalRelease,
          longDateFormat
        )}`}
      >
        <Icon name={icons.DISC} /> {physicalReleaseDate}
      </div>
    );
  }

  if (sortKey === 'releaseDate' && releaseDate && !showReleaseDate) {
    return (
      <div
        className={styles.info}
        title={`${translate('ReleaseDate')}: ${formatDate(
          releaseDate,
          longDateFormat
        )}`}
      >
        <Icon name={icons.CALENDAR} />{' '}
        {getRelativeDate({
          date: releaseDate,
          shortDateFormat,
          showRelativeDates,
          timeFormat,
          timeForToday: false,
        })}
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

  if (
    !showMetacriticRating &&
    sortKey === 'metacriticRating' &&
    !!ratings.metacritic
  ) {
    return (
      <div className={styles.info}>
        <MetacriticRating ratings={ratings} iconSize={12} />
      </div>
    );
  }

  if (!showTags && sortKey === 'tags' && tags.length) {
    return (
      <div className={styles.tags}>
        <div className={styles.tagsList}>
          <GameTagList tags={tags} />
        </div>
      </div>
    );
  }

  if (sortKey === 'path') {
    return (
      <div className={styles.info} title={translate('Path')}>
        {path}
      </div>
    );
  }

  if (sortKey === 'sizeOnDisk') {
    return (
      <div className={styles.info} title={translate('SizeOnDisk')}>
        {formatBytes(sizeOnDisk)}
      </div>
    );
  }

  if (sortKey === 'certification') {
    return <div className={styles.info}>{certification}</div>;
  }

  if (sortKey === 'originalTitle' && originalTitle) {
    return (
      <div className={styles.title} title={originalTitle}>
        {originalTitle}
      </div>
    );
  }

  if (sortKey === 'originalLanguage' && originalLanguage) {
    return (
      <div className={styles.info} title={translate('OriginalLanguage')}>
        {originalLanguage.name}
      </div>
    );
  }

  return null;
}

export default GameIndexPosterInfo;
