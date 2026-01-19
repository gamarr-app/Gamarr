import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { deleteGameFile, setGameFilesSort, setGameFilesTableOption } from 'Store/Actions/gameFileActions';
import { fetchLanguages, fetchQualityProfileSchema } from 'Store/Actions/settingsActions';
import createClientSideCollectionSelector from 'Store/Selectors/createClientSideCollectionSelector';
import createGameSelector from 'Store/Selectors/createGameSelector';
import getQualities from 'Utilities/Quality/getQualities';
import GameFileEditorTableContent from './GameFileEditorTableContent';

function createMapStateToProps() {
  return createSelector(
    (state, { gameId }) => gameId,
    createClientSideCollectionSelector('gameFiles'),
    (state) => state.settings.languages,
    (state) => state.settings.qualityProfiles,
    createGameSelector(),
    (
      gameId,
      gameFiles,
      languageProfiles,
      qualityProfiles
    ) => {
      const languages = languageProfiles.items;
      const qualities = getQualities(qualityProfiles.schema.items);
      const filesForGame = gameFiles.items.filter((file) => file.gameId === gameId);

      return {
        items: filesForGame,
        columns: gameFiles.columns,
        sortKey: gameFiles.sortKey,
        sortDirection: gameFiles.sortDirection,
        isDeleting: gameFiles.isDeleting,
        isSaving: gameFiles.isSaving,
        error: null,
        languages,
        qualities
      };
    }
  );
}

const mapDispatchToProps = {
  fetchQualityProfileSchema,
  fetchLanguages,
  deleteGameFile,
  setGameFilesTableOption,
  setGameFilesSort
};

class GameFileEditorTableContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.fetchLanguages();
    this.props.fetchQualityProfileSchema();
  }

  //
  // Listeners

  onDeletePress = (gameFileId) => {
    this.props.deleteGameFile({
      id: gameFileId
    });
  };

  onTableOptionChange = (payload) => {
    this.props.setGameFilesTableOption(payload);
  };

  onSortPress = (sortKey, sortDirection) => {
    this.props.setGameFilesSort({
      sortKey,
      sortDirection
    });
  };

  //
  // Render

  render() {
    return (
      <GameFileEditorTableContent
        {...this.props}
        onDeletePress={this.onDeletePress}
        onTableOptionChange={this.onTableOptionChange}
        onSortPress={this.onSortPress}
      />
    );
  }
}

GameFileEditorTableContentConnector.propTypes = {
  gameId: PropTypes.number.isRequired,
  languages: PropTypes.arrayOf(PropTypes.object).isRequired,
  qualities: PropTypes.arrayOf(PropTypes.object).isRequired,
  fetchLanguages: PropTypes.func.isRequired,
  fetchQualityProfileSchema: PropTypes.func.isRequired,
  deleteGameFile: PropTypes.func.isRequired,
  setGameFilesTableOption: PropTypes.func.isRequired,
  setGameFilesSort: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(GameFileEditorTableContentConnector);
