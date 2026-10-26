import { RefObject, useEffect, useMemo, useState } from 'react';
import { RowComponentProps } from 'react-window';
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

// DiscoverGameRowConnector is a connected component that receives props from both
// the parent and Redux via DiscoverGameItemConnector.
const TypedDiscoverGameRowConnector = DiscoverGameRowConnector;

interface DiscoverGameItem extends ModelBase {
  igdbId: number;
  sortTitle: string;
}

interface RowItemData {
  items: DiscoverGameItem[];
  columns: Column[];
  selectedState: SelectedState;
  onSelectedChange: (options: SelectStateInputProps) => void;
}

function Row({
  index,
  style,
  items,
  columns,
  selectedState,
  onSelectedChange,
}: RowComponentProps<RowItemData>) {
  const game = items[index];

  if (game == null) {
    return null;
  }

  return (
    <VirtualTableRow style={style} className={styles.tableContainer}>
      <DiscoverGameItemConnector
        key={game.igdbId}
        component={TypedDiscoverGameRowConnector}
        columns={columns}
        igdbId={game.igdbId}
        isSelected={selectedState[game.igdbId]}
        onSelectedChange={onSelectedChange}
      />
    </VirtualTableRow>
  );
}

interface DiscoverGameTableProps {
  items: DiscoverGameItem[];
  columns: Column[];
  sortKey?: string;
  sortDirection?: SortDirection;
  jumpToCharacter?: string | null;
  isSmallScreen: boolean;
  scrollerRef: RefObject<HTMLElement | null>;
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
  scrollerRef,
  onSortPress,
  allSelected,
  allUnselected,
  onSelectAllChange,
  selectedState,
  onSelectedChange,
}: DiscoverGameTableProps) {
  const [scrollIndex, setScrollIndex] = useState<number | null>(null);

  useEffect(() => {
    if (jumpToCharacter == null) {
      setScrollIndex(null);
      return;
    }

    const index = getIndexOfFirstCharacter(items, jumpToCharacter);

    if (index !== -1) {
      setScrollIndex(index);
    }
  }, [items, jumpToCharacter]);

  const rowProps = useMemo(
    () => ({ items, columns, selectedState, onSelectedChange }),
    [items, columns, selectedState, onSelectedChange]
  );

  return (
    <VirtualTable<RowItemData>
      className={styles.tableContainer}
      items={items}
      scrollIndex={scrollIndex ?? undefined}
      isSmallScreen={isSmallScreen}
      scrollerRef={scrollerRef}
      rowHeight={38}
      rowProps={rowProps}
      rowComponent={Row}
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
