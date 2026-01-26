import { useCallback, useEffect, useRef, useState } from 'react';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import MetacriticRating from 'Components/MetacriticRating';
import Tooltip from 'Components/Tooltip/Tooltip';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import GameStatusLabel from 'Game/Details/GameStatusLabel';
import { GameStatus, Image, Ratings } from 'Game/Game';
import GameGenres from 'Game/GameGenres';
import GamePoster from 'Game/GamePoster';
import GameIndexProgressBar from 'Game/Index/ProgressBar/GameIndexProgressBar';
import { GameFile } from 'GameFile/GameFile';
import { icons, kinds, sizes, tooltipPositions } from 'Helpers/Props';
import Language from 'Language/Language';
import formatRuntime from 'Utilities/Date/formatRuntime';
import translate from 'Utilities/String/translate';
import AddNewGameModal from './AddNewGameModal';
import styles from './AddNewGameSearchResult.css';

export interface AddNewGameSearchResultProps {
  igdbId: number;
  igdbSlug?: string;
  steamAppId?: number;
  youTubeTrailerId?: string;
  title: string;
  titleSlug: string;
  year: number;
  studio?: string;
  originalLanguage?: Language;
  genres?: string[];
  status: GameStatus;
  overview?: string;
  ratings: Ratings;
  folder: string;
  images: Image[];
  existingGameId?: number;
  isExistingGame: boolean;
  isExcluded?: boolean;
  isSmallScreen: boolean;
  monitored: boolean;
  isAvailable: boolean;
  gameFile?: GameFile;
  runtime: number;
  gameRuntimeFormat: string;
  certification?: string;
}

function AddNewGameSearchResult(props: AddNewGameSearchResultProps) {
  const {
    igdbId,
    igdbSlug,
    steamAppId,
    youTubeTrailerId,
    title,
    titleSlug,
    year,
    studio,
    originalLanguage,
    genres = [],
    status,
    overview,
    ratings,
    folder,
    images,
    existingGameId,
    isExistingGame,
    isExcluded = false,
    isSmallScreen,
    monitored,
    isAvailable,
    gameFile,
    runtime,
    gameRuntimeFormat,
    certification,
  } = props;

  const [isNewAddGameModalOpen, setIsNewAddGameModalOpen] = useState(false);
  const prevIsExistingGameRef = useRef(isExistingGame);

  // Close modal when game becomes existing
  useEffect(() => {
    if (!prevIsExistingGameRef.current && isExistingGame) {
      setIsNewAddGameModalOpen(false);
    }
    prevIsExistingGameRef.current = isExistingGame;
  }, [isExistingGame]);

  const onPress = useCallback(() => {
    setIsNewAddGameModalOpen(true);
  }, []);

  const onAddGameModalClose = useCallback(() => {
    setIsNewAddGameModalOpen(false);
  }, []);

  const hasGameFile = !!gameFile;

  const linkProps = isExistingGame ? { to: `/game/${titleSlug}` } : { onPress };
  const posterWidth = 167;
  const posterHeight = 250;

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  return (
    <div className={styles.searchResult}>
      <Link className={styles.underlay} {...linkProps} />

      <div className={styles.overlay}>
        {isSmallScreen ? null : (
          <div>
            <div className={styles.posterContainer}>
              <GamePoster
                className={styles.poster}
                style={elementStyle}
                images={images}
                size={250}
                overflow={true}
                lazy={false}
              />
            </div>

            {isExistingGame && existingGameId && (
              <GameIndexProgressBar
                gameId={existingGameId}
                gameFile={gameFile}
                monitored={monitored}
                hasFile={hasGameFile}
                status={status}
                width={posterWidth}
                detailedProgressBar={true}
                isAvailable={isAvailable}
              />
            )}
          </div>
        )}

        <div className={styles.content}>
          <div className={styles.titleRow}>
            <div className={styles.titleContainer}>
              <div className={styles.title}>
                {title}

                {!title.contains(String(year)) && !!year ? (
                  <span className={styles.year}>({year})</span>
                ) : null}
              </div>
            </div>

            <div className={styles.icons}>
              <div>
                {isExistingGame && (
                  <Icon
                    className={styles.alreadyExistsIcon}
                    name={icons.CHECK_CIRCLE}
                    size={36}
                    title={translate('AlreadyInYourLibrary')}
                  />
                )}

                {isExcluded && (
                  <Icon
                    className={styles.exclusionIcon}
                    name={icons.DANGER}
                    size={36}
                    title={translate('GameIsOnImportExclusionList')}
                  />
                )}
              </div>
            </div>
          </div>

          <div>
            {!!certification && (
              <span className={styles.certification}>{certification}</span>
            )}

            {!!runtime && (
              <span className={styles.runtime}>
                {formatRuntime(runtime, gameRuntimeFormat)}
              </span>
            )}
          </div>

          <div>
            {ratings.igdb?.value ? (
              <Label size={sizes.LARGE}>
                <IgdbRating ratings={ratings} iconSize={13} />
              </Label>
            ) : null}

            {ratings.metacritic?.value ? (
              <Label size={sizes.LARGE}>
                <MetacriticRating ratings={ratings} iconSize={13} />
              </Label>
            ) : null}

            {originalLanguage?.name ? (
              <Label size={sizes.LARGE}>
                <Icon name={icons.LANGUAGE} size={13} />
                <span className={styles.originalLanguage}>
                  {originalLanguage.name}
                </span>
              </Label>
            ) : null}

            {studio ? (
              <Label size={sizes.LARGE}>
                <Icon name={icons.STUDIO} size={13} />
                <span className={styles.studio}>{studio}</span>
              </Label>
            ) : null}

            {genres.length > 0 ? (
              <Label size={sizes.LARGE}>
                <Icon name={icons.GENRE} size={13} />
                <GameGenres className={styles.genres} genres={genres} />
              </Label>
            ) : null}

            <Tooltip
              anchor={
                <Label size={sizes.LARGE}>
                  <Icon name={icons.EXTERNAL_LINK} size={13} />

                  <span className={styles.links}>{translate('Links')}</span>
                </Label>
              }
              tooltip={
                <GameDetailsLinks
                  steamAppId={steamAppId || 0}
                  igdbSlug={igdbSlug}
                  youTubeTrailerId={youTubeTrailerId}
                />
              }
              canFlip={true}
              kind={kinds.INVERSE}
              position={tooltipPositions.TOP}
            />

            {isExistingGame && isSmallScreen && existingGameId && (
              <GameStatusLabel
                gameId={existingGameId}
                monitored={monitored}
                isAvailable={isAvailable}
                hasGameFiles={hasGameFile}
                status={status}
                useLabel={true}
              />
            )}
          </div>

          <div className={styles.overview}>{overview}</div>
        </div>
      </div>

      <AddNewGameModal
        isOpen={isNewAddGameModalOpen && !isExistingGame}
        igdbId={igdbId}
        steamAppId={steamAppId}
        title={title}
        year={year}
        overview={overview}
        folder={folder}
        images={images}
        onModalClose={onAddGameModalClose}
      />
    </div>
  );
}

export default AddNewGameSearchResult;
