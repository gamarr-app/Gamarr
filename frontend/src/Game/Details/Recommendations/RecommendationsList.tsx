import React from 'react';
import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import GamePoster from 'Game/GamePoster';
import useGame from 'Game/useGame';
import { icons } from 'Helpers/Props';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './RecommendationsList.css';

function createRecommendedGamesSelector(igdbIds: number[]) {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      const { items } = games;

      const igdbIdSet = new Set(igdbIds);
      const recommended = items.filter(
        (game) => igdbIdSet.has(game.igdbId) && !game.isDlc
      );

      return recommended;
    }
  );
}

interface RecommendationsListProps {
  gameId: number;
}

function RecommendationsList({ gameId }: RecommendationsListProps) {
  const game = useGame(gameId);
  const recommendations = game?.recommendations ?? [];

  const recommendedGames = useSelector(
    createRecommendedGamesSelector(recommendations)
  );

  if (!recommendations.length || !recommendedGames.length) {
    return null;
  }

  return (
    <FieldSet legend={translate('SimilarGames')}>
      <div className={styles.container}>
        <div className={styles.grid}>
          {recommendedGames.map((rec) => (
            <Link
              key={rec.id}
              className={styles.card}
              to={getPathWithUrlBase(`/game/${rec.titleSlug}`)}
            >
              <GamePoster
                className={styles.poster}
                images={rec.images}
                size={250}
              />
              <div className={styles.info}>
                <h4 className={styles.title}>{rec.title}</h4>
                <div className={styles.status}>
                  <Icon
                    name={rec.monitored ? icons.MONITORED : icons.UNMONITORED}
                    className={
                      rec.monitored
                        ? styles.monitoredIcon
                        : styles.unmonitoredIcon
                    }
                    title={
                      rec.monitored
                        ? translate('Monitored')
                        : translate('Unmonitored')
                    }
                  />
                  <Icon
                    name={rec.hasFile ? icons.CHECK : icons.MISSING}
                    className={rec.hasFile ? styles.hasFile : styles.noFile}
                    title={
                      rec.hasFile
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

export default RecommendationsList;
