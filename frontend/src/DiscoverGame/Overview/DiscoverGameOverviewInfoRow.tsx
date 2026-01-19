import React from 'react';
import Icon, { IconName } from 'Components/Icon';
import styles from './DiscoverGameOverviewInfoRow.css';

interface DiscoverGameOverviewInfoRowProps {
  title?: string;
  iconName: IconName;
  label: string | null;
}

function DiscoverGameOverviewInfoRow(props: DiscoverGameOverviewInfoRowProps) {
  const { title, iconName, label } = props;

  return (
    <div className={styles.infoRow} title={title}>
      <Icon className={styles.icon} name={iconName} size={14} />

      {label}
    </div>
  );
}

export default DiscoverGameOverviewInfoRow;
