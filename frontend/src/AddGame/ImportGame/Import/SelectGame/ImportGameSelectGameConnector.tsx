import _ from 'lodash';
import { Component } from 'react';
import { connect, ConnectedProps } from 'react-redux';
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

const connector = connect(createMapStateToProps, mapDispatchToProps);

interface OwnProps {
  id: string;
  isExistingGame: boolean;
}

interface StateFromSelector {
  isLookingUpGame: boolean;
  items?: GameItem[];
  selectedGame?: SelectedGame;
  isSelected?: boolean;
}

type PropsFromRedux = ConnectedProps<typeof connector>;

type ImportGameSelectGameConnectorProps = OwnProps &
  StateFromSelector &
  PropsFromRedux;

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

export default connector(
  ImportGameSelectGameConnector
) as unknown as React.ComponentType<OwnProps>;
