import React from 'react';
import { useSelector } from 'react-redux';
import Icon from 'Components/Icon';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import { icons } from 'Helpers/Props';
import Game from 'Game/Game';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import formatDate from 'Utilities/Date/formatDate';
import getRelativeDate from 'Utilities/Date/getRelativeDate';
import translate from 'Utilities/String/translate';
import styles from './GameReleaseDates.css';

type GameReleaseDatesProps = Pick<
  Game,
  'igdbId' | 'inCinemas' | 'digitalRelease' | 'physicalRelease'
>;

function GameReleaseDates({
  igdbId,
  inCinemas,
  digitalRelease,
  physicalRelease,
}: GameReleaseDatesProps) {
  const { showRelativeDates, shortDateFormat, longDateFormat, timeFormat } =
    useSelector(createUISettingsSelector());

  if (!inCinemas && !physicalRelease && !digitalRelease) {
    return (
      <div>
        <div className={styles.dateIcon}>
          <Icon name={icons.MISSING} />
        </div>

        <InlineMarkdown
          data={translate('NoGameReleaseDatesAvailable', {
            url: `https://www.thegamedb.org/game/${igdbId}`,
          })}
        />
      </div>
    );
  }

  return (
    <>
      {inCinemas ? (
        <div
          title={`${translate('InDevelopment')}: ${formatDate(
            inCinemas,
            longDateFormat
          )}`}
        >
          <div className={styles.dateIcon}>
            <Icon name={icons.IN_CINEMAS} />
          </div>

          {getRelativeDate({
            date: inCinemas,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}

      {digitalRelease ? (
        <div
          title={`${translate('DigitalRelease')}: ${formatDate(
            digitalRelease,
            longDateFormat
          )}`}
        >
          <div className={styles.dateIcon}>
            <Icon name={icons.GAME_FILE} />
          </div>

          {getRelativeDate({
            date: digitalRelease,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}

      {physicalRelease ? (
        <div
          title={`${translate('PhysicalRelease')}: ${formatDate(
            physicalRelease,
            longDateFormat
          )}`}
        >
          <div className={styles.dateIcon}>
            <Icon name={icons.DISC} />
          </div>

          {getRelativeDate({
            date: physicalRelease,
            shortDateFormat,
            showRelativeDates,
            timeFormat,
            timeForToday: false,
          })}
        </div>
      ) : null}
    </>
  );
}

export default GameReleaseDates;
