import _ from 'lodash';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  ImportGameItem,
  ImportGameSelectedGame,
} from 'App/State/ImportGameAppState';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { InputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import ImportGameRow from './ImportGameRow';

function createImportGameItemSelector() {
  return createSelector(
    (_state: AppState, { id }: { id: string }) => id,
    (state: AppState) => state.importGame.items,
    (id, items): Partial<ImportGameItem> => {
      return _.find(items, { id }) || {};
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

      // Exclude id from the spread - id always comes from ownProps, not state
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { id: _id, ...itemWithoutId } = item;

      return {
        ...itemWithoutId,
        isExistingGame,
      };
    }
  );
}

const mapDispatchToProps = {
  queueLookupGame,
  setImportGameValue,
};

interface SetImportGameValuePayload {
  id: string;
  [key: string]: unknown;
}

interface ImportGameRowConnectorProps {
  rootFolderId: number;
  id: string;
  monitor?: string;
  qualityProfileId?: number;
  minimumAvailability?: string;
  relativePath?: string;
  selectedGame?: ImportGameSelectedGame;
  isExistingGame?: boolean;
  items?: ImportGameSelectedGame[];
  isSelected?: boolean;
  onSelectedChange: (payload: SelectStateInputProps) => void;
  queueLookupGame: (payload: {
    name: string;
    term: string;
    topOfQueue?: boolean;
  }) => void;
  setImportGameValue: (values: SetImportGameValuePayload) => void;
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

// The id is always passed from the parent component (ImportGameTable), not from state
export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameRowConnector) as React.ComponentType<
  Omit<ImportGameRowConnectorProps, 'queueLookupGame' | 'setImportGameValue'>
>;
