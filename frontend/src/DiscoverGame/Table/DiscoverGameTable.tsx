import {
  ComponentType,
  CSSProperties,
  useCallback,
  useEffect,
  useState,
} from 'react';
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

// DiscoverGameRowConnector is a connected component that passes additional props to DiscoverGameRow.
// The DiscoverGameItemConnector's component prop accepts ComponentType<any>, so we need only
// a simple cast to satisfy TypeScript's structural typing requirements.
const TypedDiscoverGameRowConnector =
  // eslint-disable-next-line @typescript-eslint/no-explicit-any -- Receives merged props from Redux
  DiscoverGameRowConnector as ComponentType<any>;

interface DiscoverGameItem extends ModelBase {
  igdbId: number;
  sortTitle: string;
}

interface RowRendererProps {
  key: string;
  rowIndex: number;
  style: CSSProperties;
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
    if (jumpToCharacter == null) {
      setScrollIndex(null);
      return;
    }

    const index = getIndexOfFirstCharacter(items, jumpToCharacter);

    if (index != null) {
      setScrollIndex(index);
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
