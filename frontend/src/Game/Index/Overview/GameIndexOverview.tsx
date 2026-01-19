import React, { useCallback, useMemo, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import TextTruncate from 'react-text-truncate';
import { GAME_SEARCH, REFRESH_GAME } from 'Commands/commandNames';
import GameTagList from 'Components/GameTagList';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
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
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import translate from 'Utilities/String/translate';
import createGameIndexItemSelector from '../createGameIndexItemSelector';
import GameIndexOverviewInfo from './GameIndexOverviewInfo';
import selectOverviewOptions from './selectOverviewOptions';
import styles from './GameIndexOverview.css';

const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10.
// Less side-effecty than using react-measure.
const titleRowHeight = 42;

interface GameIndexOverviewProps {
  gameId: number;
  sortKey: string;
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  isSelectMode: boolean;
  isSmallScreen: boolean;
}

function GameIndexOverview(props: GameIndexOverviewProps) {
  const {
    gameId,
    sortKey,
    posterWidth,
    posterHeight,
    rowHeight,
    isSelectMode,
    isSmallScreen,
  } = props;

  const { game, qualityProfile, isRefreshingGame, isSearchingGame } =
    useSelector(createGameIndexItemSelector(props.gameId));

  const overviewOptions = useSelector(selectOverviewOptions);

  const {
    title,
    monitored,
    status,
    path,
    titleSlug,
    overview,
    statistics = {} as Statistics,
    images,
    tags,
    hasFile,
    isAvailable,
    igdbId,
    steamAppId,
    studio,
    added,
    youTubeTrailerId,
  } = game;

  const { sizeOnDisk = 0 } = statistics;

  const dispatch = useDispatch();
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

  const link = `/game/${igdbId}`;

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  const contentHeight = useMemo(() => {
    const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

    return rowHeight - padding;
  }, [rowHeight, isSmallScreen]);

  const overviewHeight = contentHeight - titleRowHeight;

  return (
    <div>
      <div className={styles.content}>
        <div className={styles.poster}>
          <div className={styles.posterContainer}>
            {isSelectMode ? (
              <GameIndexPosterSelect gameId={gameId} titleSlug={titleSlug} />
            ) : null}

            {status === 'deleted' ? (
              <div className={styles.deleted} title={translate('Deleted')} />
            ) : null}

            <Link className={styles.link} style={elementStyle} to={link}>
              <GamePoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                size={250}
                lazy={false}
                overflow={true}
              />
            </Link>
          </div>

          <GameIndexProgressBar
            gameId={gameId}
            gameFile={game.gameFile}
            monitored={monitored}
            hasFile={hasFile}
            isAvailable={isAvailable}
            status={status}
            width={posterWidth}
            detailedProgressBar={overviewOptions.detailedProgressBar}
            bottomRadius={false}
          />
        </div>

        <div className={styles.info} style={{ maxHeight: contentHeight }}>
          <div className={styles.titleRow}>
            <Link className={styles.title} to={link}>
              {title}
            </Link>

            <div className={styles.actions}>
              <span className={styles.externalLinks}>
                <Popover
                  anchor={<Icon name={icons.EXTERNAL_LINK} size={12} />}
                  title={translate('Links')}
                  body={
                    <GameDetailsLinks
                      steamAppId={steamAppId}
                      igdbId={igdbId}
                      youTubeTrailerId={youTubeTrailerId}
                    />
                  }
                />
              </span>

              <SpinnerIconButton
                name={icons.REFRESH}
                title={translate('RefreshGame')}
                isSpinning={isRefreshingGame}
                onPress={onRefreshPress}
              />

              {overviewOptions.showSearchAction ? (
                <SpinnerIconButton
                  className={styles.actions}
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
            </div>
          </div>

          <div className={styles.details}>
            <div className={styles.overviewContainer}>
              <Link className={styles.overview} to={link}>
                <TextTruncate
                  line={Math.floor(
                    overviewHeight / (defaultFontSize * lineHeight)
                  )}
                  text={overview}
                />
              </Link>

              {overviewOptions.showTags ? (
                <div className={styles.tags}>
                  <GameTagList tags={tags} />
                </div>
              ) : null}
            </div>
            <GameIndexOverviewInfo
              height={overviewHeight}
              monitored={monitored}
              qualityProfile={qualityProfile}
              studio={studio}
              sizeOnDisk={sizeOnDisk}
              added={added}
              path={path}
              sortKey={sortKey}
              {...overviewOptions}
            />
          </div>
        </div>
      </div>

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

export default GameIndexOverview;
