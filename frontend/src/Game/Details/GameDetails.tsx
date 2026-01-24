import React, {
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useHistory } from 'react-router';
import TextTruncate from 'react-text-truncate';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import * as commandNames from 'Commands/commandNames';
import Alert from 'Components/Alert';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import InfoLabel from 'Components/InfoLabel';
import IconButton from 'Components/Link/IconButton';
import Marquee from 'Components/Marquee';
import MetacriticRating from 'Components/MetacriticRating';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import PageToolbarSeparator from 'Components/Page/Toolbar/PageToolbarSeparator';
import Popover from 'Components/Tooltip/Popover';
import Tooltip from 'Components/Tooltip/Tooltip';
import DeleteGameModal from 'Game/Delete/DeleteGameModal';
import EditGameModal from 'Game/Edit/EditGameModal';
import { Image, Statistics } from 'Game/Game';
import GameCollectionLabel from 'Game/GameCollectionLabel';
import GameGenres from 'Game/GameGenres';
import GamePoster from 'Game/GamePoster';
import GameStatus from 'Game/GameStatus';
import getGameStatusDetails from 'Game/getGameStatusDetails';
import GameHistoryModal from 'Game/History/GameHistoryModal';
import GameInteractiveSearchModal from 'Game/Search/GameInteractiveSearchModal';
import useGame from 'Game/useGame';
import GameFileEditorTable from 'GameFile/Editor/GameFileEditorTable';
import ExtraFileTable from 'GameFile/Extras/ExtraFileTable';
import useMeasure from 'Helpers/Hooks/useMeasure';
import usePrevious from 'Helpers/Hooks/usePrevious';
import {
  icons,
  kinds,
  sizes,
  sortDirections,
  tooltipPositions,
} from 'Helpers/Props';
import InteractiveImportModal from 'InteractiveImport/InteractiveImportModal';
import OrganizePreviewModal from 'Organize/OrganizePreviewModal';
import QualityProfileName from 'Settings/Profiles/Quality/QualityProfileName';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  clearExtraFiles,
  fetchExtraFiles,
} from 'Store/Actions/extraFileActions';
import { toggleGameMonitored } from 'Store/Actions/gameActions';
import { clearGameFiles, fetchGameFiles } from 'Store/Actions/gameFileActions';
import {
  clearQueueDetails,
  fetchQueueDetails,
} from 'Store/Actions/queueActions';
import {
  cancelFetchReleases,
  clearReleases,
} from 'Store/Actions/releaseActions';
import { fetchImportListSchema } from 'Store/Actions/Settings/importLists';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import createCommandsSelector from 'Store/Selectors/createCommandsSelector';
import createDimensionsSelector from 'Store/Selectors/createDimensionsSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import fonts from 'Styles/Variables/fonts';
import sortByProp from 'Utilities/Array/sortByProp';
import { findCommand, isCommandExecuting } from 'Utilities/Command';
import formatRuntime from 'Utilities/Date/formatRuntime';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import formatBytes from 'Utilities/Number/formatBytes';
import {
  registerPagePopulator,
  unregisterPagePopulator,
} from 'Utilities/pagePopulator';
import translate from 'Utilities/String/translate';
import DlcList from './Dlc/DlcList';
import GameDetailsLinks from './GameDetailsLinks';
import GameReleaseDates from './GameReleaseDates';
import GameStatusLabel from './GameStatusLabel';
import GameTags from './GameTags';
import GameTitlesTable from './Titles/GameTitlesTable';
import styles from './GameDetails.css';

const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

function getFanartUrl(images: Image[]) {
  const image = images.find((image) => image.coverType === 'fanart');
  return image?.url ?? image?.remoteUrl;
}

function createGameFilesSelector() {
  return createSelector(
    (state: AppState) => state.gameFiles,
    ({ items, isFetching, isPopulated, error }) => {
      const hasGameFiles = !!items.length;

      return {
        isGameFilesFetching: isFetching,
        isGameFilesPopulated: isPopulated,
        gameFilesError: error,
        hasGameFiles,
      };
    }
  );
}

function createExtraFilesSelector() {
  return createSelector(
    (state: AppState) => state.extraFiles,
    ({ isFetching, isPopulated, error }) => {
      return {
        isExtraFilesFetching: isFetching,
        isExtraFilesPopulated: isPopulated,
        extraFilesError: error,
      };
    }
  );
}


interface GameDetailsProps {
  gameId: number;
}

