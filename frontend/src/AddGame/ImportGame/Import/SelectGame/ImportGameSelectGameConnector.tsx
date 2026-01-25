import _ from 'lodash';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createImportGameItemSelector from 'Store/Selectors/createImportGameItemSelector';
import ImportGameSelectGame from './ImportGameSelectGame';

interface SelectedGame {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
  [key: string]: unknown;
}

interface GameItem {
  igdbId: number;
  title: string;
  year: number;
  studio?: string;
}

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.importGame.isLookingUpGame,
    createImportGameItemSelector(),
    (isLookingUpGame, item) => {
      return {
        isLookingUpGame,
        ...item,
      };
    }
  );
}

const mapDispatchToProps = {
  queueLookupGame,
  setImportGameValue,
};

interface ImportGameSelectGameConnectorProps {
  id: string;
  items?: GameItem[];
  selectedGame?: SelectedGame;
  isSelected?: boolean;
  isExistingGame: boolean;
  isLookingUpGame?: boolean;
  queueLookupGame: (payload: {
    name: string;
    term: string;
    topOfQueue?: boolean;
  }) => void;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  setImportGameValue: (values: any) => void;
}

class ImportGameSelectGameConnector extends Component<ImportGameSelectGameConnectorProps> {
  //
  // Listeners

  onSearchInputChange = (term: string) => {
    this.props.queueLookupGame({
      name: this.props.id,
      term,
      topOfQueue: true,
    });
  };

  onGameSelect = (igdbId: number) => {
    const { id, items } = this.props;

    this.props.setImportGameValue({
      id,
      selectedGame: _.find(items, { igdbId }),
    });
  };

  //
  // Render

  render() {
    return (
      <ImportGameSelectGame
        {...this.props}
        isLookingUpGame={this.props.isLookingUpGame || false}
        onSearchInputChange={this.onSearchInputChange}
        onGameSelect={this.onGameSelect}
      />
    );
  }
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameSelectGameConnector as any);
