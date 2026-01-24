import React, { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { GAME_SEARCH, REFRESH_GAME } from 'Commands/commandNames';
import GameTagList from 'Components/GameTagList';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import MetacriticRating from 'Components/MetacriticRating';
import Popover from 'Components/Tooltip/Popover';
import DeleteGameModal from 'Game/Delete/DeleteGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import EditGameModal from 'Game/Edit/EditGameModal';
import { Statistics } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import GameIndexProgressBar from 'Game/Index/ProgressBar/GameIndexProgressBar';
import GameIndexPosterSelect from 'Game/Index/Select/GameIndexPosterSelect';
import { icons } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import formatDate from 'Utilities/Date/formatDate';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import createGameIndexItemSelector from '../createGameIndexItemSelector';
import GameIndexPosterInfo from './GameIndexPosterInfo';
import selectPosterOptions from './selectPosterOptions';
import styles from './GameIndexPoster.css';

interface GameIndexPosterProps {
  gameId: number;
  sortKey: string;
  isSelectMode: boolean;
  posterWidth: number;
  posterHeight: number;
}

function GameIndexPoster(props: GameIndexPosterProps) {
  const { gameId, sortKey, isSelectMode, posterWidth, posterHeight } = props;

  const { game, qualityProfile, isRefreshingGame, isSearchingGame } =
    useSelector(createGameIndexItemSelector(props.gameId));

  const {
    detailedProgressBar,
    showTitle,
    showMonitored,
    showQualityProfile,
    showCinemaRelease,
    showDigitalRelease,
    showPhysicalRelease,
    showReleaseDate,
    showIgdbRating,
    showMetacriticRating,
    showTags,
    showSearchAction,
  } = useSelector(selectPosterOptions);

  const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
    useSelector(createUISettingsSelector());

  const {
    title,
    monitored,
    status,
    images,
    titleSlug,
    steamAppId,
    igdbSlug,
    youTubeTrailerId,
    hasFile,
    isAvailable,
    studio,
    added,
    year,
    inCinemas,
    physicalRelease,
    digitalRelease,
    releaseDate,
    path,
    gameFile,
    ratings,
    statistics = {} as Statistics,
    certification,
    originalTitle,
    originalLanguage,
    tags = [],
    gameType,
    gameTypeDisplayName,
  } = game;

  const { sizeOnDisk = 0 } = statistics;

  const dispatch = useDispatch();
  const [hasPosterError, setHasPosterError] = useState(false);
  const [isEditGameModalOpen, setIsEditGameModalOpen] = useState(false);
  const [isDeleteGameModalOpen, setIsDeleteGameModalOpen] = useState(false);

  const onRefreshPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: REFRESH_GAME,
        gameIds: [gameId],
      })
    );
  }, [gameId, dispatch]);

  const onSearchPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: GAME_SEARCH,
        gameIds: [gameId],
      })
    );
  }, [gameId, dispatch]);

  const onPosterLoadError = useCallback(() => {
    setHasPosterError(true);
  }, [setHasPosterError]);

  const onPosterLoad = useCallback(() => {
    setHasPosterError(false);
  }, [setHasPosterError]);

  const onEditGamePress = useCallback(() => {
    setIsEditGameModalOpen(true);
  }, [setIsEditGameModalOpen]);

  const onEditGameModalClose = useCallback(() => {
    setIsEditGameModalOpen(false);
  }, [setIsEditGameModalOpen]);

  const onDeleteGamePress = useCallback(() => {
    setIsEditGameModalOpen(false);
    setIsDeleteGameModalOpen(true);
  }, [setIsDeleteGameModalOpen]);

  const onDeleteGameModalClose = useCallback(() => {
    setIsDeleteGameModalOpen(false);
  }, [setIsDeleteGameModalOpen]);

  const link = `/game/${titleSlug}`;

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  return (
    <div className={styles.content}>
      <div className={styles.posterContainer} title={title}>
        {isSelectMode ? (
          <GameIndexPosterSelect gameId={gameId} titleSlug={titleSlug} />
        ) : null}

        <Label className={styles.controls}>
          <SpinnerIconButton
            name={icons.REFRESH}
            title={translate('RefreshGame')}
            isSpinning={isRefreshingGame}
            onPress={onRefreshPress}
          />

          {showSearchAction ? (
            <SpinnerIconButton
              className={styles.action}
              name={icons.SEARCH}
              title={translate('SearchForGame')}
              isSpinning={isSearchingGame}
              onPress={onSearchPress}
            />
          ) : null}

          <IconButton
            name={icons.EDIT}
            title={translate('EditGame')}
            onPress={onEditGamePress}
          />

          <span className={styles.externalLinks}>
            <Popover
              anchor={<Icon name={icons.EXTERNAL_LINK} size={12} />}
              title={translate('Links')}
              body={
                <GameDetailsLinks
                  steamAppId={steamAppId}
                  igdbSlug={igdbSlug}
                  youTubeTrailerId={youTubeTrailerId}
                />
              }
            />
          </span>
        </Label>

        {status === 'deleted' ? (
          <div className={styles.deleted} title={translate('Deleted')} />
        ) : null}

        {gameType && gameType !== 'mainGame' ? (
          <div className={styles.gameTypeBadge}>
            {gameTypeDisplayName}
          </div>
        ) : null}

        <Link className={styles.link} style={elementStyle} to={link}>
          <GamePoster
            className={styles.poster}
            style={elementStyle}
            images={images}
            size={250}
            lazy={false}
            overflow={true}
            onError={onPosterLoadError}
            onLoad={onPosterLoad}
          />

          {hasPosterError ? (
            <div className={styles.overlayTitle}>{title}</div>
          ) : null}
        </Link>
      </div>

      <GameIndexProgressBar
        gameId={gameId}
        gameFile={gameFile}
        monitored={monitored}
        hasFile={hasFile}
        isAvailable={isAvailable}
        status={status}
        width={posterWidth}
        detailedProgressBar={detailedProgressBar}
        bottomRadius={false}
      />

      {showTitle ? (
        <div className={styles.title} title={title}>
          {title}
        </div>
      ) : null}

      {showMonitored ? (
        <div className={styles.title}>
          {monitored ? translate('Monitored') : translate('Unmonitored')}
        </div>
      ) : null}

      {showQualityProfile && !!qualityProfile?.name ? (
        <div className={styles.title} title={translate('QualityProfile')}>
          {qualityProfile.name}
        </div>
      ) : null}

      {showCinemaRelease && inCinemas ? (
        <div
          className={styles.title}
          title={`${translate('InDevelopment')}: ${formatDate(
            inCinemas,
            longDateFormat
          )}`}
        >
          <Icon name={icons.IN_CINEMAS} />{' '}
          {getRelativeDate({
            date: inCinemas,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}

      {showDigitalRelease && digitalRelease ? (
        <div
          className={styles.title}
          title={`${translate('DigitalRelease')}: ${formatDate(
            digitalRelease,
            longDateFormat
          )}`}
        >
          <Icon name={icons.GAME_FILE} />{' '}
          {getRelativeDate({
            date: digitalRelease,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}

      {showPhysicalRelease && physicalRelease ? (
        <div
          className={styles.title}
          title={`${translate('PhysicalRelease')}: ${formatDate(
            physicalRelease,
            longDateFormat
          )}`}
        >
          <Icon name={icons.DISC} />{' '}
          {getRelativeDate({
            date: physicalRelease,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}

      {showReleaseDate && releaseDate ? (
        <div
          className={styles.title}
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
      ) : null}

      {showIgdbRating && !!ratings.igdb?.value ? (
        <div className={styles.title}>
          <IgdbRating ratings={ratings} iconSize={12} />
        </div>
      ) : null}

      {showMetacriticRating && !!ratings.metacritic?.value ? (
        <div className={styles.title}>
          <MetacriticRating ratings={ratings} iconSize={12} />
        </div>
      ) : null}

      {showTags && tags.length ? (
        <div className={styles.tags}>
          <div className={styles.tagsList}>
            <GameTagList tags={tags} />
          </div>
        </div>
      ) : null}

      <GameIndexPosterInfo
        studio={studio}
        qualityProfile={qualityProfile}
        added={added}
        year={year}
        showQualityProfile={showQualityProfile}
        showCinemaRelease={showCinemaRelease}
        showDigitalRelease={showDigitalRelease}
        showPhysicalRelease={showPhysicalRelease}
        showReleaseDate={showReleaseDate}
        showRelativeDates={showRelativeDates}
        shortDateFormat={shortDateFormat}
        longDateFormat={longDateFormat}
        timeFormat={timeFormat}
        inCinemas={inCinemas}
        physicalRelease={physicalRelease}
        digitalRelease={digitalRelease}
        releaseDate={releaseDate}
        ratings={ratings}
        sizeOnDisk={sizeOnDisk}
        sortKey={sortKey}
        path={path}
        certification={certification}
        originalTitle={originalTitle}
        originalLanguage={originalLanguage}
        tags={tags}
        showIgdbRating={showIgdbRating}
        showMetacriticRating={showMetacriticRating}
        showTags={showTags}
      />

      <EditGameModal
        isOpen={isEditGameModalOpen}
        gameId={gameId}
        onModalClose={onEditGameModalClose}
        onDeleteGamePress={onDeleteGamePress}
      />

      <DeleteGameModal
        isOpen={isDeleteGameModalOpen}
        gameId={gameId}
        onModalClose={onDeleteGameModalClose}
      />
    </div>
  );
}

export default GameIndexPoster;
