import { Component } from 'react';
import TextTruncate from 'react-text-truncate';
import { Navigation } from 'swiper';
import { Swiper, SwiperSlide } from 'swiper/react';
import type { NavigationOptions } from 'swiper/types/modules/navigation';
import EditGameCollectionModal from 'Collection/Edit/EditGameCollectionModal';
import CheckInput from 'Components/Form/CheckInput';
import Icon from 'Components/Icon';
import Label from 'Components/Label';
import IconButton from 'Components/Link/IconButton';
import MonitorToggleButton from 'Components/MonitorToggleButton';
import { GameStatus, Image } from 'Game/Game';
import GameGenres from 'Game/GameGenres';
import { icons, sizes } from 'Helpers/Props';
import QualityProfileName from 'Settings/Profiles/Quality/QualityProfileName';
import { SelectStateInputProps } from 'typings/props';
import translate from 'Utilities/String/translate';
import CollectionGameConnector from './CollectionGameConnector';
import CollectionGameLabelConnector from './CollectionGameLabelConnector';
import styles from './CollectionOverview.css';

// Import Swiper styles
import 'swiper/css';
import 'swiper/css/navigation';

// eslint-disable-next-line @typescript-eslint/no-var-requires, @typescript-eslint/no-require-imports
const dimensions = require('Styles/Variables/dimensions');
// eslint-disable-next-line @typescript-eslint/no-var-requires, @typescript-eslint/no-require-imports
const fonts = require('Styles/Variables/fonts');

const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10. 19 + 5 for List Row
// Less side-effecty than using react-measure.
const titleRowHeight = 100;

function getContentHeight(rowHeight: number, isSmallScreen: boolean): number {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
}

interface CollectionGame {
  igdbId: number;
  title: string;
  year: number;
  status: GameStatus;
  overview?: string;
  images: Image[];
  [key: string]: unknown;
}

interface OverviewOptions {
  showDetails: boolean;
  showOverview: boolean;
  showPosters: boolean;
  detailedProgressBar: boolean;
}

