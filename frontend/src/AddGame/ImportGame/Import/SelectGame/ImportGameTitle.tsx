import Label from 'Components/Label';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ImportGameTitle.css';

interface ImportGameTitleProps {
  title: string;
  year: number;
  studio?: string;
  network?: string;
  isExistingGame: boolean;
}

function ImportGameTitle(props: ImportGameTitleProps) {
  const { title, year, studio, network, isExistingGame } = props;

  // Support both studio and network props for backwards compatibility
  const studioOrNetwork = studio || network;

  return (
    <div className={styles.titleContainer}>
      <div className={styles.title}>
        {title}

        {!title.contains(String(year)) && (
          <span className={styles.year}>({year})</span>
        )}
      </div>

      {!!studioOrNetwork && <Label>{studioOrNetwork}</Label>}

      {isExistingGame && (
        <Label kind={kinds.WARNING}>{translate('Existing')}</Label>
      )}
    </div>
  );
}

export default ImportGameTitle;
