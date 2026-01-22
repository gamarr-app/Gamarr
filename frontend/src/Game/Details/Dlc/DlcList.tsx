import React from 'react';
import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import GamePoster from 'Game/GamePoster';
import useGame from 'Game/useGame';
import { icons } from 'Helpers/Props';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './DlcList.css';

function createDlcsSelector(parentIgdbId: number) {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      const { items, isFetching, isPopulated } = games;

      // Find all games that have this game as their parent
      const dlcs = items.filter(
        (game) => game.parentGameIgdbId === parentIgdbId
      );

      return {
        dlcs,
        isFetching,
        isPopulated,
      };
    }
  );
}

interface DlcListProps {
  gameId: number;
}

function DlcList({ gameId }: DlcListProps) {
  const game = useGame(gameId);

  const igdbId = game?.igdbId ?? 0;
  const dlcIds = game?.dlcIds ?? [];
  const dlcCount = game?.dlcCount ?? 0;

  const { dlcs, isFetching, isPopulated } = useSelector(
    createDlcsSelector(igdbId)
  );

  if (isFetching) {
    return (
      <div className={styles.container}>
        <LoadingIndicator />
      </div>
    );
  }

  // If there are no DLCs listed and no DLC IDs, don't show anything
  if (!dlcs.length && !dlcIds.length) {
    return null;
  }

  // Show DLCs that are in the library
  if (dlcs.length > 0) {
    return (
      <div className={styles.container}>
        <div className={styles.dlcGrid}>
          {dlcs.map((dlc) => (
            <Link
              key={dlc.id}
              className={styles.dlcCard}
              to={getPathWithUrlBase(`/game/${dlc.titleSlug}`)}
            >
              <GamePoster
                className={styles.dlcPoster}
                images={dlc.images}
                size={250}
              />
              <div className={styles.dlcInfo}>
                <h4 className={styles.dlcTitle}>{dlc.title}</h4>
                <span className={styles.dlcType}>
                  {dlc.gameTypeDisplayName}
                </span>
                <div className={styles.dlcStatus}>
                  <Icon
                    name={dlc.monitored ? icons.MONITORED : icons.UNMONITORED}
                    className={
                      dlc.monitored
                        ? styles.monitoredIcon
                        : styles.unmonitoredIcon
                    }
                    title={
                      dlc.monitored
                        ? translate('Monitored')
                        : translate('Unmonitored')
                    }
                  />
                  <Icon
                    name={dlc.hasFile ? icons.CHECK : icons.MISSING}
                    className={dlc.hasFile ? styles.hasFile : styles.noFile}
                    title={
                      dlc.hasFile
                        ? translate('Downloaded')
                        : translate('Missing')
                    }
                  />
                </div>
              </div>
            </Link>
          ))}
        </div>
      </div>
    );
  }

  // Show info about known DLCs not in the library
  if (dlcCount > 0 && isPopulated) {
    return (
      <div className={styles.container}>
        <div className={styles.blankpad}>
          {translate('DlcAvailableCount', { count: dlcCount })}
        </div>
      </div>
    );
  }

  return null;
}

export default DlcList;
