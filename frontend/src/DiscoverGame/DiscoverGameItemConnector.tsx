import React from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';

interface DiscoverGame {
  igdbId: number;
  title: string;
  sortTitle: string;
  year?: number;
  studio?: string;
  status: string;
  overview?: string;
  images?: Array<{ coverType: string; url: string }>;
  genres: string[];
  ratings?: {
    igdb?: { value: number };
    metacritic?: { value: number };
  };
  certification?: string;
  inCinemas?: string;
  physicalRelease?: string;
  digitalRelease?: string;
  runtime?: number;
  isExcluded: boolean;
  isExisting: boolean;
  lists: unknown[];
}

interface DiscoverGameState {
  items: DiscoverGame[];
}

interface RootState {
  discoverGame: DiscoverGameState;
}

export interface DiscoverGameProps {
  igdbId: number;
  isSelected?: boolean;
  onSelectedChange?: (options: {
    id: number | string;
    value: boolean | null;
    shiftKey: boolean;
  }) => void;
  [key: string]: unknown;
}

interface OwnProps {
  igdbId: number;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  component: React.ComponentType<any>;
  isSelected?: boolean;
  onSelectedChange?: (options: {
    id: number | string;
    value: boolean | null;
    shiftKey: boolean;
  }) => void;
  [key: string]: unknown;
}

function createMapStateToProps() {
  return createSelector(
    (_state: RootState, { igdbId }: OwnProps) => igdbId,
    (state: RootState) => state.discoverGame,
    (igdbId, discoverGame) => {
      const game = discoverGame.items.find((g) => g.igdbId === igdbId);

      // If a game is deleted this selector may fire before the parent
      // selectors, which will result in an undefined game, if that happens
      // we want to return early here and again in the render function to avoid
      // trying to show a game that has no information available.

      if (!game) {
        return { igdbId: undefined };
      }

      return {
        ...game,
      };
    }
  );
}

interface StateProps {
  igdbId?: number;
}

type DiscoverGameItemConnectorProps = OwnProps & StateProps;

function DiscoverGameItemConnector({
  igdbId,
  component: ItemComponent,
  ...otherProps
}: DiscoverGameItemConnectorProps) {
  if (!igdbId) {
    return null;
  }

  return <ItemComponent {...otherProps} igdbId={igdbId} />;
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(createMapStateToProps)(DiscoverGameItemConnector as any);
