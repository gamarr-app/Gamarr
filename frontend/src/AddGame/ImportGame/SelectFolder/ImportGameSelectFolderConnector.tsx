import { push } from 'connected-react-router';
import _ from 'lodash';
import React, { Component } from 'react';
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

interface RootFolderItem {
  id: number;
  path: string;
  freeSpace?: number;
  unmappedFolders?: UnmappedFolder[];
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
  items: RootFolderItem[];
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
          unmappedFolders: item.unmappedFolders || [],
        }))}
        onNewRootFolderSelect={this.onNewRootFolderSelect}
        onDeleteRootFolderPress={this.onDeleteRootFolderPress}
      />
    );
  }
}

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameSelectFolderConnector as any);
