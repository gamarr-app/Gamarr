import { useCallback, useState } from 'react';
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
import { Image, Ratings } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import { icons, kinds } from 'Helpers/Props';
import dimensions from 'Styles/Variables/dimensions';
import fonts from 'Styles/Variables/fonts';
import { CheckInputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import translate from 'Utilities/String/translate';
import DiscoverGameOverviewInfo from './DiscoverGameOverviewInfo';
import styles from './DiscoverGameOverview.css';

const columnPadding = parseInt(dimensions.gameIndexColumnPadding);
const columnPaddingSmallScreen = parseInt(
  dimensions.gameIndexColumnPaddingSmallScreen
);
const defaultFontSize = parseInt(fonts.defaultFontSize);
const lineHeight = parseFloat(fonts.lineHeight);

// Hardcoded height beased on line-height of 32 + bottom margin of 10. 19 + 5 for List Row
// Less side-effecty than using react-measure.
const titleRowHeight = 66;

function getContentHeight(rowHeight: number, isSmallScreen: boolean) {
  const padding = isSmallScreen ? columnPaddingSmallScreen : columnPadding;

  return rowHeight - padding;
}

interface OverviewOptions {
  size: string;
  showYear: boolean;
  showStudio: boolean;
  showGenres: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  showCertification: boolean;
}

interface DiscoverGameOverviewProps {
  igdbId: number;
  steamAppId?: number;
  youTubeTrailerId?: string;
  title: string;
  folder: string;
  year: number;
  overview: string;
  status: string;
  images: Image[];
  posterWidth: number;
  posterHeight: number;
  rowHeight: number;
  overviewOptions: OverviewOptions;
  showRelativeDates: boolean;
  shortDateFormat: string;
  longDateFormat: string;
  timeFormat: string;
  isSmallScreen: boolean;
  isExisting: boolean;
  isExcluded: boolean;
  isRecommendation: boolean;
  isPopular: boolean;
  isTrending: boolean;
  isSelected?: boolean;
  lists?: number[];
  sortKey: string;
  studio?: string;
  genres: string[];
  certification?: string;
  ratings?: Ratings;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function DiscoverGameOverview(props: DiscoverGameOverviewProps) {
  const {
    igdbId,
    steamAppId,
    youTubeTrailerId,
    title,
    folder,
    year,
    overview,
    images,
    lists = [],
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
    onSelectedChange,
    ...otherProps
  } = props;

  const [isNewAddGameModalOpen, setIsNewAddGameModalOpen] = useState(false);
  const [isExcludeGameModalOpen, setIsExcludeGameModalOpen] = useState(false);

  const onPress = useCallback(() => {
    setIsNewAddGameModalOpen(true);
  }, []);

  const onAddGameModalClose = useCallback(() => {
    setIsNewAddGameModalOpen(false);
  }, []);

  const onExcludeGamePress = useCallback(() => {
    setIsExcludeGameModalOpen(true);
  }, []);

  const onExcludeGameModalClose = useCallback(() => {
    setIsExcludeGameModalOpen(false);
  }, []);

  const onChange = useCallback(
    ({ value, shiftKey }: CheckInputChanged) => {
      onSelectedChange({ id: igdbId, value, shiftKey });
    },
    [igdbId, onSelectedChange]
  );

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  // Use steamAppId for slug if available, otherwise igdbId
  const titleSlug = steamAppId && steamAppId > 0 ? steamAppId : igdbId;
  const linkProps = isExisting ? { to: `/game/${titleSlug}` } : { onPress };

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
                onChange={onChange}
              />
            </div>

            <Link className={styles.link} style={elementStyle} {...linkProps}>
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
            <Link className={styles.title} {...linkProps}>
              {title}

              {isExisting ? (
                <Icon
                  className={styles.alreadyExistsIcon}
                  name={icons.CHECK_CIRCLE}
                  size={30}
                  title={translate('AlreadyInYourLibrary')}
                />
              ) : null}
              {isExcluded && (
                <Icon
                  className={styles.exclusionIcon}
                  name={icons.DANGER}
                  size={30}
                  title={translate('GameAlreadyExcluded')}
                />
              )}
            </Link>

            <div className={styles.actions}>
              <span className={styles.externalLinks}>
                <Popover
                  anchor={<Icon name={icons.EXTERNAL_LINK} size={12} />}
                  title={translate('Links')}
                  body={
                    <GameDetailsLinks
                      steamAppId={steamAppId ?? 0}
                      igdbSlug={undefined}
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
                onPress={onExcludeGamePress}
              />
            </div>
          </div>

          <div className={styles.lists}>
            {isRecommendation ? (
              <Label kind={kinds.INFO}>
                <Icon
                  name={icons.RECOMMENDED}
                  size={10}
                  title={translate('GameIsRecommend')}
                />
              </Label>
            ) : null}

            {isPopular ? (
              <Label kind={kinds.INFO}>{translate('Popular')}</Label>
            ) : null}

            {isTrending ? (
              <Label kind={kinds.INFO}>{translate('Trending')}</Label>
            ) : null}

            <ImportListList lists={lists} />
          </div>

          <div className={styles.details}>
            <div className={styles.overviewContainer}>
              <Link className={styles.overview} {...linkProps}>
                <TextTruncate
                  line={Math.floor(
                    overviewHeight / (defaultFontSize * lineHeight)
                  )}
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
        onModalClose={onAddGameModalClose}
      />

      <ExcludeGameModal
        isOpen={isExcludeGameModalOpen}
        igdbId={igdbId}
        title={title}
        year={year}
        onModalClose={onExcludeGameModalClose}
      />
    </div>
  );
}

export default DiscoverGameOverview;
