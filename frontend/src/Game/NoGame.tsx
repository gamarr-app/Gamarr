import Button from 'Components/Link/Button';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './NoGame.css';

interface NoGameProps {
  totalItems: number;
}

function NoGame(props: NoGameProps) {
  const { totalItems } = props;

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
      <div className={styles.message}>{translate('NoGamesExist')}</div>

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
    </div>
  );
}

export default NoGame;
