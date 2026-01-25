import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { clearAddGame, lookupGame } from 'Store/Actions/addGameActions';
import { clearGameFiles, fetchGameFiles } from 'Store/Actions/gameFileActions';
import {
  clearQueueDetails,
  fetchQueueDetails,
} from 'Store/Actions/queueActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import hasDifferentItems from 'Utilities/Object/hasDifferentItems';
import selectUniqueIds from 'Utilities/Object/selectUniqueIds';
import parseUrl from 'Utilities/String/parseUrl';
import AddNewGame from './AddNewGame';

interface AddNewGameItem {
  titleSlug: string;
  igdbId: number;
  internalId?: number;
  id?: number;
  [key: string]: unknown;
}

function createMapStateToProps() {
  return createSelector(
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.addGame,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.games.items.length,
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (state: any) => state.router.location,
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

interface AddNewGameConnectorProps {
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
  _gameLookupTimeout: ReturnType<typeof setTimeout> | null = null;

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchRootFolders();
    this.props.fetchQueueDetails();
  }

  componentDidUpdate(prevProps: AddNewGameConnectorProps) {
    const { items } = this.props;

    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    if (hasDifferentItems(prevProps.items as any, items as any)) {
      // eslint-disable-next-line @typescript-eslint/no-explicit-any
      const gameIds = selectUniqueIds(items as any, 'internalId') as number[];

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

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(AddNewGameConnector);
