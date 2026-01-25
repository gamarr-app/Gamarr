import React from 'react';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import { ExtraFileType } from 'GameFile/ExtraFile';
import titleCase from 'Utilities/String/titleCase';
import ExtraFileDetailsPopover from './ExtraFileDetailsPopover';
import styles from './ExtraFileRow.css';

interface ExtraFileRowProps {
  id: number;
  extension: string;
  type: ExtraFileType;
  relativePath: string;
  title?: string;
  languageTags?: string[];
}

function ExtraFileRow(props: ExtraFileRowProps) {
  const { relativePath, extension, type, title, languageTags } = props;

  return (
    <TableRow>
      <TableRowCell className={styles.relativePath} title={relativePath}>
        {relativePath}
      </TableRowCell>

      <TableRowCell className={styles.extension} title={extension}>
        {extension}
      </TableRowCell>

      <TableRowCell className={styles.type} title={type}>
        {titleCase(type)}
      </TableRowCell>

      <TableRowCell className={styles.actions}>
        <ExtraFileDetailsPopover
          type={type}
          title={title}
          languageTags={languageTags}
        />
      </TableRowCell>
    </TableRow>
  );
}

export default ExtraFileRow;
