import React from 'react';
import Label from 'Components/Label';
import VirtualTableRowCell from 'Components/Table/Cells/VirtualTableRowCell';
import styles from './SelectGameRow.css';

interface SelectGameRowProps {
  title: string;
  igdbId: number;
  imdbId?: string;
  year: number;
}

function SelectGameRow({ title, year, igdbId, imdbId }: SelectGameRowProps) {
  return (
    <>
      <VirtualTableRowCell className={styles.title}>
        {title}
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.year}>{year}</VirtualTableRowCell>

      <VirtualTableRowCell className={styles.imdbId}>
        {imdbId ? <Label>{imdbId}</Label> : null}
      </VirtualTableRowCell>

      <VirtualTableRowCell className={styles.igdbId}>
        <Label>{igdbId}</Label>
      </VirtualTableRowCell>
    </>
  );
}

export default SelectGameRow;
