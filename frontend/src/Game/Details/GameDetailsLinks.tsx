import Label from 'Components/Label';
import Link from 'Components/Link/Link';
import Game from 'Game/Game';
import { kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './GameDetailsLinks.css';

type GameDetailsLinksProps = Pick<
  Game,
  'steamAppId' | 'igdbSlug' | 'youTubeTrailerId'
>;

function GameDetailsLinks(props: GameDetailsLinksProps) {
  const { steamAppId, igdbSlug, youTubeTrailerId } = props;

  return (
    <div className={styles.links}>
      {steamAppId > 0 ? (
        <Link
          className={styles.link}
          to={`https://store.steampowered.com/app/${steamAppId}`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            Steam
          </Label>
        </Link>
      ) : null}

      {igdbSlug ? (
        <Link
          className={styles.link}
          to={`https://www.igdb.com/games/${igdbSlug}`}
        >
          <Label
            className={styles.linkLabel}
            kind={kinds.INFO}
            size={sizes.LARGE}
          >
            IGDB
          </Label>
        </Link>
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
