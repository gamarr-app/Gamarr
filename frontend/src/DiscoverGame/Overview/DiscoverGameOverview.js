import PropTypes from 'prop-types';
import React, { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import ImportListList from 'Components/ImportListList';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import Link from 'Components/Link/Link';
import Popover from 'Components/Tooltip/Popover';
import AddNewDiscoverGameModal from 'DiscoverGame/AddNewDiscoverGameModal';
import ExcludeGameModal from 'DiscoverGame/Exclusion/ExcludeGameModal';
import GameDetailsLinks from 'Game/Details/GameDetailsLinks';
import GamePoster from 'Game/GamePoster';
import { icons, kinds } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import translate from 'Utilities/String/translate';
import DiscoverGameOverviewInfo from './DiscoverGameOverviewInfo';
import styles from './DiscoverGameOverview.css';

const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(dimensions.gameIndexColumnPaddingSmallScreen);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10. 19 + 5 for List Row
// Less side-effecty than using react-measure.
const titleRowHeight = 66;

function getContentHeight(rowHeight, isSmallScreen) {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
}

class DiscoverGameOverview extends Component {

  //
  // Lifecycle

  constructor(props, context) {
    super(props, context);

    this.state = {
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
      imdbId,
      youTubeTrailerId,
      title,
      folder,
      year,
      overview,
      images,
      lists,
      posterWidth,
      posterHeight,
      rowHeight,
      isSmallScreen,
      isExisting,
      isExcluded,
      isRecommendation,
      isPopular,
      isTrending,
      isSelected,
      overviewOptions,
      ...otherProps
    } = this.props;

    const {
      isNewAddGameModalOpen,
      isExcludeGameModalOpen
    } = this.state;

    const elementStyle = {
      width: `${posterWidth}px`,
      height: `${posterHeight}px`
    };

    // Use steamAppId for slug if available, otherwise igdbId
    const titleSlug = steamAppId > 0 ? steamAppId : igdbId;
    const linkProps = isExisting ? { to: `/game/${titleSlug}` } : { onPress: this.onPress };

    const contentHeight = getContentHeight(rowHeight, isSmallScreen);
    const overviewHeight = contentHeight - titleRowHeight;

    return (
      <div className={styles.container}>
        <div className={styles.content}>
          <div className={styles.poster}>
            <div className={styles.posterContainer}>
              <div className={styles.editorSelect}>
                <CheckInput
                  className={styles.checkInput}
                  name={igdbId.toString()}
                  value={isSelected}
                  onChange={this.onChange}
                />
              </div>

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
                />
              </Link>
            </div>
          </div>

          <div className={styles.info} style={{ maxHeight: contentHeight }}>
            <div className={styles.titleRow}>
              <Link
                className={styles.title}
                {...linkProps}
              >
                {title}

                {
                  isExisting ?
                    <Icon
                      className={styles.alreadyExistsIcon}
                      name={icons.CHECK_CIRCLE}
                      size={30}
                      title={translate('AlreadyInYourLibrary')}
                    /> : null
                }
                {
                  isExcluded &&
                    <Icon
                      className={styles.exclusionIcon}
                      name={icons.DANGER}
                      size={30}
                      title={translate('GameAlreadyExcluded')}
                    />
                }
              </Link>

              <div className={styles.actions}>
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
                        igdbId={igdbId}
                        imdbId={imdbId}
                        youTubeTrailerId={youTubeTrailerId}
                      />
                    }
                  />
                </span>

                <IconButton
                  name={icons.REMOVE}
                  title={isExcluded ? translate('GameAlreadyExcluded') : translate('ExcludeGame')}
                  onPress={this.onExcludeGamePress}
                  isDisabled={isExcluded}
                />
              </div>
            </div>

            <div className={styles.lists}>
              {
                isRecommendation ?
                  <Label
                    kind={kinds.INFO}
                  >
                    <Icon
                      name={icons.RECOMMENDED}
                      size={10}
                      title={translate('GameIsRecommend')}
                    />
                  </Label> :
                  null
              }

              {
                isPopular ?
                  <Label
                    kind={kinds.INFO}
                  >
                    {translate('Popular')}
                  </Label> :
                  null
              }

              {
                isTrending ?
                  <Label
                    kind={kinds.INFO}
                  >
                    {translate('Trending')}
                  </Label> :
                  null
              }

              <ImportListList
                lists={lists}
              />
            </div>

            <div className={styles.details}>
              <div className={styles.overviewContainer}>
                <Link className={styles.overview} {...linkProps}>
                  <TextTruncate
                    line={Math.floor(overviewHeight / (defaultFontSize * lineHeight))}
                    text={overview}
                  />
                </Link>
              </div>

              <DiscoverGameOverviewInfo
                height={overviewHeight}
                year={year}
                {...overviewOptions}
                {...otherProps}
              />
            </div>
          </div>
        </div>

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

DiscoverGameOverview.propTypes = {
  igdbId: PropTypes.number.isRequired,
  steamAppId: PropTypes.number,
  imdbId: PropTypes.string,
  youTubeTrailerId: PropTypes.string,
  title: PropTypes.string.isRequired,
  folder: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  overview: PropTypes.string.isRequired,
  status: PropTypes.string.isRequired,
  images: PropTypes.arrayOf(PropTypes.object).isRequired,
  posterWidth: PropTypes.number.isRequired,
  posterHeight: PropTypes.number.isRequired,
  rowHeight: PropTypes.number.isRequired,
  overviewOptions: PropTypes.object.isRequired,
  showRelativeDates: PropTypes.bool.isRequired,
  shortDateFormat: PropTypes.string.isRequired,
  longDateFormat: PropTypes.string.isRequired,
  timeFormat: PropTypes.string.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  isExisting: PropTypes.bool.isRequired,
  isExcluded: PropTypes.bool.isRequired,
  isRecommendation: PropTypes.bool.isRequired,
  isPopular: PropTypes.bool.isRequired,
  isTrending: PropTypes.bool.isRequired,
  isSelected: PropTypes.bool,
  lists: PropTypes.arrayOf(PropTypes.number).isRequired,
  onSelectedChange: PropTypes.func.isRequired
};

DiscoverGameOverview.defaultProps = {
  lists: []
};

export default DiscoverGameOverview;
