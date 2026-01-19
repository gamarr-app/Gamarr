import PropTypes from 'prop-types';
import React, { Component } from 'react';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import IgdbRating from 'Components/IgdbRating';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import MetacriticRating from 'Components/MetacriticRating';
import Popover from 'Components/Tooltip/Popover';
import AddNewDiscoverGameModal from 'DiscoverGame/AddNewDiscoverGameModal';
import ExcludeGameModal from 'DiscoverGame/Exclusion/ExcludeGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import GamePoster from 'Game/GamePoster';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import DiscoverGamePosterInfo from './DiscoverGamePosterInfo';
import styles from './DiscoverGamePoster.css';

class DiscoverGamePoster extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
      hasPosterError: false,
      isNewAddGameModalOpen: false,
      isExcludeGameModalOpen: false
    };
  }

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddGameModalOpen: true });
  };

  onAddGameModalClose = () => {
    this.setState({ isNewAddGameModalOpen: false });
  };

  onExcludeGamePress = () => {
    this.setState({ isExcludeGameModalOpen: true });
  };

  onExcludeGameModalClose = () => {
    this.setState({ isExcludeGameModalOpen: false });
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

  onChange = ({ value, shiftKey }) => {
    const {
      igdbId,
      onSelectedChange
    } = this.props;

    onSelectedChange({ id: igdbId, value, shiftKey });
  };

  //
  // Render

  render() {
    const {
      igdbId,
      steamAppId,
      youTubeTrailerId,
      title,
      year,
      overview,
      folder,
      images,
      posterWidth,
      posterHeight,
      showTitle,
      showIgdbRating,
      showMetacriticRating,
      ratings,
      isExisting,
      isExcluded,
      isSelected,
      showRelativeDates,
      shortDateFormat,
      timeFormat,
      gameRuntimeFormat,
      ...otherProps
    } = this.props;

    const {
      hasPosterError,
      isNewAddGameModalOpen,
      isExcludeGameModalOpen
    } = this.state;

    // Use steamAppId for slug if available, otherwise igdbId
    const titleSlug = steamAppId > 0 ? steamAppId : igdbId;
    const linkProps = isExisting ? { to: `/game/${titleSlug}` } : { onPress: this.onPress };

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`
    };

    return (
      <div className={styles.content}>
        <div className={styles.posterContainer} title={title}>
          {
            <div className={styles.editorSelect}>
              <CheckInput
                className={styles.checkInput}
                name={igdbId.toString()}
                value={isSelected}
                onChange={this.onChange}
              />
            </div>
          }

          <Label className={styles.controls}>
            <IconButton
              className={styles.action}
              name={icons.REMOVE}
              title={isExcluded ? translate('GameAlreadyExcluded') : translate('ExcludeGame')}
              onPress={this.onExcludeGamePress}
              isDisabled={isExcluded}
            />
            <span className={styles.externalLinks}>
              <Popover
                anchor={
                  <Icon
                    name={icons.EXTERNAL_LINK}
                    size={12}
                  />
                }
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
          </Label>

          {
            isExcluded &&
              <div
                className={styles.excluded}
                title={translate('Excluded')}
              />
          }

          {
            isExisting &&
              <div
                className={styles.existing}
                title={translate('Existing')}
              />
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
          </Link>
        </div>

        {showTitle ?
          <div className={styles.title} title={title}>
            {title}
          </div> :
          null}

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

        <DiscoverGamePosterInfo
          showRelativeDates={showRelativeDates}
          shortDateFormat={shortDateFormat}
          timeFormat={timeFormat}
          gameRuntimeFormat={gameRuntimeFormat}
          ratings={ratings}
          showIgdbRating={showIgdbRating}
          showMetacriticRating={showMetacriticRating}
          {...otherProps}
        />

        <AddNewDiscoverGameModal
          isOpen={isNewAddGameModalOpen && !isExisting}
          igdbId={igdbId}
          title={title}
          year={year}
          overview={overview}
          folder={folder}
          images={images}
          onModalClose={this.onAddGameModalClose}
        />

        <ExcludeGameModal
          isOpen={isExcludeGameModalOpen}
          igdbId={igdbId}
          title={title}
          year={year}
          onModalClose={this.onExcludeGameModalClose}
        />
      </div>
    );
  }
}

DiscoverGamePoster.propTypes = {
  igdbId: PropTypes.number.isRequired,
  steamAppId: PropTypes.number,
  youTubeTrailerId: PropTypes.string,
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  overview: PropTypes.string.isRequired,
  folder: PropTypes.string.isRequired,
  status: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  showTitle: PropTypes.bool.isRequired,
  showIgdbRating: PropTypes.bool.isRequired,
  showMetacriticRating: PropTypes.bool.isRequired,
  ratings: PropTypes.object.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  gameRuntimeFormat: PropTypes.string.isRequired,
  isExisting: PropTypes.bool.isRequired,
  isExcluded: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  onSelectedChange: PropTypes.func.isRequired
};

export default DiscoverGamePoster;
