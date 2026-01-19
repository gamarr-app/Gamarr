import React, { useCallback, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { useSelect } from 'App/SelectContext';
import { GAME_SEARCH, REFRESH_GAME } from 'Commands/commandNames';
import GameTagList from 'Components/GameTagList';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import MetacriticRating from 'Components/MetacriticRating';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import Column from 'Components/Table/Column';
import Tooltip from 'Components/Tooltip/Tooltip';
import DeleteGameModal from 'Game/Delete/DeleteGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import EditGameModal from 'Game/Edit/EditGameModal';
import { Statistics } from 'Game/Game';
import GamePopularityIndex from 'Game/GamePopularityIndex';
import GameTitleLink from 'Game/GameTitleLink';
import createGameIndexItemSelector from 'Game/Index/createGameIndexItemSelector';
import { icons, kinds } from 'Helpers/Props';
import { executeCommand } from 'Store/Actions/commandActions';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import { SelectStateInputProps } from 'typings/props';
import formatRuntime from 'Utilities/Date/formatRuntime';
import formatBytes from 'Utilities/Number/formatBytes';
import firstCharToUpper from 'Utilities/String/firstCharToUpper';
import translate from 'Utilities/String/translate';
import GameIndexProgressBar from '../ProgressBar/GameIndexProgressBar';
import GameStatusCell from './GameStatusCell';
import selectTableOptions from './selectTableOptions';
import styles from './GameIndexRow.css';

interface GameIndexRowProps {
  gameId: number;
  sortKey: string;
  columns: Column[];
  isSelectMode: boolean;
}

function GameIndexRow(props: GameIndexRowProps) {
  const { gameId, columns, isSelectMode } = props;

  const { game, qualityProfile, isRefreshingGame, isSearchingGame } =
    useSelector(createGameIndexItemSelector(props.gameId));

  const { showSearchAction } = useSelector(selectTableOptions);

  const { gameRuntimeFormat } = useSelector(createUISettingsSelector());

  const {
    monitored,
    titleSlug,
    title,
    collection,
    studio,
    status,
    originalLanguage,
    originalTitle,
    added,
    statistics = {} as Statistics,
    year,
    inCinemas,
    digitalRelease,
    physicalRelease,
    releaseDate,
    runtime,
    minimumAvailability,
    path,
    genres = [],
    keywords = [],
    ratings,
    popularity,
    certification,
    tags = [],
    igdbId,
    imdbId,
    isAvailable,
    hasFile,
    gameFile,
    youTubeTrailerId,
    isSaving = false,
  } = game;

  const { sizeOnDisk = 0, releaseGroups = [] } = statistics;

  const dispatch = useDispatch();
  const [isEditGameModalOpen, setIsEditGameModalOpen] = useState(false);
  const [isDeleteGameModalOpen, setIsDeleteGameModalOpen] = useState(false);
  const [selectState, selectDispatch] = useSelect();

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

  const onSelectedChange = useCallback(
    ({ id, value, shiftKey }: SelectStateInputProps) => {
      selectDispatch({
        type: 'toggleSelected',
        id,
        isSelected: value,
        shiftKey,
      });
    },
    [selectDispatch]
  );

  return (
    <>
      {isSelectMode ? (
        <VirtualTableSelectCell
          id={gameId}
          isSelected={selectState.selectedState[gameId]}
          isDisabled={false}
          onSelectedChange={onSelectedChange}
        />
      ) : null}

      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'status') {
          return (
            <GameStatusCell
              key={name}
              className={styles[name]}
              gameId={gameId}
              monitored={monitored}
              status={status}
              isSelectMode={isSelectMode}
              isSaving={isSaving}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'sortTitle') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <GameTitleLink titleSlug={titleSlug} title={title} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'collection') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {collection ? collection.title : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'studio') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {studio}
            </VirtualTableRowCell>
          );
        }

        if (name === 'originalLanguage') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {originalLanguage.name}
            </VirtualTableRowCell>
          );
        }

        if (name === 'originalTitle') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {originalTitle}
            </VirtualTableRowCell>
          );
        }

        if (name === 'qualityProfileId') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {qualityProfile?.name ?? ''}
            </VirtualTableRowCell>
          );
        }

        if (name === 'added') {
          return (
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore ts(2739)
            <RelativeDateCell
              key={name}
              className={styles[name]}
              date={added}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'year') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {year > 0 ? year : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'inCinemas') {
          return (
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore ts(2739)
            <RelativeDateCell
              key={name}
              className={styles[name]}
              date={inCinemas}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'digitalRelease') {
          return (
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore ts(2739)
            <RelativeDateCell
              key={name}
              className={styles[name]}
              date={digitalRelease}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'physicalRelease') {
          return (
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore ts(2739)
            <RelativeDateCell
              key={name}
              className={styles[name]}
              date={physicalRelease}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'releaseDate') {
          return (
            // eslint-disable-next-line @typescript-eslint/ban-ts-comment
            // @ts-ignore ts(2739)
            <RelativeDateCell
              key={name}
              className={styles[name]}
              date={releaseDate}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'runtime') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {formatRuntime(runtime, gameRuntimeFormat)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'minimumAvailability') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {translate(firstCharToUpper(minimumAvailability))}
            </VirtualTableRowCell>
          );
        }

        if (name === 'path') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span title={path}>{path}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'sizeOnDisk') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {formatBytes(sizeOnDisk)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'genres') {
          const joinedGenres = genres.join(', ');

          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span title={joinedGenres}>{joinedGenres}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'keywords') {
          const joinedKeywords = keywords.join(', ');
          const truncatedKeywords =
            keywords.length > 3
              ? `${keywords.slice(0, 3).join(', ')}...`
              : joinedKeywords;

          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span title={joinedKeywords}>{truncatedKeywords}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'gameStatus') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <GameIndexProgressBar
                gameId={gameId}
                gameFile={gameFile}
                monitored={monitored}
                hasFile={hasFile}
                isAvailable={isAvailable}
                status={status}
                width={125}
                detailedProgressBar={true}
                bottomRadius={false}
                isStandAlone={true}
              />
            </VirtualTableRowCell>
          );
        }

        if (name === 'igdbRating') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {ratings.igdb ? <IgdbRating ratings={ratings} /> : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'metacriticRating') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {ratings.metacritic ? (
                <MetacriticRating ratings={ratings} />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'popularity') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <GamePopularityIndex popularity={popularity} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'certification') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              {certification}
            </VirtualTableRowCell>
          );
        }

        if (name === 'releaseGroups') {
          const joinedReleaseGroups = releaseGroups.join(', ');
          const truncatedReleaseGroups =
            releaseGroups.length > 3
              ? `${releaseGroups.slice(0, 3).join(', ')}...`
              : joinedReleaseGroups;

          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span title={joinedReleaseGroups}>{truncatedReleaseGroups}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'tags') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <GameTagList tags={tags} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <VirtualTableRowCell key={name} className={styles[name]}>
              <span className={styles.externalLinks}>
                <Tooltip
                  anchor={<Icon name={icons.EXTERNAL_LINK} size={12} />}
                  tooltip={
                    <GameDetailsLinks
                      igdbId={igdbId}
                      imdbId={imdbId}
                      youTubeTrailerId={youTubeTrailerId}
                    />
                  }
                  canFlip={true}
                  kind={kinds.INVERSE}
                />
              </span>

              <SpinnerIconButton
                name={icons.REFRESH}
                title={translate('RefreshGame')}
                isSpinning={isRefreshingGame}
                onPress={onRefreshPress}
              />

              {showSearchAction ? (
                <SpinnerIconButton
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
            </VirtualTableRowCell>
          );
        }

        return null;
      })}

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
    </>
  );
}

export default GameIndexRow;
