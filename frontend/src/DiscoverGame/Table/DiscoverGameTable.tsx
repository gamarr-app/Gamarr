import React, { ComponentType, useCallback, useEffect, useState } from 'react';
import ModelBase from 'App/ModelBase';
import Column from 'Components/Table/Column';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import DiscoverGameItemConnector from 'DiscoverGame/DiscoverGameItemConnector';
import { SelectedState } from 'Helpers/Hooks/useSelectState';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { CheckInputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import getIndexOfFirstCharacter from 'Utilities/Array/getIndexOfFirstCharacter';
import DiscoverGameHeaderConnector from './DiscoverGameHeaderConnector';
import DiscoverGameRowConnector from './DiscoverGameRowConnector';
import styles from './DiscoverGameTable.css';

// Type for DiscoverGameItemConnector props
interface DiscoverGameItemProps {
  igdbId: number;
  component: ComponentType<Record<string, unknown>>;
  columns?: Column[];
  isSelected?: boolean;
  onSelectedChange?: (options: SelectStateInputProps) => void;
}

// Cast the connected component to the expected type
const TypedDiscoverGameItemConnector =
  DiscoverGameItemConnector as unknown as ComponentType<DiscoverGameItemProps>;

interface DiscoverGameItem extends ModelBase {
  igdbId: number;
  sortTitle: string;
}

interface RowRendererProps {
  key: string;
  rowIndex: number;
  style: React.CSSProperties;
}

interface DiscoverGameTableProps {
  items: DiscoverGameItem[];
  columns: Column[];
  sortKey?: string;
  sortDirection?: SortDirection;
  jumpToCharacter?: string | null;
  isSmallScreen: boolean;
  scroller: Element;
  onSortPress: (sortKey: string) => void;
  allSelected: boolean;
  allUnselected: boolean;
  selectedState: SelectedState;
  onSelectedChange: (options: SelectStateInputProps) => void;
  onSelectAllChange: (change: CheckInputChanged) => void;
}

function DiscoverGameTable({
  items,
  columns,
  sortKey,
  sortDirection,
  jumpToCharacter,
  isSmallScreen,
  scroller,
  onSortPress,
  allSelected,
  allUnselected,
  onSelectAllChange,
  selectedState,
  onSelectedChange,
}: DiscoverGameTableProps) {
  const [scrollIndex, setScrollIndex] = useState<number | null>(null);

  useEffect(() => {
    if (jumpToCharacter != null) {
      const index = getIndexOfFirstCharacter(items, jumpToCharacter);

      if (index != null) {
        setScrollIndex(index);
      }
    } else {
      setScrollIndex(null);
    }
  }, [items, jumpToCharacter]);

  const rowRenderer = useCallback(
    ({ key, rowIndex, style }: RowRendererProps) => {
      const game = items[rowIndex];

      return (
        <VirtualTableRow
          key={key}
          style={style}
          className={styles.tableContainer}
        >
          <TypedDiscoverGameItemConnector
            key={game.igdbId}
            // eslint-disable-next-line @typescript-eslint/no-explicit-any
            component={DiscoverGameRowConnector as any}
            columns={columns}
            igdbId={game.igdbId}
            isSelected={selectedState[game.igdbId]}
            onSelectedChange={onSelectedChange}
          />
        </VirtualTableRow>
      );
    },
    [items, columns, selectedState, onSelectedChange]
  );

  return (
    <VirtualTable
      className={styles.tableContainer}
      items={items}
      scrollIndex={scrollIndex ?? undefined}
      isSmallScreen={isSmallScreen}
      scroller={scroller}
      rowHeight={38}
      rowRenderer={rowRenderer}
      header={
        <DiscoverGameHeaderConnector
          columns={columns}
          sortKey={sortKey}
          sortDirection={sortDirection}
          allSelected={allSelected}
          allUnselected={allUnselected}
          onSortPress={onSortPress}
          onSelectAllChange={onSelectAllChange}
        />
      }
    />
  );
}

export default DiscoverGameTable;
