import PropTypes from 'prop-types';
import React, { Component } from 'react';
import AddNewGameCollectionGameModal from 'Collection/AddNewGameCollectionGameModal';
import Link from 'Components/Link/Link';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import EditGameModal from 'Game/Edit/EditGameModal';
import GameIndexProgressBar from 'Game/Index/ProgressBar/GameIndexProgressBar';
import GamePoster from 'Game/GamePoster';
import translate from 'Utilities/String/translate';
import styles from './CollectionGame.css';

class CollectionGame extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasPosterError: false,
      isEditGameModalOpen: false,
      isNewAddGameModalOpen: false
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
      collectionId
    } = this.props;

    const {
      hasPosterError,
      isEditGameModalOpen,
      isNewAddGameModalOpen
    } = this.state;

    const linkProps = id ? { to: `/game/${igdbId}` } : { onPress: this.onAddGamePress };

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`,
      borderRadius: '5px'
    };

    return (
      <div className={styles.content}>
        <div className={styles.posterContainer}>
          {
            isExistingGame &&
              <div className={styles.editorSelect}>
                <MonitorToggleButton
                  className={styles.monitorToggleButton}
                  monitored={monitored}
                  size={20}
                  onPress={onMonitorTogglePress}
                />
              </div>
          }

          {
            isExcluded ?
              <div
                className={styles.excluded}
                title={translate('Excluded')}
              /> :
              null
          }

          <Link
            className={styles.link}
            style={elementStyle}
            {...linkProps}
          >
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

            {
              hasPosterError &&
                <div className={styles.overlayTitle}>
                  {title}
                </div>
            }

            <div className={styles.overlayHover}>
              <div className={styles.overlayHoverTitle}>
                {title} {year > 0 ? `(${year})` : ''}
              </div>

              {
                id ?
                  <GameIndexProgressBar
                    gameId={id}
                    gameFile={gameFile}
                    monitored={monitored}
                    hasFile={hasFile}
                    status={status}
                    bottomRadius={true}
                    width={posterWidth}
                    detailedProgressBar={detailedProgressBar}
                    isAvailable={isAvailable}
                  /> :
                  null
              }
            </div>
          </Link>
        </div>

        <AddNewGameCollectionGameModal
          isOpen={isNewAddGameModalOpen && !isExistingGame}
          igdbId={igdbId}
          title={title}
          year={year}
          overview={overview}
          images={images}
          folder={folder}
          onModalClose={this.onAddGameModalClose}
          collectionId={collectionId}
        />

        <EditGameModal
          isOpen={isEditGameModalOpen}
          gameId={id}
          onModalClose={this.onEditGameModalClose}
          onDeleteGamePress={this.onDeleteGamePress}
        />
      </div>
    );
  }
}

CollectionGame.propTypes = {
  id: PropTypes.number,
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  status: PropTypes.string.isRequired,
  overview: PropTypes.string,
  monitored: PropTypes.bool,
  collectionId: PropTypes.number.isRequired,
  hasFile: PropTypes.bool,
  folder: PropTypes.string,
  isAvailable: PropTypes.bool,
  gameFile: PropTypes.object,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  detailedProgressBar: PropTypes.bool.isRequired,
  isExistingGame: PropTypes.bool,
  isExcluded: PropTypes.bool,
  igdbId: PropTypes.number.isRequired,
  imdbId: PropTypes.string,
  youTubeTrailerId: PropTypes.string,
  onMonitorTogglePress: PropTypes.func.isRequired
};

export default CollectionGame;
