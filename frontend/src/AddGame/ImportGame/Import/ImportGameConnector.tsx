import _ from 'lodash';
import { Component } from 'react';
import { connect } from 'react-redux';
import { RouteComponentProps } from 'react-router-dom';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { ImportGameItem } from 'App/State/ImportGameAppState';
import { setAddGameDefault } from 'Store/Actions/addGameActions';
import {
  clearImportGame,
  importGame,
  setImportGameValue,
} from 'Store/Actions/importGameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import QualityProfile from 'typings/QualityProfile';
import ImportGame from './ImportGame';

interface MatchParams {
  rootFolderId: string;
}

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface RootFolderItem {
  id: number;
  path: string;
  unmappedFolders?: UnmappedFolder[];
  [key: string]: unknown;
}

function createMapStateToProps() {
  return createSelector(
    (_state: AppState, { match }: RouteComponentProps<MatchParams>) => match,
    (state: AppState) => state.rootFolders,
    (state: AppState) => state.addGame,
    (state: AppState) => state.importGame,
    (state: AppState) => state.settings.qualityProfiles,
    (match, rootFolders, addGame, importGameState, qualityProfiles) => {
      const {
        isFetching: rootFoldersFetching,
        isPopulated: rootFoldersPopulated,
        error: rootFoldersError,
        items,
      } = rootFolders;

      const rootFolderId = parseInt(match.params.rootFolderId);

      const result = {
        rootFolderId,
        rootFoldersFetching,
        rootFoldersPopulated,
        rootFoldersError,
        qualityProfiles: qualityProfiles.items,
        defaultQualityProfileId: addGame.defaults.qualityProfileId,
      };

      if (items.length) {
        const rootFolder = _.find(items, { id: rootFolderId }) as
          | RootFolderItem
          | undefined;

        return {
          ...result,
          ...rootFolder,
          items: importGameState.items,
        };
      }

      return {
        ...result,
        items: [] as ImportGameItem[],
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetImportGameValue: setImportGameValue,
  dispatchImportGame: importGame,
  dispatchClearImportGame: clearImportGame,
  dispatchFetchRootFolders: fetchRootFolders,
  dispatchSetAddGameDefault: setAddGameDefault,
};

interface SetImportGameValuePayload {
  id: string;
  [key: string]: unknown;
}

interface ImportGamePayload {
  ids: string[];
}

interface FetchRootFoldersPayload {
  id?: number;
  timeout?: boolean;
}

interface ImportGameConnectorProps extends RouteComponentProps<MatchParams> {
  rootFolderId: number;
  rootFoldersFetching: boolean;
  rootFoldersPopulated: boolean;
  qualityProfiles: QualityProfile[];
  defaultQualityProfileId: number;
  items: ImportGameItem[];
  dispatchSetImportGameValue: (values: SetImportGameValuePayload) => void;
  dispatchImportGame: (payload: ImportGamePayload) => void;
  dispatchClearImportGame: () => void;
  dispatchFetchRootFolders: (payload?: FetchRootFoldersPayload) => void;
  dispatchSetAddGameDefault: (defaults: Record<string, unknown>) => void;
}

class ImportGameConnector extends Component<ImportGameConnectorProps> {
  //
  // Lifecycle

  componentDidMount() {
    const {
      rootFolderId,
      qualityProfiles,
      defaultQualityProfileId,
      dispatchFetchRootFolders,
      dispatchSetAddGameDefault,
    } = this.props;

    dispatchFetchRootFolders({ id: rootFolderId, timeout: false });

    let setDefaults = false;
    const setDefaultPayload: Record<string, unknown> = {};

    if (
      !defaultQualityProfileId ||
      !qualityProfiles.some((p) => p.id === defaultQualityProfileId)
    ) {
      setDefaults = true;
      setDefaultPayload.qualityProfileId = qualityProfiles[0].id;
    }

    if (setDefaults) {
      dispatchSetAddGameDefault(setDefaultPayload);
    }
  }

  componentWillUnmount() {
    this.props.dispatchClearImportGame();
  }

  //
  // Listeners

  onInputChange = (ids: string[], name: string, value: unknown) => {
    this.props.dispatchSetAddGameDefault({ [name]: value });

    ids.forEach((id) => {
      this.props.dispatchSetImportGameValue({
        id,
        [name]: value,
      });
    });
  };

  onImportPress = (ids: string[]) => {
    this.props.dispatchImportGame({ ids });
  };

  //
  // Render

  render() {
    return (
      <ImportGame
        {...this.props}
        onInputChange={this.onInputChange}
        onImportPress={this.onImportPress}
      />
    );
  }
}

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameConnector);
