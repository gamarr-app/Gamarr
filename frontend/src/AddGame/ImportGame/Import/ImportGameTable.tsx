import _ from 'lodash';
import { Component, CSSProperties } from 'react';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import Game from 'Game/Game';
import { SelectStateInputProps } from 'typings/props';
import ImportGameHeader from './ImportGameHeader';
import ImportGameRowConnector from './ImportGameRowConnector';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface SelectedGame {
  igdbId: number;
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
  scroller: Element;
  onSelectAllChange: (payload: { value: boolean }) => void;
  onSelectedChange: (payload: SelectStateInputProps) => void;
  onRemoveSelectedStateItem: (id: string) => void;
  onGameLookup: (id: string, path: string, relativePath: string) => void;
  onSetImportGameValue: (values: {
    id: string;
    [key: string]: unknown;
  }) => void;
}

interface RowRendererParams {
  key: string;
  rowIndex: number;
  style: CSSProperties;
}

class ImportGameTable extends Component<ImportGameTableProps> {
  //
  // Lifecycle

  componentDidMount() {
    const {
      unmappedFolders,
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
      onGameLookup,
      onSetImportGameValue,
    } = this.props;

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
  }

  // This isn't great, but it's the most reliable way to ensure the items
  // are checked off even if they aren't actually visible since the cells
  // are virtualized.

  componentDidUpdate(prevProps: ImportGameTableProps) {
    const {
      items,
      selectedState,
      onSelectedChange,
      onRemoveSelectedStateItem,
    } = this.props;

    prevProps.items.forEach((prevItem) => {
      const { id } = prevItem;

      const item = _.find(items, { id });

      if (!item) {
        onRemoveSelectedStateItem(id);
        return;
      }

      const selectedGame = item.selectedGame;
      const isSelected = selectedState[id];

      const isExistingGame =
        !!selectedGame &&
        _.some(prevProps.allGames, { igdbId: selectedGame.igdbId });

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
  }

  //
  // Control

  rowRenderer = ({ key, rowIndex, style }: RowRendererParams) => {
    const { rootFolderId, items, selectedState, onSelectedChange } = this.props;

    const item = items[rowIndex];

    return (
      <VirtualTableRow key={key} style={style} className="">
        <ImportGameRowConnector
          key={item.id}
          rootFolderId={rootFolderId}
          isSelected={selectedState[item.id]}
          id={item.id}
          onSelectedChange={onSelectedChange}
        />
      </VirtualTableRow>
    );
  };

  //
  // Render

  render() {
    const {
      items,
      allSelected,
      allUnselected,
      isSmallScreen,
      scroller,
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      selectedState: _selectedState,
      onSelectAllChange,
    } = this.props;

    if (!items.length) {
      return null;
    }

    return (
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      <VirtualTable<any>
        items={items}
        isSmallScreen={isSmallScreen}
        scroller={scroller}
        rowHeight={52}
        rowRenderer={this.rowRenderer}
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
}

export default ImportGameTable;
