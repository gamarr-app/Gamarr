import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { updateGameFiles } from 'Store/Actions/gameFileActions';
import { fetchQualityProfileSchema } from 'Store/Actions/settingsActions';
import createGameFileSelector from 'Store/Selectors/createGameFileSelector';
import getQualities from 'Utilities/Quality/getQualities';
import FileEditModalContent from './FileEditModalContent';

function createMapStateToProps() {
  return createSelector(
    createGameFileSelector(),
    (state) => state.settings.qualityProfiles,
    (state) => state.settings.languages,
    (gameFile, qualityProfiles, languages) => {

      const filterItems = ['Any', 'Original'];
      const filteredLanguages = languages.items.filter((lang) => !filterItems.includes(lang.name));

      const quality = gameFile.quality;

      return {
        isFetching: qualityProfiles.isSchemaFetching || languages.isFetching,
        isPopulated: qualityProfiles.isSchemaPopulated && languages.isPopulated,
        error: qualityProfiles.error || languages.error,
        qualityId: quality ? quality.quality.id : 0,
        qualities: getQualities(qualityProfiles.schema.items),
        languageIds: gameFile.languages ? gameFile.languages.map((l) => l.id) : [],
        languages: filteredLanguages,
        indexerFlags: gameFile.indexerFlags,
        edition: gameFile.edition,
        releaseGroup: gameFile.releaseGroup,
        relativePath: gameFile.relativePath
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchFetchQualityProfileSchema: fetchQualityProfileSchema,
  dispatchUpdateGameFiles: updateGameFiles
};

class FileEditModalContentConnector extends Component {

  //
  // Lifecycle

  componentDidMount = () => {
    if (!this.props.isPopulated) {
      this.props.dispatchFetchQualityProfileSchema();
    }
  };

  //
  // Listeners

  onSaveInputs = ( payload ) => {
    const {
      qualityId,
      languageIds,
      edition,
      releaseGroup,
      indexerFlags
    } = payload;

    const quality = this.props.qualities.find((item) => item.id === qualityId);

    const languages = [];

    languageIds.forEach((languageId) => {
      const language = this.props.languages.find((item) => item.id === parseInt(languageId));

      if (language !== undefined) {
        languages.push(language);
      }
    });

    const revision = {
      version: 1,
      real: 0
    };

    this.props.dispatchUpdateGameFiles({
      files: [{
        id: this.props.gameFileId,
        languages,
        indexerFlags,
        edition,
        releaseGroup,
        quality: {
          quality,
          revision
        }
      }]
    });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <FileEditModalContent
        {...this.props}
        onSaveInputs={this.onSaveInputs}
      />
    );
  }
}

FileEditModalContentConnector.propTypes = {
  gameFileId: PropTypes.number.isRequired,
  isFetching: PropTypes.bool.isRequired,
  isPopulated: PropTypes.bool.isRequired,
  error: PropTypes.object,
  qualities: PropTypes.arrayOf(PropTypes.object).isRequired,
  languages: PropTypes.arrayOf(PropTypes.object).isRequired,
  languageIds: PropTypes.arrayOf(PropTypes.number).isRequired,
  indexerFlags: PropTypes.number.isRequired,
  qualityId: PropTypes.number.isRequired,
  edition: PropTypes.string.isRequired,
  releaseGroup: PropTypes.string.isRequired,
  relativePath: PropTypes.string.isRequired,
  dispatchFetchQualityProfileSchema: PropTypes.func.isRequired,
  dispatchUpdateGameFiles: PropTypes.func.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(FileEditModalContentConnector);
