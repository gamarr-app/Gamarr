import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoDiscoverGame.css';

interface NoDiscoverGameProps {
  totalItems: number;
}

function NoDiscoverGame({ totalItems }: NoDiscoverGameProps) {
  if (totalItems > 0) {
    return (
      <div>
        <div className={styles.message}>
          {translate('AllGamesHiddenDueToFilter')}
        </div>
      </div>
    );
  }

  return (
    <div>
      <div className={styles.message}>{translate('NoListRecommendations')}</div>

      <div className={styles.buttonContainer}>
        <Button to="/add/import" kind={kinds.PRIMARY}>
          {translate('ImportExistingGames')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button to="/add/new" kind={kinds.PRIMARY}>
          {translate('AddNewGame')}
        </Button>
      </div>

      <div className={styles.buttonContainer}>
        <Button to="/settings/importlists" kind={kinds.PRIMARY}>
          {translate('AddImportList')}
        </Button>
      </div>
    </div>
  );
}

export default NoDiscoverGame;
