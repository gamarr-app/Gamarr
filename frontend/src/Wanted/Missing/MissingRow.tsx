import React from 'react';
import RelativeDateCell from 'Components/Table/Cells/RelativeDateCell';
import TableRowCell from 'Components/Table/Cells/TableRowCell';
import TableSelectCell from 'Components/Table/Cells/TableSelectCell';
import Column from 'Components/Table/Column';
import TableRow from 'Components/Table/TableRow';
import GameSearchCell from 'Game/GameSearchCell';
import GameStatus from 'Game/GameStatus';
import GameTitleLink from 'Game/GameTitleLink';
import { SelectStateInputProps } from 'typings/props';
import styles from './MissingRow.css';

interface MissingRowProps {
  id: number;
  gameFileId?: number;
  inCinemas?: string;
  digitalRelease?: string;
  physicalRelease?: string;
  lastSearchTime?: string;
  title: string;
  year: number;
  titleSlug: string;
  isSelected?: boolean;
  columns: Column[];
  onSelectedChange: (options: SelectStateInputProps) => void;
}

function MissingRow({
  id,
  gameFileId,
  inCinemas,
  digitalRelease,
  physicalRelease,
  lastSearchTime,
  title,
  year,
  titleSlug,
  isSelected,
  columns,
  onSelectedChange,
}: MissingRowProps) {
  if (!title) {
    return null;
  }

  return (
    <TableRow>
      <TableSelectCell
        id={id}
        isSelected={isSelected}
        onSelectedChange={onSelectedChange}
      />

      {columns.map((column) => {
        const { name, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'gameMetadata.sortTitle') {
          return (
            <TableRowCell key={name}>
              <GameTitleLink titleSlug={titleSlug} title={title} />
            </TableRowCell>
          );
        }

        if (name === 'gameMetadata.year') {
          return <TableRowCell key={name}>{year}</TableRowCell>;
        }

        if (name === 'gameMetadata.inCinemas') {
          return (
            <RelativeDateCell
              key={name}
              date={inCinemas}
              timeForToday={false}
            />
          );
        }

        if (name === 'gameMetadata.digitalRelease') {
          return (
            <RelativeDateCell
              key={name}
              date={digitalRelease}
              timeForToday={false}
            />
          );
        }

        if (name === 'gameMetadata.physicalRelease') {
          return (
            <RelativeDateCell
              key={name}
              date={physicalRelease}
              timeForToday={false}
            />
          );
        }

        if (name === 'games.lastSearchTime') {
          return (
            <RelativeDateCell
              key={name}
              date={lastSearchTime}
              includeSeconds={true}
            />
          );
        }

        if (name === 'status') {
          return (
            <TableRowCell key={name} className={styles.status}>
              <GameStatus
                gameId={id}
                gameFileId={gameFileId}
                gameEntity="wanted.missing"
              />
            </TableRowCell>
          );
        }

        if (name === 'actions') {
          return (
            <GameSearchCell
              key={name}
              gameId={id}
              gameEntity="wanted.missing"
            />
          );
        }

        return null;
      })}
    </TableRow>
  );
}

export default MissingRow;
