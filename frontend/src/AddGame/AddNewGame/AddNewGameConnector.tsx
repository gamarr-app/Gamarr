import { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps, withRouter } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { clearAddGame, lookupGame } from 'Store/Actions/addGameActions';
import { clearGameFiles, fetchGameFiles } from 'Store/Actions/gameFileActions';
import {
  clearQueueDetails,
  fetchQueueDetails,
} from 'Store/Actions/queueActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import hasDifferentItems from 'Utilities/Object/hasDifferentItems';
import parseUrl from 'Utilities/String/parseUrl';
import AddNewGame, { AddNewGameItem } from './AddNewGame';

function createMapStateToProps() {
  return createSelector(
    (state: AppState) => state.addGame,
    (state: AppState) => state.games.items.length,
    (_state: AppState, ownProps: RouteComponentProps) => ownProps.location,
    (addGame, existingGamesCount, location) => {
      const { params } = parseUrl(location.search);

      return {
        ...addGame,
        term: params.term as string | undefined,
        hasExistingGames: existingGamesCount > 0,
      };
    }
  );
}

const mapDispatchToProps = {
  lookupGame,
  clearAddGame,
  fetchRootFolders,
  fetchQueueDetails,
  clearQueueDetails,
  fetchGameFiles,
  clearGameFiles,
};

interface AddNewGameConnectorProps extends RouteComponentProps {
  term?: string;
  isFetching: boolean;
  isAdding: boolean;
  hasExistingGames: boolean;
  items: AddNewGameItem[];
  lookupGame: (payload: { term: string }) => void;
  clearAddGame: () => void;
  fetchRootFolders: () => void;
  fetchQueueDetails: () => void;
  clearQueueDetails: () => void;
  fetchGameFiles: (payload: { gameId: number[] }) => void;
  clearGameFiles: () => void;
}

class AddNewGameConnector extends Component<AddNewGameConnectorProps> {
  // eslint-disable-next-line react/sort-comp
  private _gameLookupTimeout: ReturnType<typeof setTimeout> | null = null;

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchRootFolders();
    this.props.fetchQueueDetails();
  }

  componentDidUpdate(prevProps: AddNewGameConnectorProps) {
    const { items } = this.props;

    if (hasDifferentItems(prevProps.items, items)) {
      const gameIds = items
        .filter((item) => item.internalId != null)
        .map((item) => item.internalId as number)
        .filter((id, index, self) => self.indexOf(id) === index);

      if (gameIds.length) {
        this.props.fetchGameFiles({ gameId: gameIds });
      }
    }
  }

  componentWillUnmount() {
    if (this._gameLookupTimeout) {
      clearTimeout(this._gameLookupTimeout);
    }

    this.props.clearAddGame();
    this.props.clearQueueDetails();
    this.props.clearGameFiles();
  }

  //
  // Listeners

  onGameLookupChange = (term: string) => {
    if (this._gameLookupTimeout) {
      clearTimeout(this._gameLookupTimeout);
    }

    if (term.trim() === '') {
      this.props.clearAddGame();
    } else {
      this._gameLookupTimeout = setTimeout(() => {
        this.props.lookupGame({ term });
      }, 300);
    }
  };

  onClearGameLookup = () => {
    this.props.clearAddGame();
  };

  //
  // Render

  render() {
    const { term, ...otherProps } = this.props;

    return (
      <AddNewGame
        term={term}
        {...otherProps}
        onGameLookupChange={this.onGameLookupChange}
        onClearGameLookup={this.onClearGameLookup}
      />
    );
  }
}

export default withRouter(
  connect(createMapStateToProps, mapDispatchToProps)(AddNewGameConnector)
);
