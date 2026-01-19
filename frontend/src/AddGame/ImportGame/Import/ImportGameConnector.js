import _ from 'lodash';
import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import createRouteMatchShape from 'Helpers/Props/Shapes/createRouteMatchShape';
import { setAddGameDefault } from 'Store/Actions/addGameActions';
import { clearImportGame, importGame, setImportGameValue } from 'Store/Actions/importGameActions';
import { fetchRootFolders } from 'Store/Actions/rootFolderActions';
import ImportGame from './ImportGame';

function createMapStateToProps() {
  return createSelector(
    (state, { match }) => match,
    (state) => state.rootFolders,
    (state) => state.addGame,
    (state) => state.importGame,
    (state) => state.settings.qualityProfiles,
    (
      match,
      rootFolders,
      addGame,
      importGameState,
      qualityProfiles
    ) => {
      const {
        isFetching: rootFoldersFetching,
        isPopulated: rootFoldersPopulated,
        error: rootFoldersError,
        items
      } = rootFolders;

      const rootFolderId = parseInt(match.params.rootFolderId);

      const result = {
        rootFolderId,
        rootFoldersFetching,
        rootFoldersPopulated,
        rootFoldersError,
        qualityProfiles: qualityProfiles.items,
        defaultQualityProfileId: addGame.defaults.qualityProfileId
      };

      if (items.length) {
        const rootFolder = _.find(items, { id: rootFolderId });

        return {
          ...result,
          ...rootFolder,
          items: importGameState.items
        };
      }

      return result;
    }
  );
}

const mapDispatchToProps = {
  dispatchSetImportGameValue: setImportGameValue,
  dispatchImportGame: importGame,
  dispatchClearImportGame: clearImportGame,
  dispatchFetchRootFolders: fetchRootFolders,
  dispatchSetAddGameDefault: setAddGameDefault
};

class ImportGameConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    const {
      rootFolderId,
      qualityProfiles,
      defaultQualityProfileId,
      dispatchFetchRootFolders,
      dispatchSetAddGameDefault
    } = this.props;

    dispatchFetchRootFolders({ id: rootFolderId, timeout: false });

    let setDefaults = false;
    const setDefaultPayload = {};

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

  onInputChange = (ids, name, value) => {
    this.props.dispatchSetAddGameDefault({ [name]: value });

    ids.forEach((id) => {
      this.props.dispatchSetImportGameValue({
        id,
        [name]: value
      });
    });
  };

  onImportPress = (ids) => {
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

const routeMatchShape = createRouteMatchShape({
  rootFolderId: PropTypes.string.isRequired
});

ImportGameConnector.propTypes = {
  match: routeMatchShape.isRequired,
  rootFolderId: PropTypes.number.isRequired,
  rootFoldersFetching: PropTypes.bool.isRequired,
  rootFoldersPopulated: PropTypes.bool.isRequired,
  qualityProfiles: PropTypes.arrayOf(PropTypes.object).isRequired,
  defaultQualityProfileId: PropTypes.number.isRequired,
  dispatchSetImportGameValue: PropTypes.func.isRequired,
  dispatchImportGame: PropTypes.func.isRequired,
  dispatchClearImportGame: PropTypes.func.isRequired,
  dispatchFetchRootFolders: PropTypes.func.isRequired,
  dispatchSetAddGameDefault: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(ImportGameConnector);
