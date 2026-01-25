import React, { useCallback, useState } from 'react';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import ImportListList from 'Components/ImportListList';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MetacriticRating from 'Components/MetacriticRating';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import VirtualTableSelectCell from 'Components/Table/Cells/VirtualTableSelectCell';
import Column from 'Components/Table/Column';
import Popover from 'Components/Tooltip/Popover';
import AddNewDiscoverGameModal from 'DiscoverGame/AddNewDiscoverGameModal';
import ExcludeGameModal from 'DiscoverGame/Exclusion/ExcludeGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import { Collection, Image, Ratings } from 'Game/Game';
import GamePopularityIndex from 'Game/GamePopularityIndex';
import { icons } from 'Helpers/Props';
import Language from 'Language/Language';
import { SelectStateInputProps } from 'typings/props';
import formatRuntime from 'Utilities/Date/formatRuntime';
import translate from 'Utilities/String/translate';
import ListGameStatusCell from './ListGameStatusCell';
import styles from './DiscoverGameRow.css';

interface DiscoverGameRowProps {
  igdbId: number;
  steamAppId?: number;
  youTubeTrailerId?: string;
  status: string;
  title: string;
  originalLanguage: Language;
  year: number;
  overview?: string;
  folder?: string;
  images: Image[];
  studio?: string;
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  runtime?: number;
  genres: string[];
  ratings: Ratings;
  popularity: number;
  certification?: string;
  collection?: Collection;
  gameRuntimeFormat: string;
  columns: Column[];
  isExisting: boolean;
  isExcluded: boolean;
  isSelected?: boolean;
  isRecommendation: boolean;
  isPopular: boolean;
  isTrending: boolean;
  lists: number[];
  onSelectedChange: (options: SelectStateInputProps) => void;
}

