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
  rawgId?: number;
  title: string;
  year: number;
  images: Image[];
  source: 'igdb' | 'rawg';
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
  const igdbRecommendations = game?.igdbRecommendations;
  const rawgRecommendations = game?.rawgRecommendations;
  const allGames = useSelector(gamesSelector);

  const [externalGames, setExternalGames] = useState<ExternalGame[]>([]);
  const [isFetching, setIsFetching] = useState(false);
  const fetchedIgdbIdsRef = useRef<Set<number>>(new Set());
  const fetchedRawgIdsRef = useRef<Set<number>>(new Set());

  const igdbIds = useMemo(() => {
    return igdbRecommendations ?? [];
  }, [igdbRecommendations]);

  const rawgIds = useMemo(() => {
    return rawgRecommendations ?? [];
  }, [rawgRecommendations]);

  // Memoize library games that match IGDB recommendations
  const libraryGames = useMemo(() => {
    if (!igdbIds.length) {
      return [];
    }
    const igdbIdSet = new Set(igdbIds);
    return allGames.filter((g) => igdbIdSet.has(g.igdbId) && !g.isDlc);
  }, [igdbIds, allGames]);

  // Memoize which IGDB IDs are in the library
  const libraryIgdbIds = useMemo(() => {
    return new Set(allGames.map((g) => g.igdbId));
  }, [allGames]);

  // Find which IGDB recommendations are NOT in the library
  const missingIgdbIds = useMemo(() => {
    return igdbIds.filter((igdbId) => !libraryIgdbIds.has(igdbId));
  }, [igdbIds, libraryIgdbIds]);

  // Fetch external games from both RAWG and IGDB
  useEffect(() => {
    const igdbIdsToFetch = missingIgdbIds
      .filter((id) => !fetchedIgdbIdsRef.current.has(id))
      .slice(0, 4);

    const rawgIdsToFetch = rawgIds
      .filter((id) => !fetchedRawgIdsRef.current.has(id))
      .slice(0, 4);

    if (igdbIdsToFetch.length === 0 && rawgIdsToFetch.length === 0) {
      return;
    }

    // Mark as fetched immediately to prevent re-fetching
    igdbIdsToFetch.forEach((id) => fetchedIgdbIdsRef.current.add(id));
    rawgIdsToFetch.forEach((id) => fetchedRawgIdsRef.current.add(id));

    setIsFetching(true);

    const fetchGames = async () => {
      const fetchedGames: ExternalGame[] = [];
      const seenTitles = new Set<string>();

      // Fetch RAWG games first (better recommendations)
      await Promise.all(
        rawgIdsToFetch.map(async (rawgId) => {
          try {
            const { request } = createAjaxRequest({
              url: `/game/lookup/rawg?rawgId=${rawgId}`,
            });

            const data = await request;
            const titleKey = data?.title?.toLowerCase();
            if (data && data.title && !seenTitles.has(titleKey)) {
              seenTitles.add(titleKey);
              fetchedGames.push({
                igdbId: data.igdbId || 0,
                rawgId: data.rawgId,
                title: data.title,
                year: data.year,
                images: data.images || [],
                source: 'rawg',
              });
            }
          } catch {
            // Ignore failed fetches for individual games
          }
        })
      );

      // Fetch IGDB games
      await Promise.all(
        igdbIdsToFetch.map(async (igdbId) => {
          try {
            const { request } = createAjaxRequest({
              url: `/game/lookup/igdb?igdbId=${igdbId}`,
            });

            const data = await request;
            const titleKey = data?.title?.toLowerCase();
            if (data && data.title && !seenTitles.has(titleKey)) {
              seenTitles.add(titleKey);
              fetchedGames.push({
                igdbId: data.igdbId,
                title: data.title,
                year: data.year,
                images: data.images || [],
                source: 'igdb',
              });
            }
          } catch {
            // Ignore failed fetches for individual games
          }
        })
      );

      setExternalGames((prev) => {
        // Merge with existing, avoiding duplicates by title
        const existingTitles = new Set(prev.map((g) => g.title.toLowerCase()));
        const newGames = fetchedGames.filter(
          (g) => !existingTitles.has(g.title.toLowerCase())
        );
        return [...prev, ...newGames];
      });
      setIsFetching(false);
    };

    fetchGames();
  }, [missingIgdbIds, rawgIds]);

  // Don't show anything if there are no recommendations from either source
  if (!igdbIds.length && !rawgIds.length) {
    return null;
  }

  // Check if we're still waiting to fetch external games
  const hasPendingFetches =
    missingIgdbIds.some((id) => !fetchedIgdbIdsRef.current.has(id)) ||
    rawgIds.some((id) => !fetchedRawgIdsRef.current.has(id));

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
                key={`${ext.source}-${ext.source === 'rawg' ? ext.rawgId : ext.igdbId}`}
                className={styles.card}
                to={getPathWithUrlBase(
                  ext.source === 'rawg' && ext.rawgId
                    ? `/add/new?term=rawg:${ext.rawgId}`
                    : `/add/new?term=igdb:${ext.igdbId}`
                )}
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
