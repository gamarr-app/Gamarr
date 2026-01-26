import { useCallback, useRef, useState } from 'react';
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

function CollectionOverview(props: CollectionOverviewProps) {
  const {
    id,
    monitored,
    qualityProfileId,
    rootFolderPath,
    genres,
    title,
    games,
    overview,
    missingGames,
    posterHeight,
    posterWidth,
    rowHeight,
    isSmallScreen,
    isSelected,
    overviewOptions,
    onMonitorTogglePress,
    onSelectedChange,
  } = props;

  const { showDetails, showOverview, showPosters, detailedProgressBar } =
    overviewOptions;

  const [isEditCollectionModalOpen, setIsEditCollectionModalOpen] =
    useState(false);

  const swiperPrevRef = useRef<HTMLSpanElement | null>(null);
  const swiperNextRef = useRef<HTMLSpanElement | null>(null);

  const setSliderPrevRef = useCallback((ref: HTMLSpanElement | null) => {
    swiperPrevRef.current = ref;
  }, []);

  const setSliderNextRef = useCallback((ref: HTMLSpanElement | null) => {
    swiperNextRef.current = ref;
  }, []);

  const onEditCollectionPress = useCallback(() => {
    setIsEditCollectionModalOpen(true);
  }, []);

  const onEditCollectionModalClose = useCallback(() => {
    setIsEditCollectionModalOpen(false);
  }, []);

  const onChange = useCallback(
    ({ value, shiftKey }: { value: boolean; shiftKey: boolean }) => {
      onSelectedChange({ id, value, shiftKey });
    },
    [id, onSelectedChange]
  );

  const contentHeight = getContentHeight(rowHeight, isSmallScreen);
  const overviewHeight = contentHeight - titleRowHeight - posterHeight;

  return (
    <div>
      <div className={styles.content}>
        <div className={styles.editorSelect}>
          <CheckInput
            name={id.toString()}
            value={isSelected}
            onChange={onChange}
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
                onPress={onEditCollectionPress}
              />
            </div>

            {showPosters && (
              <div className={styles.navigationButtons}>
                <span ref={setSliderPrevRef}>
                  <IconButton
                    name={icons.ARROW_LEFT}
                    title={translate('ScrollGames')}
                    size={20}
                  />
                </span>

                <span ref={setSliderNextRef}>
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
                    nav.prevEl = swiperPrevRef.current;
                    nav.nextEl = swiperNextRef.current;
                  }
                  swiper.navigation.init();
                  swiper.navigation.update();
                }}
              >
                {games.map((game) => (
                  <SwiperSlide key={game.igdbId} style={{ width: posterWidth }}>
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
        onModalClose={onEditCollectionModalClose}
      />
    </div>
  );
}

export default CollectionOverview;
