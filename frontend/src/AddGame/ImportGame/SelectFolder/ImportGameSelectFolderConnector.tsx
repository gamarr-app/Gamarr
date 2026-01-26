import { push } from 'connected-react-router';
import _ from 'lodash';
import { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import {
  addRootFolder,
  deleteRootFolder,
  fetchRootFolders,
} from 'Store/Actions/rootFolderActions';
import createRootFoldersSelector from 'Store/Selectors/createRootFoldersSelector';
import createSystemStatusSelector from 'Store/Selectors/createSystemStatusSelector';
import ImportGameSelectFolder from './ImportGameSelectFolder';

// eslint-disable-next-line init-declarations
declare const window: Window & {
  Gamarr: {
    urlBase: string;
  };
};

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

interface RootFolderItemFromState {
  id: number;
  path: string;
  freeSpace?: number;
  unmappedFolders?: object[];
}

function createMapStateToProps() {
  return createSelector(
    createRootFoldersSelector(),
    createSystemStatusSelector(),
    (rootFolders, systemStatus) => {
      return {
        ...rootFolders,
        isWindows: systemStatus.isWindows,
      };
    }
  );
}

const mapDispatchToProps = {
  fetchRootFolders,
  addRootFolder,
  deleteRootFolder,
  push,
};

interface ImportGameSelectFolderConnectorProps {
  isSaving: boolean;
  isWindows?: boolean;
  isFetching?: boolean;
  isPopulated?: boolean;
  saveError?: object;
  items: RootFolderItemFromState[];
  fetchRootFolders: () => void;
  addRootFolder: (payload: { path: string }) => void;
  deleteRootFolder: (payload: { id: number }) => void;
  push: (path: string) => void;
}

class ImportGameSelectFolderConnector extends Component<ImportGameSelectFolderConnectorProps> {
  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchRootFolders();
  }

  componentDidUpdate(prevProps: ImportGameSelectFolderConnectorProps) {
    const { items, isSaving, saveError } = this.props;

    if (prevProps.isSaving && !isSaving && !saveError) {
      const newRootFolders = _.differenceBy(
        items,
        prevProps.items,
        (item) => item.id
      );

      if (newRootFolders.length === 1) {
        this.props.push(
          `${window.Gamarr.urlBase}/add/import/${newRootFolders[0].id}`
        );
      }
    }
  }

  //
  // Listeners

  onNewRootFolderSelect = (path: string) => {
    this.props.addRootFolder({ path });
  };

  onDeleteRootFolderPress = (id: number) => {
    this.props.deleteRootFolder({ id });
  };

  //
  // Render

  render() {
    return (
      <ImportGameSelectFolder
        {...this.props}
        isWindows={this.props.isWindows || false}
        isFetching={this.props.isFetching || false}
        isPopulated={this.props.isPopulated || false}
        items={this.props.items.map((item) => ({
          ...item,
          freeSpace: item.freeSpace || 0,
          unmappedFolders: (item.unmappedFolders || []) as UnmappedFolder[],
        }))}
        onNewRootFolderSelect={this.onNewRootFolderSelect}
        onDeleteRootFolderPress={this.onDeleteRootFolderPress}
      />
    );
  }
}

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameSelectFolderConnector);
