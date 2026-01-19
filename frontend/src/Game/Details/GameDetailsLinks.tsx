import React from 'react';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import { kinds, sizes } from 'Helpers/Props';
import Game from 'Game/Game';
import translate from 'Utilities/String/translate';
import styles from './GameDetailsLinks.css';

type GameDetailsLinksProps = Pick<
  Game,
  'igdbId' | 'imdbId' | 'youTubeTrailerId'
>;

function GameDetailsLinks(props: GameDetailsLinksProps) {
  const { igdbId, imdbId, youTubeTrailerId } = props;

  return (
    <div className={styles.links}>
      <Link
        className={styles.link}
        to={`https://www.thegamedb.org/game/${igdbId}`}
      >
        <Label
          className={styles.linkLabel}
          kind={kinds.INFO}
          size={sizes.LARGE}
        >
          {translate('TMDb')}
        </Label>
      </Link>

      <Link
        className={styles.link}
        to={`https://trakt.tv/search/igdb/${igdbId}?id_type=game`}
      >
        <Label
          className={styles.linkLabel}
          kind={kinds.INFO}
          size={sizes.LARGE}
        >
          {translate('Trakt')}
        </Label>
      </Link>

      <Link
        className={styles.link}
        to={`https://letterboxd.com/igdb/${igdbId}`}
      >
        <Label
          className={styles.linkLabel}
          kind={kinds.INFO}
          size={sizes.LARGE}
        >
          {translate('Letterboxd')}
        </Label>
      </Link>

      {imdbId ? (
        <>
          <Link
            className={styles.link}
            to={`https://imdb.com/title/${imdbId}/`}
          >
            <Label
              className={styles.linkLabel}
              kind={kinds.INFO}
              size={sizes.LARGE}
            >
              {translate('IMDb')}
            </Label>
          </Link>

          <Link className={styles.link} to={`https://gamechat.org/${imdbId}/`}>
            <Label
              className={styles.linkLabel}
              kind={kinds.INFO}
              size={sizes.LARGE}
            >
              {translate('GameChat')}
            </Label>
          </Link>

          <Link
            className={styles.link}
            to={`https://mdblist.com/game/${imdbId}`}
          >
            <Label
              className={styles.linkLabel}
              kind={kinds.INFO}
              size={sizes.LARGE}
            >
              MDBList
            </Label>
          </Link>

          <Link
            className={styles.link}
            to={`https://www.blu-ray.com/search/?quicksearch=1&quicksearch_keyword=${imdbId}&section=theatrical`}
          >
            <Label
              className={styles.linkLabel}
              kind={kinds.INFO}
              size={sizes.LARGE}
            >
              Blu-ray
            </Label>
          </Link>
        </>
      ) : null}

      {youTubeTrailerId ? (
        <Link
          className={styles.link}
          to={`https://www.youtube.com/watch?v=${youTubeTrailerId}`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.DANGER}
            size={sizes.LARGE}
          >
            {translate('Trailer')}
          </Label>
        </Link>
      ) : null}
    </div>
  );
}

export default GameDetailsLinks;