function DiscoverGameRow({
  status,
  igdbId,
  steamAppId,
  youTubeTrailerId,
  title,
  originalLanguage,
  studio,
  inCinemas,
  physicalRelease,
  digitalRelease,
  runtime,
  year,
  overview = '',
  images,
  genres = [],
  ratings,
  popularity,
  certification,
  gameRuntimeFormat,
  collection,
  columns,
  isExisting,
  isExcluded,
  isRecommendation,
  isTrending,
  isPopular,
  isSelected,
  lists = [],
  onSelectedChange,
}: DiscoverGameRowProps) {
  const [isNewAddGameModalOpen, setIsNewAddGameModalOpen] = useState(false);
  const [isExcludeGameModalOpen, setIsExcludeGameModalOpen] = useState(false);

  const handleAddGamePress = useCallback(() => {
    setIsNewAddGameModalOpen(true);
  }, []);

  const handleAddGameModalClose = useCallback(() => {
    setIsNewAddGameModalOpen(false);
  }, []);

  const handleExcludeGamePress = useCallback(() => {
    setIsExcludeGameModalOpen(true);
  }, []);

  const handleExcludeGameModalClose = useCallback(() => {
    setIsExcludeGameModalOpen(false);
  }, []);

  // Use steamAppId for slug if available, otherwise igdbId
  const titleSlug = steamAppId && steamAppId > 0 ? steamAppId : igdbId;
  const linkProps = isExisting
    ? { to: `/game/${titleSlug}` }
    : { onPress: handleAddGamePress };

  return (
    <>
      <VirtualTableSelectCell
        inputClassName={styles.checkInput}
        id={igdbId}
        isSelected={isSelected}
        isDisabled={false}
        onSelectedChange={onSelectedChange}
      />

      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'status') {
          return (
            <ListGameStatusCell
              key={name}
              className={styles[name as keyof typeof styles]}
              status={status}
              isExclusion={isExcluded}
              isExisting={isExisting}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'sortTitle') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              <Link {...linkProps}>{title}</Link>

              {isExisting ? (
                <Icon
                  className={styles.alreadyExistsIcon}
                  name={icons.CHECK_CIRCLE}
                  title={translate('AlreadyInYourLibrary')}
                />
              ) : null}

              {isExcluded ? (
                <Icon
                  className={styles.exclusionIcon}
                  name={icons.DANGER}
                  title={translate('GameExcludedFromAutomaticAdd')}
                />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'year') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {year}
            </VirtualTableRowCell>
          );
        }

        if (name === 'collection') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {collection ? collection.title : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'originalLanguage') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {originalLanguage.name}
            </VirtualTableRowCell>
          );
        }

        if (name === 'studio') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {studio}
            </VirtualTableRowCell>
          );
        }

        if (name === 'inCinemas') {
          return (
            <RelativeDateCell
              key={name}
              className={styles[name as keyof typeof styles]}
              date={inCinemas}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'physicalRelease') {
          return (
            <RelativeDateCell
              key={name}
              className={styles[name as keyof typeof styles]}
              date={physicalRelease}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'digitalRelease') {
          return (
            <RelativeDateCell
              key={name}
              className={styles[name as keyof typeof styles]}
              date={digitalRelease}
              timeForToday={false}
              component={VirtualTableRowCell}
            />
          );
        }

        if (name === 'runtime') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {formatRuntime(runtime ?? 0, gameRuntimeFormat)}
            </VirtualTableRowCell>
          );
        }

        if (name === 'genres') {
          const joinedGenres = genres.join(', ');

          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              <span title={joinedGenres}>{joinedGenres}</span>
            </VirtualTableRowCell>
          );
        }

        if (name === 'igdbRating') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {ratings.igdb?.value ? <IgdbRating ratings={ratings} /> : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'metacriticRating') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {ratings.metacritic?.value ? (
                <MetacriticRating ratings={ratings} />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'popularity') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              <GamePopularityIndex popularity={popularity} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'certification') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {certification}
            </VirtualTableRowCell>
          );
        }

        if (name === 'lists') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              <ImportListList lists={lists} />
            </VirtualTableRowCell>
          );
        }

        if (name === 'isRecommendation') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {isRecommendation ? (
                <Icon
                  className={styles.statusIcon}
                  name={icons.RECOMMENDED}
                  size={12}
                  title={translate('GameIsRecommend')}
                />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'isTrending') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {isTrending ? (
                <Icon
                  className={styles.statusIcon}
                  name={icons.TRENDING}
                  size={12}
                  title={translate('GameIsTrending')}
                />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'isPopular') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              {isPopular ? (
                <Icon
                  className={styles.statusIcon}
                  name={icons.POPULAR}
                  size={12}
                  title={translate('GameIsPopular')}
                />
              ) : null}
            </VirtualTableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <VirtualTableRowCell
              key={name}
              className={styles[name as keyof typeof styles]}
            >
              <span className={styles.externalLinks}>
                <Popover
                  anchor={<Icon name={icons.EXTERNAL_LINK} size={12} />}
                  title={translate('Links')}
                  body={
                    <GameDetailsLinks
                      steamAppId={steamAppId ?? 0}
                      igdbSlug={String(igdbId)}
                      youTubeTrailerId={youTubeTrailerId}
                    />
                  }
                />
              </span>

              <IconButton
                name={icons.REMOVE}
                title={
                  isExcluded
                    ? translate('GameAlreadyExcluded')
                    : translate('ExcludeGame')
                }
                isDisabled={isExcluded}
                onPress={handleExcludeGamePress}
              />
            </VirtualTableRowCell>
          );
        }

        return null;
      })}

      <AddNewDiscoverGameModal
        isOpen={isNewAddGameModalOpen && !isExisting}
        igdbId={igdbId}
        title={title}
        year={year}
        overview={overview}
        images={images}
        onModalClose={handleAddGameModalClose}
      />

      <ExcludeGameModal
        isOpen={isExcludeGameModalOpen}
        igdbId={igdbId}
        title={title}
        year={year}
        onModalClose={handleExcludeGameModalClose}
      />
    </>
  );
}

export default DiscoverGameRow;
