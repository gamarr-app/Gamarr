import Label from 'Components/Label';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import styles from './SelectGameRow.css';

interface SelectGameRowProps {
  title: string;
  igdbId: number;
  steamAppId: number;
  year: number;
}

function SelectGameRow({
  title,
  year,
  igdbId,
  steamAppId,
}: SelectGameRowProps) {
  return (
    <>
      <VirtualTableRowCell className={styles.title}>
        {title}
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.year}>{year}</VirtualTableRowCell>

      <VirtualTableRowCell className={styles.steamAppId}>
        {steamAppId ? <Label>{steamAppId}</Label> : null}
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.igdbId}>
        <Label>{igdbId}</Label>
      </VirtualTableRowCell>
    </>
  );
}

export default SelectGameRow;
