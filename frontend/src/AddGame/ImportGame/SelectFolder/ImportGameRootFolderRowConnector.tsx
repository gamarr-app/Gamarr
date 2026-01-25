import { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteRootFolder } from 'Store/Actions/rootFolderActions';
import ImportGameRootFolderRow from './ImportGameRootFolderRow';

interface UnmappedFolder {
  name: string;
  path: string;
  relativePath: string;
}

function createMapStateToProps() {
  return createSelector(() => {
    return {};
  });
}

const mapDispatchToProps = {
  deleteRootFolder,
};

interface ImportGameRootFolderRowConnectorProps {
  id: number;
  path: string;
  freeSpace: number;
  unmappedFolders: UnmappedFolder[];
  deleteRootFolder: (payload: { id: number }) => void;
}

class ImportGameRootFolderRowConnector extends Component<ImportGameRootFolderRowConnectorProps> {
  //
  // Listeners

  onDeletePress = () => {
    this.props.deleteRootFolder({ id: this.props.id });
  };

  //
  // Render

  render() {
    return (
      <ImportGameRootFolderRow
        {...this.props}
        onDeletePress={this.onDeletePress}
      />
    );
  }
}

export default connect(
  createMapStateToProps,
  mapDispatchToProps
)(ImportGameRootFolderRowConnector);
