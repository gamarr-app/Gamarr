import { useCallback } from 'react';
import Icon from 'Components/Icon';
import Link from 'Components/Link/Link';
import { icons } from 'Helpers/Props';
import ImportGameTitle from './ImportGameTitle';
import styles from './ImportGameSearchResult.css';

interface ImportGameSearchResultProps {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
  isExistingGame: boolean;
  onPress: (igdbId: number) => void;
}

function ImportGameSearchResult(props: ImportGameSearchResultProps) {
  const { igdbId, title, year, studio, isExistingGame, onPress } = props;

  const handlePress = useCallback(() => {
    onPress(igdbId);
  }, [igdbId, onPress]);

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
        to={`https://www.thegamedb.org/game/${igdbId}`}
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
