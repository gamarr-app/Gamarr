import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import VirtualTable from 'Components/Table/VirtualTable';
import VirtualTableRow from 'Components/Table/VirtualTableRow';
import ImportGameHeader from './ImportGameHeader';
import ImportGameRowConnector from './ImportGameRowConnector';

class ImportGameTable extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      unmappedFolders,
      defaultMonitor,
      defaultQualityProfileId,
      defaultMinimumAvailability,
      onGameLookup,
      onSetImportGameValue
    } = this.props;

    const values = {
      monitor: defaultMonitor,
      qualityProfileId: defaultQualityProfileId,
      minimumAvailability: defaultMinimumAvailability
    };

    unmappedFolders.forEach((unmappedFolder) => {
      const id = unmappedFolder.name;

      onGameLookup(id, unmappedFolder.path, unmappedFolder.relativePath);

      onSetImportGameValue({
        id,
        ...values
      });
    });
  }

  // This isn't great, but it's the most reliable way to ensure the items
  // are checked off even if they aren't actually visible since the cells
  // are virtualized.

  componentDidUpdate(prevProps) {
    const {
      items,
      selectedState,
      onSelectedChange,
      onRemoveSelectedStateItem
    } = this.props;

    prevProps.items.forEach((prevItem) => {
      const {
        id
      } = prevItem;

      const item = _.find(items, { id });

      if (!item) {
        onRemoveSelectedStateItem(id);
        return;
      }

      const selectedGame = item.selectedGame;
      const isSelected = selectedState[id];

      const isExistingGame = !!selectedGame &&
        _.some(prevProps.allGames, { igdbId: selectedGame.igdbId });

      // Props doesn't have a selected game or
      // the selected game is an existing game.
      if ((!selectedGame && prevItem.selectedGame) || (isExistingGame && !prevItem.selectedGame)) {
        onSelectedChange({ id, value: false });

        return;
      }

      // State is selected, but a game isn't selected or
      // the selected game is an existing game.
      if (isSelected && (!selectedGame || isExistingGame)) {
        onSelectedChange({ id, value: false });

        return;
      }

      // A game is being selected that wasn't previously selected.
      if (selectedGame && selectedGame !== prevItem.selectedGame) {
        onSelectedChange({ id, value: true });

        return;
      }
    });
  }

  //
  // Control

  rowRenderer = ({ key, rowIndex, style }) => {
    const {
      rootFolderId,
      items,
      selectedState,
      onSelectedChange
    } = this.props;

    const item = items[rowIndex];

    return (
      <VirtualTableRow
        key={key}
        style={style}
      >
        <ImportGameRowConnector
          key={item.id}
          rootFolderId={rootFolderId}
          isSelected={selectedState[item.id]}
          onSelectedChange={onSelectedChange}
          id={item.id}
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
      selectedState,
      onSelectAllChange
    } = this.props;

    if (!items.length) {
      return null;
    }

    return (
      <VirtualTable
        items={items}
        isSmallScreen={isSmallScreen}
        scroller={scroller}
        rowHeight={52}
        overscanRowCount={2}
        rowRenderer={this.rowRenderer}
        header={
          <ImportGameHeader
            allSelected={allSelected}
            allUnselected={allUnselected}
            onSelectAllChange={onSelectAllChange}
          />
        }
        selectedState={selectedState}
      />
    );
  }
}

ImportGameTable.propTypes = {
  rootFolderId: PropTypes.number.isRequired,
  items: PropTypes.arrayOf(PropTypes.object),
  unmappedFolders: PropTypes.arrayOf(PropTypes.object),
  defaultMonitor: PropTypes.string.isRequired,
  defaultQualityProfileId: PropTypes.number,
  defaultMinimumAvailability: PropTypes.string,
  allSelected: PropTypes.bool.isRequired,
  allUnselected: PropTypes.bool.isRequired,
  selectedState: PropTypes.object.isRequired,
  isSmallScreen: PropTypes.bool.isRequired,
  allGames: PropTypes.arrayOf(PropTypes.object),
  scroller: PropTypes.instanceOf(Element).isRequired,
  onSelectAllChange: PropTypes.func.isRequired,
  onSelectedChange: PropTypes.func.isRequired,
  onRemoveSelectedStateItem: PropTypes.func.isRequired,
  onGameLookup: PropTypes.func.isRequired,
  onSetImportGameValue: PropTypes.func.isRequired
};

export default ImportGameTable;
