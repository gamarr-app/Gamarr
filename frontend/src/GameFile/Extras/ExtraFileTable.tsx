import ExtraFileTableContentConnector from './ExtraFileTableContentConnector';
import styles from './ExtraFileTable.css';

interface ExtraFileTableProps {
  gameId: number;
}

function ExtraFileTable(props: ExtraFileTableProps) {
  const { gameId } = props;

  return (
    <div className={styles.container}>
      <ExtraFileTableContentConnector gameId={gameId} />
    </div>
  );
}

export default ExtraFileTable;
