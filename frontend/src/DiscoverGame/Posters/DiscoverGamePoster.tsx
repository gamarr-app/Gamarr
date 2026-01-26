import { useCallback, useState } from 'react';
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
import { Image, Ratings } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import { icons } from 'Helpers/Props';
import { CheckInputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import translate from 'Utilities/String/translate';
import DiscoverGamePosterInfo from './DiscoverGamePosterInfo';
import styles from './DiscoverGamePoster.css';

interface DiscoverGamePosterProps {
  igdbId: number;
  steamAppId?: number;
  youTubeTrailerId?: string;
  title: string;
  year: number;
  overview: string;
  folder: string;
  status: string;
  images: Image[];
  posterWidth: number;
  posterHeight: number;
  showTitle: boolean;
  showIgdbRating: boolean;
  showMetacriticRating: boolean;
  ratings: Ratings;
  showRelativeDates: boolean;
  shortDateFormat: string;
  timeFormat: string;
  gameRuntimeFormat: string;
  isExisting: boolean;
  isExcluded: boolean;
  isSelected?: boolean;
  sortKey: string;
  onSelectedChange: (props: SelectStateInputProps) => void;
}

function DiscoverGamePoster(props: DiscoverGamePosterProps) {
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
    onSelectedChange,
    ...otherProps
  } = props;

  const [hasPosterError, setHasPosterError] = useState(false);
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

  const onChange = useCallback(
    ({ value, shiftKey }: CheckInputChanged) => {
      onSelectedChange({ id: igdbId, value, shiftKey });
    },
    [igdbId, onSelectedChange]
  );

  // Use steamAppId for slug if available, otherwise igdbId
  const titleSlug = steamAppId && steamAppId > 0 ? steamAppId : igdbId;
  const linkProps = isExisting ? { to: `/game/${titleSlug}` } : { onPress };

  const elementStyle = {
    width: `${posterWidth}px`,
    height: `${posterHeight}px`,
  };

  return (
    <div className={styles.content}>
      <div className={styles.posterContainer} title={title}>
        <div className={styles.editorSelect}>
          <CheckInput
            className={styles.checkInput}
            name={igdbId.toString()}
            value={isSelected}
            onChange={onChange}
          />
        </div>

        <Label className={styles.controls}>
          <IconButton
            className={styles.action}
            name={icons.REMOVE}
            title={
              isExcluded
                ? translate('GameAlreadyExcluded')
                : translate('ExcludeGame')
            }
            isDisabled={isExcluded}
            onPress={onExcludeGamePress}
          />
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
        </Label>

        {isExcluded && (
          <div className={styles.excluded} title={translate('Excluded')} />
        )}

        {isExisting && (
          <div className={styles.existing} title={translate('Existing')} />
        )}

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
        </Link>
      </div>

      {showTitle ? (
        <div className={styles.title} title={title}>
          {title}
        </div>
      ) : null}

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

export default DiscoverGamePoster;
