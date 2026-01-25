import React, { useEffect, useMemo, useRef, useState } from 'react';
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

// Stable selector that doesn't depend on recommendations array
const gamesSelector = createSelector(
  (state: AppState) => state.games.items,
  (items) => items
);

interface RecommendationsListProps {
  gameId: number;
}

function RecommendationsList({ gameId }: RecommendationsListProps) {
  const game = useGame(gameId);
  const gameRecommendations = game?.recommendations;
  const allGames = useSelector(gamesSelector);

  const [externalGames, setExternalGames] = useState<ExternalGame[]>([]);
  const [isFetching, setIsFetching] = useState(false);
  const fetchedIdsRef = useRef<Set<number>>(new Set());

  const recommendations = useMemo(() => {
    return gameRecommendations ?? [];
  }, [gameRecommendations]);

  // Memoize library games that match recommendations
  const libraryGames = useMemo(() => {
    if (!recommendations.length) {
      return [];
    }
    const igdbIdSet = new Set(recommendations);
    return allGames.filter((g) => igdbIdSet.has(g.igdbId) && !g.isDlc);
  }, [recommendations, allGames]);

  // Memoize which IGDB IDs are in the library
  const libraryIgdbIds = useMemo(() => {
    return new Set(allGames.map((g) => g.igdbId));
  }, [allGames]);

  // Find which recommendations are NOT in the library
  const missingIgdbIds = useMemo(() => {
    return recommendations.filter((igdbId) => !libraryIgdbIds.has(igdbId));
  }, [recommendations, libraryIgdbIds]);

  // Fetch external games - only runs once per unique set of missing IDs
  useEffect(() => {
    if (missingIgdbIds.length === 0) {
      return;
    }

    // Check if we already fetched these IDs
    const idsToFetch = missingIgdbIds
      .filter((id) => !fetchedIdsRef.current.has(id))
      .slice(0, 6);

    if (idsToFetch.length === 0) {
      return;
    }

    // Mark as fetched immediately to prevent re-fetching
    idsToFetch.forEach((id) => fetchedIdsRef.current.add(id));

    setIsFetching(true);

    const fetchGames = async () => {
      const fetchedGames: ExternalGame[] = [];
      const seenIds = new Set<number>();

      await Promise.all(
        idsToFetch.map(async (igdbId) => {
          try {
            const { request } = createAjaxRequest({
              url: `/game/lookup/igdb?igdbId=${igdbId}`,
            });

            const data = await request;
            if (data && data.title && !seenIds.has(data.igdbId)) {
              seenIds.add(data.igdbId);
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

      setExternalGames((prev) => {
        // Merge with existing, avoiding duplicates
        const existingIds = new Set(prev.map((g) => g.igdbId));
        const newGames = fetchedGames.filter((g) => !existingIds.has(g.igdbId));
        return [...prev, ...newGames];
      });
      setIsFetching(false);
    };

    fetchGames();
  }, [missingIgdbIds]);

  // Don't show anything if there are no recommendations
  if (!recommendations.length) {
    return null;
  }

  // Check if we're still waiting to fetch external games
  const hasPendingFetches = missingIgdbIds.some(
    (id) => !fetchedIdsRef.current.has(id)
  );

  // Don't show if nothing to display and nothing pending
  if (
    libraryGames.length === 0 &&
    !isFetching &&
    !hasPendingFetches &&
    externalGames.length === 0
  ) {
    return null;
  }

  const isLoading = isFetching || hasPendingFetches;

  return (
    <FieldSet legend={translate('SimilarGames')}>
      <div className={styles.container}>
        {isLoading && libraryGames.length === 0 && externalGames.length === 0 ? (
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
