import React from 'react';
import { Tag } from 'App/State/TagsAppState';
import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import GamePoster from 'Game/GamePoster';
import { SuggestedGame } from './GameSearchInput';
import styles from './GameSearchResult.css';

interface Match {
  key: string;
  refIndex: number;
}

interface GameSearchResultProps extends SuggestedGame {
  match: Match;
}

function GameSearchResult(props: GameSearchResultProps) {
  const { match, title, year, images, alternateTitles, igdbId, imdbId, tags } =
    props;

  let alternateTitle = null;
  let tag: Tag | null = null;

  if (match.key === 'alternateTitles.title') {
    alternateTitle = alternateTitles[match.refIndex];
  } else if (match.key === 'tags.label') {
    tag = tags[match.refIndex];
  }

  return (
    <div className={styles.result}>
      <GamePoster
        className={styles.poster}
        images={images}
        size={250}
        lazy={false}
        overflow={true}
      />

      <div className={styles.titles}>
        <div className={styles.title}>
          {title} {year > 0 ? `(${year})` : ''}
        </div>

        {alternateTitle ? (
          <div className={styles.alternateTitle}>{alternateTitle.title}</div>
        ) : null}

        {match.key === 'igdbId' && igdbId ? (
          <div className={styles.alternateTitle}>IgdbId: {igdbId}</div>
        ) : null}

        {match.key === 'imdbId' && imdbId ? (
          <div className={styles.alternateTitle}>ImdbId: {imdbId}</div>
        ) : null}

        {tag ? (
          <div className={styles.tagContainer}>
            <Label key={tag.id} kind={kinds.INFO}>
              {tag.label}
            </Label>
          </div>
        ) : null}
      </div>
    </div>
  );
}

export default GameSearchResult;