interface CollectionOverviewProps {
  id: number;
  monitored: boolean;
  qualityProfileId: number;
  minimumAvailability: string;
  searchOnAdd: boolean;
  rootFolderPath: string;
  igdbId: number;
  title: string;
  overview: string;
  games: CollectionGame[];
  genres: string[];
  missingGames: number;
  images: Image[];
  rowHeight: number;
  posterHeight: number;
  posterWidth: number;
  overviewOptions: OverviewOptions;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
  isSmallScreen: boolean;
  isSelected?: boolean;
  onMonitorTogglePress: (monitored: boolean) => void;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

interface CollectionOverviewState {
  isEditCollectionModalOpen: boolean;
  isNewAddGameModalOpen: boolean;
}

class CollectionOverview extends Component<
  CollectionOverviewProps,
  CollectionOverviewState
> {
  // eslint-disable-next-line react/sort-comp
  private _swiperPrevRef: HTMLSpanElement | null = null;
  private _swiperNextRef: HTMLSpanElement | null = null;

  //
  // Lifecycle

  constructor(props: CollectionOverviewProps) {
    super(props);

    this.state = {
      isEditCollectionModalOpen: false,
      isNewAddGameModalOpen: false,
    };
  }

  //
  // Control

  setSliderPrevRef = (ref: HTMLSpanElement | null) => {
    this._swiperPrevRef = ref;
  };

  setSliderNextRef = (ref: HTMLSpanElement | null) => {
    this._swiperNextRef = ref;
  };

  //
  // Listeners

  onPress = () => {
    this.setState({ isNewAddGameModalOpen: true });
  };

  onEditCollectionPress = () => {
    this.setState({ isEditCollectionModalOpen: true });
  };

  onEditCollectionModalClose = () => {
    this.setState({ isEditCollectionModalOpen: false });
  };

  onAddGameModalClose = () => {
    this.setState({ isNewAddGameModalOpen: false });
  };

  onChange = ({ value, shiftKey }: { value: boolean; shiftKey: boolean }) => {
    const { id, onSelectedChange } = this.props;

    onSelectedChange({ id, value, shiftKey });
  };

  //
  // Render

  render() {
    const {
      monitored,
      qualityProfileId,
      rootFolderPath,
      genres,
      id,
      title,
      games,
      overview,
      missingGames,
      posterHeight,
      posterWidth,
      rowHeight,
      isSmallScreen,
      isSelected,
      onMonitorTogglePress,
    } = this.props;

    const { showDetails, showOverview, showPosters, detailedProgressBar } =
      this.props.overviewOptions;

    const { isEditCollectionModalOpen } = this.state;

    const contentHeight = getContentHeight(rowHeight, isSmallScreen);
    const overviewHeight = contentHeight - titleRowHeight - posterHeight;

    return (
      <div>
        <div className={styles.content}>
          <div className={styles.editorSelect}>
            <CheckInput
              name={id.toString()}
              value={isSelected}
              onChange={this.onChange}
            />
          </div>
          <div className={styles.info} style={{ maxHeight: contentHeight }}>
            <div className={styles.titleRow}>
              <div className={styles.titleContainer}>
                <div className={styles.toggleMonitoredContainer}>
                  <MonitorToggleButton
                    className={styles.monitorToggleButton}
                    monitored={monitored}
                    size={isSmallScreen ? 20 : 25}
                    onPress={onMonitorTogglePress}
                  />
                </div>
                <div className={styles.title}>{title}</div>

                <IconButton
                  name={icons.EDIT}
                  title={translate('EditCollection')}
                  onPress={this.onEditCollectionPress}
                />
              </div>

              {showPosters && (
                <div className={styles.navigationButtons}>
                  <span ref={this.setSliderPrevRef}>
                    <IconButton
                      name={icons.ARROW_LEFT}
                      title={translate('ScrollGames')}
                      size={20}
                    />
                  </span>

                  <span ref={this.setSliderNextRef}>
                    <IconButton
                      name={icons.ARROW_RIGHT}
                      title={translate('ScrollGames')}
                      size={20}
                    />
                  </span>
                </div>
              )}
            </div>

            {showDetails && (
              <div className={styles.defaults}>
                <Label className={styles.detailsLabel} size={sizes.MEDIUM}>
                  <Icon name={icons.DRIVE} size={13} />
                  <span className={styles.status}>
                    {translate('CountMissingGamesFromLibrary', {
                      count: missingGames,
                    })}
                  </span>
                </Label>

                {!isSmallScreen && (
                  <Label className={styles.detailsLabel} size={sizes.MEDIUM}>
                    <Icon name={icons.PROFILE} size={13} />
                    <span className={styles.qualityProfileName}>
                      <QualityProfileName qualityProfileId={qualityProfileId} />
                    </span>
                  </Label>
                )}

                {!isSmallScreen && (
                  <Label className={styles.detailsLabel} size={sizes.MEDIUM}>
                    <Icon name={icons.FOLDER} size={13} />
                    <span className={styles.path}>{rootFolderPath}</span>
                  </Label>
                )}

                {!isSmallScreen && (
                  <Label className={styles.detailsLabel} size={sizes.MEDIUM}>
                    <Icon name={icons.GENRE} size={13} />
                    <GameGenres className={styles.genres} genres={genres} />
                  </Label>
                )}
              </div>
            )}

            {showOverview && (
              <div className={styles.details}>
                <div className={styles.overview}>
                  <TextTruncate
                    line={Math.floor(
                      overviewHeight / (defaultFontSize * lineHeight)
                    )}
                    text={overview}
                  />
                </div>
              </div>
            )}

            {showPosters ? (
              <div className={styles.sliderContainer}>
                <Swiper
                  slidesPerView="auto"
                  spaceBetween={10}
                  slidesPerGroup={3}
                  loop={false}
                  loopFillGroupWithBlank={true}
                  className="mySwiper"
                  modules={[Navigation]}
                  // eslint-disable-next-line react/jsx-no-bind
                  onInit={(swiper) => {
                    if (
                      swiper.params.navigation &&
                      typeof swiper.params.navigation !== 'boolean'
                    ) {
                      const nav = swiper.params.navigation as NavigationOptions;
                      nav.prevEl = this._swiperPrevRef;
                      nav.nextEl = this._swiperNextRef;
                    }
                    swiper.navigation.init();
                    swiper.navigation.update();
                  }}
                >
                  {games.map((game) => (
                    <SwiperSlide
                      key={game.igdbId}
                      style={{ width: posterWidth }}
                    >
                      <CollectionGameConnector
                        key={game.igdbId}
                        posterWidth={posterWidth}
                        posterHeight={posterHeight}
                        detailedProgressBar={detailedProgressBar}
                        collectionId={id}
                        {...game}
                      />
                    </SwiperSlide>
                  ))}
                </Swiper>
              </div>
            ) : (
              <div className={styles.labelsContainer}>
                {games.map((game) => (
                  <CollectionGameLabelConnector
                    key={game.igdbId}
                    collectionId={id}
                    {...game}
                  />
                ))}
              </div>
            )}
          </div>
        </div>

        <EditGameCollectionModal
          isOpen={isEditCollectionModalOpen}
          collectionId={id}
          onModalClose={this.onEditCollectionModalClose}
        />
      </div>
    );
  }
}

export default CollectionOverview;
