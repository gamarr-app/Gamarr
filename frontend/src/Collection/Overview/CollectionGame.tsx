import React, { Component, CSSProperties } from 'react';
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

interface CollectionGameState {
  hasPosterError: boolean;
  isEditGameModalOpen: boolean;
  isNewAddGameModalOpen: boolean;
}

class CollectionGame extends Component<
  CollectionGameProps,
  CollectionGameState
> {
  //
  // Lifecycle

  constructor(props: CollectionGameProps) {
    super(props);

    this.state = {
      hasPosterError: false,
      isEditGameModalOpen: false,
      isNewAddGameModalOpen: false,
    };
  }

  //
  // Listeners

  onEditGamePress = () => {
    this.setState({ isEditGameModalOpen: true });
  };

  onEditGameModalClose = () => {
    this.setState({ isEditGameModalOpen: false });
  };

  onAddGamePress = () => {
    this.setState({ isNewAddGameModalOpen: true });
  };

  onAddGameModalClose = () => {
    this.setState({ isNewAddGameModalOpen: false });
  };

  onPosterLoad = () => {
    if (this.state.hasPosterError) {
      this.setState({ hasPosterError: false });
    }
  };

  onPosterLoadError = () => {
    if (!this.state.hasPosterError) {
      this.setState({ hasPosterError: true });
    }
  };

  onDeleteGamePress = () => {
    // Placeholder for delete functionality
  };

  //
  // Render

  render() {
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
    } = this.props;

    const { hasPosterError, isEditGameModalOpen, isNewAddGameModalOpen } =
      this.state;

    // Use steamAppId for slug if available, otherwise igdbId
    const titleSlug = steamAppId && steamAppId > 0 ? steamAppId : igdbId;
    const linkProps = id
      ? { to: `/game/${titleSlug}` }
      : { onPress: this.onAddGamePress };

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
              onError={this.onPosterLoadError}
              onLoad={this.onPosterLoad}
            />

            {hasPosterError && (
              <div className={styles.overlayTitle}>{title}</div>
            )}

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
          onModalClose={this.onAddGameModalClose}
        />

        <EditGameModal
          isOpen={isEditGameModalOpen}
          gameId={id ?? 0}
          onModalClose={this.onEditGameModalClose}
          onDeleteGamePress={this.onDeleteGamePress}
        />
      </div>
    );
  }
}

export default CollectionGame;
