import React from 'react';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableRow from 'Components/Table/TableRow';
import titleCase from 'Utilities/String/titleCase';

interface GameTitlesRowProps {
  title: string;
  sourceType: string;
}

function GameTitlesRow({ title, sourceType }: GameTitlesRowProps) {
  return (
    <TableRow>
      <TableRowCell>{title}</TableRowCell>

      <TableRowCell>{titleCase(sourceType)}</TableRowCell>
    </TableRow>
  );
}

export default GameTitlesRow;
