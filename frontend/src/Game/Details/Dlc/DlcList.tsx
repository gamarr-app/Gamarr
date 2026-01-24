import React from 'react';
import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FieldSet from 'Components/FieldSet';
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

function createParentGameSelector(parentIgdbId: number | undefined) {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      if (!parentIgdbId) {
        return null;
      }

      const { items } = games;
      return items.find((game) => game.igdbId === parentIgdbId) ?? null;
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
  const isDlc = game?.isDlc ?? false;
  const parentGameIgdbId = game?.parentGameIgdbId;

  const { dlcs, isFetching, isPopulated } = useSelector(
    createDlcsSelector(igdbId)
  );

  const parentGame = useSelector(createParentGameSelector(parentGameIgdbId));

  // This game is a DLC - show parent game
  if (isDlc || parentGameIgdbId) {
    if (!parentGame) {
      // Parent not in library
      if (parentGameIgdbId) {
        return (
          <FieldSet legend={translate('ParentGame')}>
            <div className={styles.container}>
              <div className={styles.blankpad}>
                {translate('ParentGameNotInLibrary')}
              </div>
            </div>
          </FieldSet>
        );
      }
      return null;
    }

    // Show parent game in same card style as DLCs
    return (
      <FieldSet legend={translate('ParentGame')}>
        <div className={styles.container}>
          <div className={styles.dlcGrid}>
            <Link
              className={styles.dlcCard}
              to={getPathWithUrlBase(`/game/${parentGame.titleSlug}`)}
            >
              <GamePoster
                className={styles.dlcPoster}
                images={parentGame.images}
                size={250}
              />
              <div className={styles.dlcInfo}>
                <h4 className={styles.dlcTitle}>{parentGame.title}</h4>
                <div className={styles.dlcStatus}>
                  <Icon
                    name={parentGame.monitored ? icons.MONITORED : icons.UNMONITORED}
                    className={
                      parentGame.monitored
                        ? styles.monitoredIcon
                        : styles.unmonitoredIcon
                    }
                    title={
                      parentGame.monitored
                        ? translate('Monitored')
                        : translate('Unmonitored')
                    }
                  />
                  <Icon
                    name={parentGame.hasFile ? icons.CHECK : icons.MISSING}
                    className={parentGame.hasFile ? styles.hasFile : styles.noFile}
                    title={
                      parentGame.hasFile
                        ? translate('Downloaded')
                        : translate('Missing')
                    }
                  />
                </div>
              </div>
            </Link>
          </div>
        </div>
      </FieldSet>
    );
  }

  if (isFetching) {
    return (
      <FieldSet legend={translate('DlcAndExpansions')}>
        <div className={styles.container}>
          <LoadingIndicator />
        </div>
      </FieldSet>
    );
  }

  // If there are no DLCs listed and no DLC IDs, don't show anything
  if (!dlcs.length && !dlcIds.length) {
    return null;
  }

  // Show DLCs that are in the library
  if (dlcs.length > 0) {
    return (
      <FieldSet legend={translate('DlcAndExpansions')}>
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
      </FieldSet>
    );
  }

  // Show info about known DLCs not in the library
  if (dlcCount > 0 && isPopulated) {
    return (
      <FieldSet legend={translate('DlcAndExpansions')}>
        <div className={styles.container}>
          <div className={styles.blankpad}>
            {translate('DlcAvailableCount', { count: dlcCount })}
          </div>
        </div>
      </FieldSet>
    );
  }

  return null;
}

export default DlcList;
