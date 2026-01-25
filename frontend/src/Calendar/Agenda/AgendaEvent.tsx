import classNames from 'classnames';
import moment from 'moment';
import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import AppState from 'App/State/AppState';
import CalendarEventQueueDetails from 'Calendar/Events/CalendarEventQueueDetails';
import getStatusStyle from 'Calendar/getStatusStyle';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import useGameFile from 'GameFile/useGameFile';
import { icons, kinds } from 'Helpers/Props';
import { createQueueItemSelectorForHook } from 'Store/Selectors/createQueueItemSelector';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import translate from 'Utilities/String/translate';
import styles from './AgendaEvent.css';

interface AgendaEventProps {
  id: number;
  gameFileId: number;
  title: string;
  titleSlug: string;
  genres: string[];
  inCinemas?: string;
  digitalRelease?: string;
  physicalRelease?: string;
  sortDate: moment.Moment;
  isAvailable: boolean;
  monitored: boolean;
  hasFile: boolean;
  grabbed?: boolean;
  showDate: boolean;
}

function AgendaEvent({
  id,
  gameFileId,
  title,
  titleSlug,
  genres = [],
  inCinemas,
  digitalRelease,
  physicalRelease,
  sortDate,
  isAvailable,
  monitored: isMonitored,
  hasFile,
  grabbed,
  showDate,
}: AgendaEventProps) {
  const gameFile = useGameFile(gameFileId);
  const queueItem = useSelector(createQueueItemSelectorForHook(id));
  const { longDateFormat, enableColorImpairedMode } = useSelector(
    createUISettingsSelector()
  );

  const { showGameInformation, showCutoffUnmetIcon } = useSelector(
    (state: AppState) => state.calendar.options
  );

  const { eventDate, eventTitle, releaseIcon } = useMemo(() => {
    if (physicalRelease && sortDate.isSame(moment(physicalRelease), 'day')) {
      return {
        eventDate: physicalRelease,
        eventTitle: translate('PhysicalRelease'),
        releaseIcon: icons.DISC,
      };
    }

    if (digitalRelease && sortDate.isSame(moment(digitalRelease), 'day')) {
      return {
        eventDate: digitalRelease,
        eventTitle: translate('DigitalRelease'),
        releaseIcon: icons.GAME_FILE,
      };
    }

    if (inCinemas && sortDate.isSame(moment(inCinemas), 'day')) {
      return {
        eventDate: inCinemas,
        eventTitle: translate('InDevelopment'),
        releaseIcon: icons.IN_CINEMAS,
      };
    }

    return {
      eventDate: null,
      eventTitle: null,
      releaseIcon: null,
    };
  }, [inCinemas, digitalRelease, physicalRelease, sortDate]);

  const downloading = !!(queueItem || grabbed);
  const statusStyle = getStatusStyle(
    hasFile,
    downloading,
    isMonitored,
    isAvailable
  );
  const joinedGenres = genres.slice(0, 2).join(', ');
  const link = `/game/${titleSlug}`;

  return (
    <div className={styles.event}>
      <Link className={styles.underlay} to={link} />

      <div className={styles.overlay}>
        <div className={styles.date}>
          {showDate && eventDate
            ? moment(eventDate).format(longDateFormat)
            : null}
        </div>

        <div className={styles.releaseIcon}>
          {releaseIcon ? (
            <Icon name={releaseIcon} kind={kinds.DEFAULT} title={eventTitle} />
          ) : null}
        </div>

        <div
          className={classNames(
            styles.eventWrapper,
            styles[statusStyle],
            enableColorImpairedMode && 'colorImpaired'
          )}
        >
          <div className={styles.gameTitle}>{title}</div>

          {showGameInformation ? (
            <div className={styles.genres}>{joinedGenres}</div>
          ) : null}

          {queueItem ? (
            <span className={styles.statusIcon}>
              <CalendarEventQueueDetails {...queueItem} />
            </span>
          ) : null}

          {!queueItem && grabbed ? (
            <Icon
              className={styles.statusIcon}
              name={icons.DOWNLOADING}
              title={translate('GameIsDownloading')}
            />
          ) : null}

          {showCutoffUnmetIcon && gameFile && gameFile.qualityCutoffNotMet ? (
            <Icon
              className={styles.statusIcon}
              name={icons.GAME_FILE}
              kind={kinds.WARNING}
              title={translate('QualityCutoffNotMet')}
            />
          ) : null}
        </div>
      </div>
    </div>
  );
}

export default AgendaEvent;
