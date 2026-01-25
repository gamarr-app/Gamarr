import _ from 'lodash';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { InputChanged } from 'typings/inputs';
import ImportGameRow from './ImportGameRow';

interface SelectedGame {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
  [key: string]: unknown;
}

interface ImportGameItem {
  id: string;
  selectedGame?: SelectedGame;
  monitor?: string;
  items?: object[];
  [key: string]: unknown;
}

function createImportGameItemSelector() {
  return createSelector(
    (_state: AppState, { id }: { id: string }) => id,
    (state: AppState) => state.importGame.items,
    (id, items) => {
      return (_.find(items, { id }) as unknown as ImportGameItem) || {};
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createImportGameItemSelector(),
    createAllGamesSelector(),
    (item, games) => {
      const selectedGame = item && item.selectedGame;
      const isExistingGame =
        !!selectedGame && _.some(games, { igdbId: selectedGame.igdbId });

      return {
        ...item,
        isExistingGame,
      };
    }
  );
}

const mapDispatchToProps = {
  queueLookupGame,
  setImportGameValue,
};

interface ImportGameRowConnectorProps {
  rootFolderId: number;
  id: string;
  monitor?: string;
  qualityProfileId?: number;
  minimumAvailability?: string;
  relativePath?: string;
  selectedGame?: SelectedGame;
  isExistingGame?: boolean;
  items?: object[];
  isSelected?: boolean;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  onSelectedChange: (payload: any) => void;
  queueLookupGame: (payload: {
    name: string;
    term: string;
    topOfQueue?: boolean;
  }) => void;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  setImportGameValue: (values: any) => void;
}

class ImportGameRowConnector extends Component<ImportGameRowConnectorProps> {
  //
  // Listeners

  onInputChange = ({ name, value }: InputChanged) => {
    this.props.setImportGameValue({
      id: this.props.id,
      [name]: value,
    });
  };

  //
  // Render

  render() {
    // Don't show the row until we have the information we require for it.

    const { items, monitor } = this.props;

    if (!items || !monitor) {
      return null;
    }

    const {
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      queueLookupGame,
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      setImportGameValue,
      ...rowProps
    } = this.props;

    return (
      <ImportGameRow
        {...rowProps}
        items={rowProps.items || []}
        monitor={rowProps.monitor || ''}
        qualityProfileId={rowProps.qualityProfileId || 0}
        minimumAvailability={rowProps.minimumAvailability || ''}
        relativePath={rowProps.relativePath || ''}
        isExistingGame={rowProps.isExistingGame || false}
        onInputChange={this.onInputChange}
      />
    );
  }
}

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameRowConnector);
