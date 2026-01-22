import React from 'react';
import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import useGame from 'Game/useGame';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './DlcList.css';

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

interface ParentGameInfoProps {
  gameId: number;
}

function ParentGameInfo({ gameId }: ParentGameInfoProps) {
  const game = useGame(gameId);

  const isDlc = game?.isDlc ?? false;
  const parentIgdbId = game?.parentGameIgdbId;
  const gameTypeDisplayName = game?.gameTypeDisplayName ?? '';

  const parentGame = useSelector(createParentGameSelector(parentIgdbId));

  if (!isDlc) {
    return null;
  }

  return (
    <div className={styles.parentGameInfo}>
      <span className={styles.gameTypeBadge}>{gameTypeDisplayName}</span>
      {parentGame ? (
        <>
          <span className={styles.parentLabel}>{translate('ParentGame')}:</span>
          <Link
            className={styles.parentLink}
            to={getPathWithUrlBase(`/game/${parentGame.titleSlug}`)}
          >
            {parentGame.title}
          </Link>
        </>
      ) : parentIgdbId ? (
        <span className={styles.parentLabel}>
          {translate('ParentGameNotInLibrary')}
        </span>
      ) : null}
    </div>
  );
}

export default ParentGameInfo;
