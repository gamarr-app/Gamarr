import React from 'react';
import SpinnerIcon from 'Components/SpinnerIcon';
import { icons } from 'Helpers/Props';
import styles from './DiscoverGameFooterLabel.css';

interface DiscoverGameFooterLabelProps {
  className?: string;
  label: string;
  isSaving: boolean;
}

function DiscoverGameFooterLabel({
  className = styles.label,
  label,
  isSaving,
}: DiscoverGameFooterLabelProps) {
  return (
    <div className={className}>
      {label}

      {isSaving ? (
        <SpinnerIcon
          className={styles.savingIcon}
          name={icons.SPINNER}
          isSpinning={true}
        />
      ) : null}
    </div>
  );
}

export default DiscoverGameFooterLabel;
