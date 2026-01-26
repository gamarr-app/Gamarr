import { CSSProperties, useCallback, useState } from 'react';
import AddNewGameCollectionGameModal from 'Collection/AddNewGameCollectionGameModal';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import EditGameModal from 'Game/Edit/EditGameModal';
import { GameStatus, Image } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import GameIndexProgressBar from 'Game/Index/ProgressBar/GameIndexProgressBar';
import { GameFile } from 'GameFile/GameFile';
import translate from 'Utilities/String/translate';
import styles from './CollectionGame.css';

interface CollectionGameProps {
  id?: number;
  title: string;
  year: number;
  status: GameStatus;
  overview?: string;
  monitored?: boolean;
  collectionId: number;
  hasFile?: boolean;
  folder?: string;
  isAvailable?: boolean;
  gameFile?: GameFile;
  images: Image[];
  posterWidth: number;
  posterHeight: number;
  detailedProgressBar: boolean;
  isExistingGame?: boolean;
  isExcluded?: boolean;
  igdbId: number;
  steamAppId?: number;
  youTubeTrailerId?: string;
  onMonitorTogglePress: (monitored: boolean) => void;
}

function CollectionGame(props: CollectionGameProps) {
  const {
    id,
    title,
    status,
    overview,
    year,
    igdbId,
    steamAppId,
    images,
    monitored,
    hasFile,
    folder,
    isAvailable,
    gameFile,
    isExistingGame,
    isExcluded,
    posterWidth,
    posterHeight,
    detailedProgressBar,
    onMonitorTogglePress,
    collectionId,
  } = props;

  const [hasPosterError, setHasPosterError] = useState(false);
  const [isEditGameModalOpen, setIsEditGameModalOpen] = useState(false);
  const [isNewAddGameModalOpen, setIsNewAddGameModalOpen] = useState(false);

  const onEditGameModalClose = useCallback(() => {
    setIsEditGameModalOpen(false);
  }, []);

  const onAddGamePress = useCallback(() => {
    setIsNewAddGameModalOpen(true);
  }, []);

  const onAddGameModalClose = useCallback(() => {
    setIsNewAddGameModalOpen(false);
  }, []);

  const onPosterLoad = useCallback(() => {
    setHasPosterError((prev) => {
      if (prev) {
        return false;
      }
      return prev;
    });
  }, []);

  const onPosterLoadError = useCallback(() => {
    setHasPosterError((prev) => {
      if (!prev) {
        return true;
      }
      return prev;
    });
  }, []);

  const onDeleteGamePress = useCallback(() => {
    // Placeholder for delete functionality
  }, []);

  // Use steamAppId for slug if available, otherwise igdbId
  const titleSlug = steamAppId && steamAppId > 0 ? steamAppId : igdbId;
  const linkProps = id
    ? { to: `/game/${titleSlug}` }
    : { onPress: onAddGamePress };

  const elementStyle: CSSProperties = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
    borderRadius: '5px',
  };

  return (
    <div className={styles.content}>
      <div className={styles.posterContainer}>
        {isExistingGame && monitored !== undefined && (
          <div className={styles.editorSelect}>
            <MonitorToggleButton
              className={styles.monitorToggleButton}
              monitored={monitored}
              size={20}
              onPress={onMonitorTogglePress}
            />
          </div>
        )}

        {isExcluded ? (
          <div className={styles.excluded} title={translate('Excluded')} />
        ) : null}

        <Link className={styles.link} style={elementStyle} {...linkProps}>
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

          {hasPosterError && <div className={styles.overlayTitle}>{title}</div>}

          <div className={styles.overlayHover}>
            <div className={styles.overlayHoverTitle}>
              {title} {year > 0 ? `(${year})` : ''}
            </div>

            {id ? (
              <GameIndexProgressBar
                gameId={id}
                gameFile={gameFile}
                monitored={monitored ?? false}
                hasFile={hasFile ?? false}
                status={status}
                bottomRadius={true}
                width={posterWidth}
                detailedProgressBar={detailedProgressBar}
                isAvailable={isAvailable ?? false}
              />
            ) : null}
          </div>
        </Link>
      </div>

      <AddNewGameCollectionGameModal
        isOpen={isNewAddGameModalOpen && !isExistingGame}
        igdbId={igdbId}
        title={title}
        year={year}
        overview={overview ?? ''}
        images={images}
        folder={folder ?? ''}
        collectionId={collectionId}
        onModalClose={onAddGameModalClose}
      />

      <EditGameModal
        isOpen={isEditGameModalOpen}
        gameId={id ?? 0}
        onModalClose={onEditGameModalClose}
        onDeleteGamePress={onDeleteGamePress}
      />
    </div>
  );
}

export default CollectionGame;
