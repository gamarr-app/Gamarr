import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { useSelector } from 'react-redux';
import { Link } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import { Image } from 'Game/Game';
import GamePoster from 'Game/GamePoster';
import useGame from 'Game/useGame';
import { icons } from 'Helpers/Props';
import createAjaxRequest from 'Utilities/createAjaxRequest';
import getPathWithUrlBase from 'Utilities/getPathWithUrlBase';
import translate from 'Utilities/String/translate';
import styles from './RecommendationsList.css';

interface ExternalGame {
  igdbId: number;
  title: string;
  year: number;
  images: Image[];
}

function createRecommendedGamesSelector(igdbIds: number[]) {
  return createSelector(
    (state: AppState) => state.games,
    (games) => {
      const { items } = games;

      const igdbIdSet = new Set(igdbIds);
      const recommended = items.filter(
        (game) => igdbIdSet.has(game.igdbId) && !game.isDlc
      );

      // Return both the games in library and the set of IGDB IDs in library
      const libraryIgdbIds = new Set(items.map((g) => g.igdbId));

      return {
        libraryGames: recommended,
        libraryIgdbIds,
      };
    }
  );
}

interface RecommendationsListProps {
  gameId: number;
}

function RecommendationsList({ gameId }: RecommendationsListProps) {
  const game = useGame(gameId);
  const gameRecommendations = game?.recommendations;

  const recommendations = useMemo(() => {
    return gameRecommendations ?? [];
  }, [gameRecommendations]);

  const { libraryGames, libraryIgdbIds } = useSelector(
    createRecommendedGamesSelector(recommendations)
  );

  const [externalGames, setExternalGames] = useState<ExternalGame[]>([]);
  const [isFetching, setIsFetching] = useState(false);

  // Find which recommendations are NOT in the library
  const missingIgdbIds = useMemo(() => {
    return recommendations.filter((igdbId) => !libraryIgdbIds.has(igdbId));
  }, [recommendations, libraryIgdbIds]);

  // Fetch external games that aren't in the library
  const fetchExternalGames = useCallback(async () => {
    if (missingIgdbIds.length === 0) {
      return;
    }

    setIsFetching(true);
    const fetchedGames: ExternalGame[] = [];

    // Fetch up to 6 games to avoid too many requests
    const idsToFetch = missingIgdbIds.slice(0, 6);

    await Promise.all(
      idsToFetch.map(async (igdbId) => {
        try {
          const { request } = createAjaxRequest({
            url: `/game/lookup/igdb?igdbId=${igdbId}`,
          });

          const data = await request;
          if (data && data.title) {
            fetchedGames.push({
              igdbId: data.igdbId,
              title: data.title,
              year: data.year,
              images: data.images || [],
            });
          }
        } catch {
          // Ignore failed fetches for individual games
        }
      })
    );

    setExternalGames(fetchedGames);
    setIsFetching(false);
  }, [missingIgdbIds]);

  useEffect(() => {
    fetchExternalGames();
  }, [fetchExternalGames]);

  // Don't show anything if there are no recommendations
  if (!recommendations.length) {
    return null;
  }

  // Don't show if nothing to display (no library games and still loading or no external)
  if (libraryGames.length === 0 && !isFetching && externalGames.length === 0) {
    return null;
  }

  return (
    <FieldSet legend={translate('SimilarGames')}>
      <div className={styles.container}>
        {isFetching && libraryGames.length === 0 ? (
          <LoadingIndicator />
        ) : (
          <div className={styles.grid}>
            {/* Games in library */}
            {libraryGames.map((rec) => (
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

            {/* Games NOT in library - with Add link */}
            {externalGames.map((ext) => (
              <Link
                key={ext.igdbId}
                className={styles.card}
                to={getPathWithUrlBase(`/add/new?term=igdb:${ext.igdbId}`)}
              >
                <GamePoster
                  className={styles.poster}
                  images={ext.images}
                  size={250}
                />
                <div className={styles.info}>
                  <h4 className={styles.title}>{ext.title}</h4>
                  {ext.year > 0 && (
                    <span className={styles.year}>{ext.year}</span>
                  )}
                  <div className={styles.status}>
                    <Icon
                      name={icons.ADD}
                      className={styles.addIcon}
                      title={translate('AddToLibrary')}
                    />
                    <span className={styles.addText}>
                      {translate('NotInLibrary')}
                    </span>
                  </div>
                </div>
              </Link>
            ))}
          </div>
        )}
      </div>
    </FieldSet>
  );
}

export default RecommendationsList;
