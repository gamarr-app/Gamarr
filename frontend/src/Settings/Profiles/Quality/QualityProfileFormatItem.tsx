import { useCallback } from 'react';
import NumberInput from 'Components/Form/NumberInput';
import { InputChanged } from 'typings/inputs';
import styles from './QualityProfileFormatItem.css';

interface QualityProfileFormatItemProps {
  formatId: number;
  name: string;
  score?: number;
  onScoreChange?: (formatId: number, value: number) => void;
}

function QualityProfileFormatItem({
  formatId,
  name,
  score = 0,
  onScoreChange,
}: QualityProfileFormatItemProps) {
  const handleScoreChange = useCallback(
    ({ value }: InputChanged<number | null>) => {
      if (onScoreChange && value !== null) {
        onScoreChange(formatId, value);
      }
    },
    [formatId, onScoreChange]
  );

  return (
    <div className={styles.qualityProfileFormatItemContainer}>
      <div className={styles.qualityProfileFormatItem}>
        <label className={styles.formatNameContainer}>
          <div className={styles.formatName}>{name}</div>
          <NumberInput
            className={styles.scoreInput}
            name={name}
            value={score}
            onChange={handleScoreChange}
          />
        </label>
      </div>
    </div>
  );
}

export default QualityProfileFormatItem;
