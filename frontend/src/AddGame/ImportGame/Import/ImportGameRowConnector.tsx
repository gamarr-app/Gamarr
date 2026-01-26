import _ from 'lodash';
import { Component } from 'react';
import { connect, ConnectedProps } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ImportGameItem } from 'App/State/ImportGameAppState';
import {
  queueLookupGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import { InputChanged } from 'typings/inputs';
import { SelectStateInputProps } from 'typings/props';
import ImportGameRow from './ImportGameRow';

// Own props - passed by parent component
interface OwnProps {
  rootFolderId: number;
  id: string;
  isSelected?: boolean;
  onSelectedChange: (payload: SelectStateInputProps) => void;
}

// State props - derived from Redux state, excluding id which comes from OwnProps
type StateProps = Omit<Partial<ImportGameItem>, 'id'> & {
  isExistingGame: boolean;
};

function createImportGameItemSelector() {
  return createSelector(
    (_state: AppState, { id }: OwnProps) => id,
    (state: AppState) => state.importGame.items,
    (id, items): Omit<Partial<ImportGameItem>, 'id'> => {
      const found = _.find(items, { id });
      if (!found) return {};
      // eslint-disable-next-line @typescript-eslint/no-unused-vars
      const { id: _id, ...rest } = found;
      return rest;
    }
  );
}

function createMapStateToProps() {
  return createSelector(
    createImportGameItemSelector(),
    createAllGamesSelector(),
    (item, games): StateProps => {
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

const connector = connect(createMapStateToProps, mapDispatchToProps);

type PropsFromRedux = ConnectedProps<typeof connector>;

type ImportGameRowConnectorProps = OwnProps & PropsFromRedux;

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

export default connector(ImportGameRowConnector);
