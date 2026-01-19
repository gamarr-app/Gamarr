import React from 'react';
import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import Game from 'Game/Game';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './GameDetailsLinks.css';

type GameDetailsLinksProps = Pick<Game, 'igdbId' | 'youTubeTrailerId'>;

function GameDetailsLinks(props: GameDetailsLinksProps) {
  const { igdbId, youTubeTrailerId } = props;

  return (
    <div className={styles.links}>
      <Link
        className={styles.link}
        to={`https://www.igdb.com/games/${igdbId}`}
      >
        <Label
          className={styles.linkLabel}
          kind={kinds.INFO}
          size={sizes.LARGE}
        >
          IGDB
        </Label>
      </Link>

      <Link
        className={styles.link}
        to={`https://store.steampowered.com/search/?term=${igdbId}`}
      >
        <Label
          className={styles.linkLabel}
          kind={kinds.INFO}
          size={sizes.LARGE}
        >
          Steam
        </Label>
      </Link>

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
