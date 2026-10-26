import { useCallback } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import ImportGameTitle from './ImportGameTitle';
import styles from './ImportGameSearchResult.css';

export interface ImportGameSearchResultGame {
  igdbId: number;
  steamAppId?: number;
  title: string;
  year: number;
  studio?: string;
}

interface ImportGameSearchResultProps {
  // `igdbId`/`steamAppId` are also read as ownProps by the connector's
  // existing-game selector.
  igdbId: number;
  steamAppId?: number;
  game: ImportGameSearchResultGame;
  title: string;
  year: number;
  studio?: string;
  isExistingGame: boolean;
  onPress: (game: ImportGameSearchResultGame) => void;
}

function ImportGameSearchResult(props: ImportGameSearchResultProps) {
  const { game, title, year, studio, isExistingGame, onPress } = props;

  // Pass the result itself, not an id: Steam-sourced results all share
  // `igdbId: 0`, so an id would not identify which row was clicked.
  const handlePress = useCallback(() => {
    onPress(game);
  }, [game, onPress]);

  return (
    <div className={styles.container}>
      <Link className={styles.game} onPress={handlePress}>
        <ImportGameTitle
          title={title}
          year={year}
          network={studio}
          isExistingGame={isExistingGame}
        />
      </Link>

      <Link
        className={styles.igdbLink}
        to={`https://www.igdb.com/search?type=1&q=${encodeURIComponent(title)}`}
      >
        <Icon
          className={styles.igdbLinkIcon}
          name={icons.EXTERNAL_LINK}
          size={16}
        />
      </Link>
    </div>
  );
}

export default ImportGameSearchResult;
