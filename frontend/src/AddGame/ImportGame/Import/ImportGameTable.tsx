import _ from 'lodash';
import { RefObject, useEffect, useMemo, useRef } from 'react';
import { RowComponentProps } from 'react-window';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import Game from 'Game/Game';
import { SelectStateInputProps } from 'typings/props';
import checkIsExistingGame from 'Utilities/Game/isExistingGame';
import ImportGameHeader from './ImportGameHeader';
import ImportGameRowConnector from './ImportGameRowConnector';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface SelectedGame {
  igdbId: number;
  steamAppId?: number;
}

interface ImportGameItem {
  id: string;
  selectedGame?: SelectedGame;
}

interface SelectedState {
  [key: string]: boolean;
}

interface ImportGameTableProps {
  rootFolderId: number;
  items: ImportGameItem[];
  unmappedFolders: UnmappedFolder[];
  defaultMonitor: string;
  defaultQualityProfileId?: number;
  defaultMinimumAvailability?: string;
  allSelected: boolean;
  allUnselected: boolean;
  selectedState: SelectedState;
  isSmallScreen: boolean;
  allGames: Game[];
  scrollerRef: RefObject<HTMLElement | null>;
  onSelectAllChange: (payload: { value: boolean }) => void;
  onSelectedChange: (payload: SelectStateInputProps) => void;
  onRemoveSelectedStateItem: (id: string) => void;
  onGameLookup: (id: string, path: string, relativePath: string) => void;
  onSetImportGameValue: (values: {
    id: string;
    [key: string]: unknown;
  }) => void;
}

interface RowItemData {
  items: ImportGameItem[];
  rootFolderId: number;
  selectedState: SelectedState;
  onSelectedChange: (payload: SelectStateInputProps) => void;
}

function Row({
  index,
  style,
  items,
  rootFolderId,
  selectedState,
  onSelectedChange,
}: RowComponentProps<RowItemData>) {
  const item = items[index];

  if (!item) {
    return null;
  }

  return (
    <VirtualTableRow style={style}>
      <ImportGameRowConnector
        key={item.id}
        rootFolderId={rootFolderId}
        isSelected={selectedState[item.id]}
        id={item.id}
        onSelectedChange={onSelectedChange}
      />
    </VirtualTableRow>
  );
}

function ImportGameTable(props: ImportGameTableProps) {
  const {
    rootFolderId,
    items,
    unmappedFolders,
    defaultMonitor,
    defaultQualityProfileId,
    defaultMinimumAvailability,
    allSelected,
    allUnselected,
    selectedState,
    isSmallScreen,
    allGames,
    scrollerRef,
    onSelectAllChange,
    onSelectedChange,
    onRemoveSelectedStateItem,
    onGameLookup,
    onSetImportGameValue,
  } = props;

  const prevItemsRef = useRef<ImportGameItem[]>(items);
  const prevAllGamesRef = useRef<Game[]>(allGames);

  // Mount effect - lookup games and set default values
  useEffect(() => {
    const values = {
      monitor: defaultMonitor,
      qualityProfileId: defaultQualityProfileId,
      minimumAvailability: defaultMinimumAvailability,
    };

    unmappedFolders.forEach((unmappedFolder) => {
      const id = unmappedFolder.name;

      onGameLookup(id, unmappedFolder.path, unmappedFolder.relativePath);

      onSetImportGameValue({
        id,
        ...values,
      });
    });
    // Only run on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Update effect - handle selection state changes
  useEffect(() => {
    const prevItems = prevItemsRef.current;

    prevItems.forEach((prevItem) => {
      const { id } = prevItem;

      const item = _.find(items, { id });

      if (!item) {
        onRemoveSelectedStateItem(id);
        return;
      }

      const selectedGame = item.selectedGame;
      const isSelected = selectedState[id];

      const isExistingGame = checkIsExistingGame(
        prevAllGamesRef.current,
        selectedGame
      );

      // Props doesn't have a selected game or
      // the selected game is an existing game.
      if (
        (!selectedGame && prevItem.selectedGame) ||
        (isExistingGame && !prevItem.selectedGame)
      ) {
        onSelectedChange({ id, value: false, shiftKey: false });

        return;
      }

      // State is selected, but a game isn't selected or
      // the selected game is an existing game.
      if (isSelected && (!selectedGame || isExistingGame)) {
        onSelectedChange({ id, value: false, shiftKey: false });

        return;
      }

      // A game is being selected that wasn't previously selected.
      if (selectedGame && selectedGame !== prevItem.selectedGame) {
        onSelectedChange({ id, value: true, shiftKey: false });

        return;
      }
    });

    prevItemsRef.current = items;
    prevAllGamesRef.current = allGames;
  }, [
    items,
    allGames,
    selectedState,
    onSelectedChange,
    onRemoveSelectedStateItem,
  ]);

  const rowProps = useMemo(
    () => ({ items, rootFolderId, selectedState, onSelectedChange }),
    [items, rootFolderId, selectedState, onSelectedChange]
  );

  if (!items.length) {
    return null;
  }

  return (
    <VirtualTable<RowItemData>
      items={items}
      isSmallScreen={isSmallScreen}
      scrollerRef={scrollerRef}
      rowHeight={52}
      rowProps={rowProps}
      rowComponent={Row}
      header={
        <ImportGameHeader
          allSelected={allSelected}
          allUnselected={allUnselected}
          onSelectAllChange={onSelectAllChange}
        />
      }
    />
  );
}

export default ImportGameTable;