function GameDetails({ gameId }: GameDetailsProps) {
  const dispatch = useDispatch();
  const history = useHistory();

  const game = useGame(gameId);
  const allGames = useSelector(createAllGamesSelector());

  const { isGameFilesFetching, gameFilesError, hasGameFiles } = useSelector(
    createGameFilesSelector()
  );
  const { isExtraFilesFetching, extraFilesError } = useSelector(
    createExtraFilesSelector()
  );
  const { gameRuntimeFormat } = useSelector(createUISettingsSelector());
  const isSidebarVisible = useSelector(
    (state: AppState) => state.app.isSidebarVisible
  );
  const { isSmallScreen } = useSelector(createDimensionsSelector());

  const commands = useSelector(createCommandsSelector());

  const { isRefreshing, isRenaming, isSearching } = useMemo(() => {
    const gameRefreshingCommand = findCommand(commands, {
      name: commandNames.REFRESH_GAME,
    });

    const isGameRefreshingCommandExecuting = isCommandExecuting(
      gameRefreshingCommand
    );

    const allGamesRefreshing =
      isGameRefreshingCommandExecuting &&
      !gameRefreshingCommand?.body.gameIds?.length;

    const isGameRefreshing =
      isGameRefreshingCommandExecuting &&
      gameRefreshingCommand?.body.gameIds?.includes(gameId);

    const isSearchingExecuting = isCommandExecuting(
      findCommand(commands, {
        name: commandNames.GAME_SEARCH,
        gameIds: [gameId],
      })
    );

    const isRenamingFiles = isCommandExecuting(
      findCommand(commands, {
        name: commandNames.RENAME_FILES,
        gameId,
      })
    );

    const isRenamingGameCommand = findCommand(commands, {
      name: commandNames.RENAME_GAME,
    });

    const isRenamingGame =
      isCommandExecuting(isRenamingGameCommand) &&
      isRenamingGameCommand?.body?.gameIds?.includes(gameId);

    return {
      isRefreshing: isGameRefreshing || allGamesRefreshing,
      isRenaming: isRenamingFiles || isRenamingGame,
      isSearching: isSearchingExecuting,
    };
  }, [gameId, commands]);

  const { nextGame, previousGame } = useMemo(() => {
    const sortedGames = [...allGames].sort(sortByProp('sortTitle'));
    const gameIndex = sortedGames.findIndex((game) => game.id === gameId);

    if (gameIndex === -1) {
      return {
        nextGame: undefined,
        previousGame: undefined,
      };
    }

    const nextGame = sortedGames[gameIndex + 1] ?? sortedGames[0];
    const previousGame =
      sortedGames[gameIndex - 1] ?? sortedGames[sortedGames.length - 1];

    return {
      nextGame: {
        title: nextGame.title,
        titleSlug: nextGame.titleSlug,
      },
      previousGame: {
        title: previousGame.title,
        titleSlug: previousGame.titleSlug,
      },
    };
  }, [gameId, allGames]);

  const touchStart = useRef<number | null>(null);
  const [isOrganizeModalOpen, setIsOrganizeModalOpen] = useState(false);
  const [isManageGamesModalOpen, setIsManageGamesModalOpen] = useState(false);
  const [isInteractiveSearchModalOpen, setIsInteractiveSearchModalOpen] =
    useState(false);
  const [isEditGameModalOpen, setIsEditGameModalOpen] = useState(false);
  const [isDeleteGameModalOpen, setIsDeleteGameModalOpen] = useState(false);
  const [isGameHistoryModalOpen, setIsGameHistoryModalOpen] = useState(false);
  const [titleRef, { width: titleWidth }] = useMeasure();
  const [overviewRef, { height: overviewHeight }] = useMeasure();
  const wasRefreshing = usePrevious(isRefreshing);
  const wasRenaming = usePrevious(isRenaming);

  const handleOrganizePress = useCallback(() => {
    setIsOrganizeModalOpen(true);
  }, []);

  const handleOrganizeModalClose = useCallback(() => {
    setIsOrganizeModalOpen(false);
  }, []);

  const handleManageGamesPress = useCallback(() => {
    setIsManageGamesModalOpen(true);
  }, []);

  const handleManageGamesModalClose = useCallback(() => {
    setIsManageGamesModalOpen(false);
  }, []);

  const handleInteractiveSearchPress = useCallback(() => {
    setIsInteractiveSearchModalOpen(true);
  }, []);

  const handleInteractiveSearchModalClose = useCallback(() => {
    setIsInteractiveSearchModalOpen(false);
  }, []);

  const handleEditGamePress = useCallback(() => {
    setIsEditGameModalOpen(true);
  }, []);

  const handleEditGameModalClose = useCallback(() => {
    setIsEditGameModalOpen(false);
  }, []);

  const handleDeleteGamePress = useCallback(() => {
    setIsEditGameModalOpen(false);
    setIsDeleteGameModalOpen(true);
  }, []);

  const handleDeleteGameModalClose = useCallback(() => {
    setIsDeleteGameModalOpen(false);
  }, []);

  const handleGameHistoryPress = useCallback(() => {
    setIsGameHistoryModalOpen(true);
  }, []);

  const handleGameHistoryModalClose = useCallback(() => {
    setIsGameHistoryModalOpen(false);
  }, []);

  const handleMonitorTogglePress = useCallback(
    (value: boolean) => {
      dispatch(
        toggleGameMonitored({
          gameId,
          monitored: value,
        })
      );
    },
    [gameId, dispatch]
  );

  const handleRefreshPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.REFRESH_GAME,
        gameIds: [gameId],
      })
    );
  }, [gameId, dispatch]);

  const handleSearchPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.GAME_SEARCH,
        gameIds: [gameId],
      })
    );
  }, [gameId, dispatch]);

  const handleTouchStart = useCallback(
    (event: TouchEvent) => {
      const touches = event.touches;
      const currentTouch = touches[0].pageX;
      const touchY = touches[0].pageY;

      // Only change when swipe is on header, we need horizontal scroll on tables
      if (touchY > 470) {
        return;
      }

      if (touches.length !== 1) {
        return;
      }

      if (
        currentTouch < 50 ||
        isSidebarVisible ||
        isOrganizeModalOpen ||
        isEditGameModalOpen ||
        isDeleteGameModalOpen ||
        isManageGamesModalOpen ||
        isInteractiveSearchModalOpen ||
        isGameHistoryModalOpen
      ) {
        return;
      }

      touchStart.current = currentTouch;
    },
    [
      isSidebarVisible,
      isOrganizeModalOpen,
      isEditGameModalOpen,
      isDeleteGameModalOpen,
      isManageGamesModalOpen,
      isInteractiveSearchModalOpen,
      isGameHistoryModalOpen,
    ]
  );

  const handleTouchEnd = useCallback(
    (event: TouchEvent) => {
      const touches = event.changedTouches;
      const currentTouch = touches[0].pageX;

      if (!touchStart.current) {
        return;
      }

      if (
        currentTouch > touchStart.current &&
        currentTouch - touchStart.current > 100 &&
        previousGame !== undefined
      ) {
        history.push(getPathWithUrlBase(`/game/${previousGame.titleSlug}`));
      } else if (
        currentTouch < touchStart.current &&
        touchStart.current - currentTouch > 100 &&
        nextGame !== undefined
      ) {
        history.push(getPathWithUrlBase(`/game/${nextGame.titleSlug}`));
      }

      touchStart.current = null;
    },
    [previousGame, nextGame, history]
  );

  const handleTouchCancel = useCallback(() => {
    touchStart.current = null;
  }, []);

  const handleTouchMove = useCallback(() => {
    if (!touchStart.current) {
      return;
    }
  }, []);

  const handleKeyUp = useCallback(
    (event: KeyboardEvent) => {
      if (
        isOrganizeModalOpen ||
        isManageGamesModalOpen ||
        isInteractiveSearchModalOpen ||
        isEditGameModalOpen ||
        isDeleteGameModalOpen ||
        isGameHistoryModalOpen
      ) {
        return;
      }

      if (event.composedPath && event.composedPath().length === 4) {
        if (event.key === 'ArrowLeft' && previousGame !== undefined) {
          history.push(getPathWithUrlBase(`/game/${previousGame.titleSlug}`));
        }

        if (event.key === 'ArrowRight' && nextGame !== undefined) {
          history.push(getPathWithUrlBase(`/game/${nextGame.titleSlug}`));
        }
      }
    },
    [
      isOrganizeModalOpen,
      isManageGamesModalOpen,
      isInteractiveSearchModalOpen,
      isEditGameModalOpen,
      isDeleteGameModalOpen,
      isGameHistoryModalOpen,
      previousGame,
      nextGame,
      history,
    ]
  );

  const populate = useCallback(() => {
    dispatch(fetchGameFiles({ gameId }));
    dispatch(fetchExtraFiles({ gameId }));
    dispatch(fetchQueueDetails({ gameId }));
    dispatch(fetchImportListSchema());
  }, [gameId, dispatch]);

  useEffect(() => {
    populate();
  }, [populate]);

  useEffect(() => {
    registerPagePopulator(populate, ['gameUpdated']);

    return () => {
      unregisterPagePopulator(populate);
      dispatch(clearGameFiles());
      dispatch(clearExtraFiles());
      dispatch(clearQueueDetails());
      dispatch(cancelFetchReleases());
      dispatch(clearReleases());
    };
  }, [populate, dispatch]);

  useEffect(() => {
    if ((!isRefreshing && wasRefreshing) || (!isRenaming && wasRenaming)) {
      populate();
    }
  }, [isRefreshing, wasRefreshing, isRenaming, wasRenaming, populate]);

  useEffect(() => {
    window.addEventListener('touchstart', handleTouchStart);
    window.addEventListener('touchend', handleTouchEnd);
    window.addEventListener('touchcancel', handleTouchCancel);
    window.addEventListener('touchmove', handleTouchMove);
    window.addEventListener('keyup', handleKeyUp);

    return () => {
      window.removeEventListener('touchstart', handleTouchStart);
      window.removeEventListener('touchend', handleTouchEnd);
      window.removeEventListener('touchcancel', handleTouchCancel);
      window.removeEventListener('touchmove', handleTouchMove);
      window.removeEventListener('keyup', handleKeyUp);
    };
  }, [
    handleTouchStart,
    handleTouchEnd,
    handleTouchCancel,
    handleTouchMove,
    handleKeyUp,
  ]);

  if (!game) {
    return null;
  }

  const {
    id,
    steamAppId,
    igdbId,
    igdbSlug,
    title,
    originalTitle,
    year,
    inCinemas,
    physicalRelease,
    digitalRelease,
    runtime,
    certification,
    ratings,
    path,
    statistics = {} as Statistics,
    qualityProfileId,
    monitored,
    studio,
    originalLanguage,
    genres = [],
    collection,
    overview,
    status,
    youTubeTrailerId,
    isAvailable,
    images,
    tags,
    isSaving = false,
    gameFileId,
  } = game;

  const { sizeOnDisk = 0 } = statistics;

  const statusDetails = getGameStatusDetails(status);

  const fanartUrl = getFanartUrl(images);
  const isFetching = isGameFilesFetching || isExtraFilesFetching;

  const marqueeWidth = isSmallScreen ? titleWidth : titleWidth - 150;

  const titleWithYear = `${title}${year > 0 ? ` (${year})` : ''}`;

  return (
    <PageContent title={titleWithYear}>
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label={translate('RefreshAndScan')}
            iconName={icons.REFRESH}
            spinningName={icons.REFRESH}
            title={translate('RefreshInformationAndScanDisk')}
            isSpinning={isRefreshing}
            onPress={handleRefreshPress}
          />

          <PageToolbarButton
            label={translate('SearchGame')}
            iconName={icons.SEARCH}
            isSpinning={isSearching}
            title={undefined}
            onPress={handleSearchPress}
          />

          <PageToolbarButton
            label={translate('InteractiveSearch')}
            iconName={icons.INTERACTIVE}
            isSpinning={isSearching}
            title={undefined}
            onPress={handleInteractiveSearchPress}
          />

          <PageToolbarSeparator />

          <PageToolbarButton
            label={translate('PreviewRename')}
            iconName={icons.ORGANIZE}
            isDisabled={!hasGameFiles}
            onPress={handleOrganizePress}
          />

          <PageToolbarButton
            label={translate('ManageFiles')}
            iconName={icons.GAME_FILE}
            onPress={handleManageGamesPress}
          />

          <PageToolbarButton
            label={translate('History')}
            iconName={icons.HISTORY}
            onPress={handleGameHistoryPress}
          />

          <PageToolbarSeparator />

          <PageToolbarButton
            label={translate('Edit')}
            iconName={icons.EDIT}
            onPress={handleEditGamePress}
          />

          <PageToolbarButton
            label={translate('Delete')}
            iconName={icons.DELETE}
            onPress={handleDeleteGamePress}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody innerClassName={styles.innerContentBody}>
        <div className={styles.header}>
          <div
            className={styles.backdrop}
            style={
              fanartUrl ? { backgroundImage: `url(${fanartUrl})` } : undefined
            }
          >
            <div className={styles.backdropOverlay} />
          </div>

          <div className={styles.headerContent}>
            <GamePoster
              className={styles.poster}
              images={images}
              size={500}
              lazy={false}
            />

            <div className={styles.info}>
              <div ref={titleRef} className={styles.titleRow}>
                <div className={styles.titleContainer}>
                  <div className={styles.toggleMonitoredContainer}>
                    <MonitorToggleButton
                      className={styles.monitorToggleButton}
                      monitored={monitored}
                      isSaving={isSaving}
                      size={40}
                      onPress={handleMonitorTogglePress}
                    />
                  </div>

                  <div className={styles.title} style={{ width: marqueeWidth }}>
                    <Marquee text={title} title={originalTitle} />
                  </div>
                </div>

                <div className={styles.gameNavigationButtons}>
                  {previousGame ? (
                    <IconButton
                      className={styles.gameNavigationButton}
                      name={icons.ARROW_LEFT}
                      size={30}
                      title={translate('GameDetailsGoTo', {
                        title: previousGame.title,
                      })}
                      to={`/game/${previousGame.titleSlug}`}
                    />
                  ) : null}

                  {nextGame ? (
                    <IconButton
                      className={styles.gameNavigationButton}
                      name={icons.ARROW_RIGHT}
                      size={30}
                      title={translate('GameDetailsGoTo', {
                        title: nextGame.title,
                      })}
                      to={`/game/${nextGame.titleSlug}`}
                    />
                  ) : null}
                </div>
              </div>

              <div className={styles.details}>
                <div>
                  {certification ? (
                    <span
                      className={styles.certification}
                      title={translate('Certification')}
                    >
                      {certification}
                    </span>
                  ) : null}

                  <span className={styles.year}>
                    <Popover
                      anchor={
                        year > 0 ? (
                          year
                        ) : (
                          <Icon
                            name={icons.WARNING}
                            kind={kinds.WARNING}
                            size={20}
                          />
                        )
                      }
                      title={translate('ReleaseDates')}
                      body={
                        <GameReleaseDates
                          igdbId={igdbId}
                          inCinemas={inCinemas}
                          digitalRelease={digitalRelease}
                          physicalRelease={physicalRelease}
                        />
                      }
                      position={tooltipPositions.BOTTOM}
                    />
                  </span>

                  {runtime ? (
                    <span
                      className={styles.runtime}
                      title={translate('Runtime')}
                    >
                      {formatRuntime(runtime, gameRuntimeFormat)}
                    </span>
                  ) : null}

                  <span className={styles.links}>
                    <Tooltip
                      anchor={<Icon name={icons.EXTERNAL_LINK} size={20} />}
                      tooltip={
                        <GameDetailsLinks
                          steamAppId={steamAppId}
                          igdbSlug={igdbSlug}
                          youTubeTrailerId={youTubeTrailerId}
                        />
                      }
                      position={tooltipPositions.BOTTOM}
                    />
                  </span>

                  {!!tags.length && (
                    <span>
                      <Tooltip
                        anchor={<Icon name={icons.TAGS} size={20} />}
                        tooltip={<GameTags gameId={id} />}
                        position={tooltipPositions.BOTTOM}
                      />
                    </span>
                  )}
                </div>
              </div>

              <div className={styles.details}>
                {ratings.igdb?.value ? (
                  <span className={styles.rating}>
                    <IgdbRating ratings={ratings} iconSize={20} />
                  </span>
                ) : null}
                {ratings.metacritic?.value ? (
                  <span className={styles.rating}>
                    <MetacriticRating ratings={ratings} iconSize={20} />
                  </span>
                ) : null}
              </div>

              <div>
                <InfoLabel
                  className={styles.detailsInfoLabel}
                  name={translate('Path')}
                  size={sizes.LARGE}
                >
                  <span className={styles.path}>{path}</span>
                </InfoLabel>

                <InfoLabel
                  className={styles.detailsInfoLabel}
                  name={translate('Status')}
                  title={statusDetails.message}
                  size={sizes.LARGE}
                >
                  <span className={styles.statusName}>
                    <GameStatusLabel
                      gameId={id}
                      monitored={monitored}
                      isAvailable={isAvailable}
                      hasGameFiles={hasGameFiles}
                      status={status}
                    />
                  </span>
                </InfoLabel>

                <InfoLabel
                  className={styles.detailsInfoLabel}
                  name={translate('QualityProfile')}
                  size={sizes.LARGE}
                >
                  <span className={styles.qualityProfileName}>
                    <QualityProfileName qualityProfileId={qualityProfileId} />
                  </span>
                </InfoLabel>

                <InfoLabel
                  className={styles.detailsInfoLabel}
                  name={translate('Size')}
                  size={sizes.LARGE}
                >
                  <span className={styles.sizeOnDisk}>
                    {formatBytes(sizeOnDisk)}
                  </span>
                </InfoLabel>

                {collection ? (
                  <InfoLabel
                    className={styles.detailsInfoLabel}
                    name={translate('Collection')}
                    size={sizes.LARGE}
                  >
                    <div className={styles.collection}>
                      <GameCollectionLabel igdbId={collection.igdbId} />
                    </div>
                  </InfoLabel>
                ) : null}

                {originalLanguage?.name && !isSmallScreen ? (
                  <InfoLabel
                    className={styles.detailsInfoLabel}
                    name={translate('OriginalLanguage')}
                    size={sizes.LARGE}
                  >
                    <span className={styles.originalLanguage}>
                      {originalLanguage.name}
                    </span>
                  </InfoLabel>
                ) : null}

                {studio && !isSmallScreen ? (
                  <InfoLabel
                    className={styles.detailsInfoLabel}
                    name={translate('Studio')}
                    size={sizes.LARGE}
                  >
                    <span className={styles.studio}>{studio}</span>
                  </InfoLabel>
                ) : null}

                {genres.length && !isSmallScreen ? (
                  <InfoLabel
                    className={styles.detailsInfoLabel}
                    name={translate('Genres')}
                    size={sizes.LARGE}
                  >
                    <GameGenres className={styles.genres} genres={genres} />
                  </InfoLabel>
                ) : null}
              </div>

              <div ref={overviewRef} className={styles.overview}>
                <TextTruncate
                  line={Math.floor(
                    overviewHeight / (defaultFontSize * lineHeight)
                  )}
                  text={overview}
                />
              </div>
            </div>
          </div>
        </div>

        <div className={styles.contentContainer}>
          {!isFetching && gameFilesError ? (
            <Alert kind={kinds.DANGER}>
              {translate('LoadingGameFilesFailed')}
            </Alert>
          ) : null}

          {!isFetching && extraFilesError ? (
            <Alert kind={kinds.DANGER}>
              {translate('LoadingGameExtraFilesFailed')}
            </Alert>
          ) : null}

          <FieldSet legend={translate('Files')}>
            <GameStatus gameId={id} gameFileId={gameFileId} />

            <GameFileEditorTable gameId={id} />

            <ExtraFileTable gameId={id} />
          </FieldSet>

          <FieldSet legend={translate('Titles')}>
            <GameTitlesTable gameId={id} />
          </FieldSet>

          <DlcList gameId={id} />
        </div>

        <OrganizePreviewModal
          isOpen={isOrganizeModalOpen}
          gameId={id}
          onModalClose={handleOrganizeModalClose}
        />

        <EditGameModal
          isOpen={isEditGameModalOpen}
          gameId={id}
          onModalClose={handleEditGameModalClose}
          onDeleteGamePress={handleDeleteGamePress}
        />

        <GameHistoryModal
          isOpen={isGameHistoryModalOpen}
          gameId={id}
          onModalClose={handleGameHistoryModalClose}
        />

        <DeleteGameModal
          isOpen={isDeleteGameModalOpen}
          gameId={id}
          onModalClose={handleDeleteGameModalClose}
        />

        <InteractiveImportModal
          isOpen={isManageGamesModalOpen}
          gameId={id}
          title={title}
          folder={path}
          initialSortKey="relativePath"
          initialSortDirection={sortDirections.ASCENDING}
          showGame={false}
          allowGameChange={false}
          showDelete={true}
          showImportMode={false}
          modalTitle={translate('ManageFiles')}
          onModalClose={handleManageGamesModalClose}
        />

        <GameInteractiveSearchModal
          isOpen={isInteractiveSearchModalOpen}
          gameId={id}
          onModalClose={handleInteractiveSearchModalClose}
        />
      </PageContentBody>
    </PageContent>
  );
}

export default GameDetails;
