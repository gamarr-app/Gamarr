import React from 'react';
import Icon, { IconName } from 'Components/Icon';
import styles from './GameIndexOverviewInfoRow.css';

interface GameIndexOverviewInfoRowProps {
  title?: string;
  iconName: IconName;
  label: string;
}

function GameIndexOverviewInfoRow(props: GameIndexOverviewInfoRowProps) {
  const { title, iconName, label } = props;

  return (
    <div className={styles.infoRow} title={title}>
      <Icon className={styles.icon} name={iconName} size={14} />

      {label}
    </div>
  );
}

export default GameIndexOverviewInfoRow;
