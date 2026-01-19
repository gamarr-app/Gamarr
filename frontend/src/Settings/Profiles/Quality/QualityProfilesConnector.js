import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import { fetchGameCollections } from 'Store/Actions/gameCollectionActions';
import { cloneQualityProfile, deleteQualityProfile, fetchQualityProfiles } from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import QualityProfiles from './QualityProfiles';

function createMapStateToProps() {
  return createSelector(
    createSortedSectionSelector('settings.qualityProfiles', sortByProp('name')),
    (qualityProfiles) => qualityProfiles
  );
}

const mapDispatchToProps = {
  dispatchFetchQualityProfiles: fetchQualityProfiles,
  dispatchDeleteQualityProfile: deleteQualityProfile,
  dispatchCloneQualityProfile: cloneQualityProfile,
  dispatchFetchGameCollections: fetchGameCollections
};

class QualityProfilesConnector extends Component {

  //
  // Lifecycle

  componentDidMount() {
    this.props.dispatchFetchQualityProfiles();
    this.props.dispatchFetchGameCollections();
  }

  //
  // Listeners

  onConfirmDeleteQualityProfile = (id) => {
    this.props.dispatchDeleteQualityProfile({ id });
  };

  onCloneQualityProfilePress = (id) => {
    this.props.dispatchCloneQualityProfile({ id });
  };

  //
  // Render

  render() {
    return (
      <QualityProfiles
        onConfirmDeleteQualityProfile={this.onConfirmDeleteQualityProfile}
        onCloneQualityProfilePress={this.onCloneQualityProfilePress}
        {...this.props}
      />
    );
  }
}

QualityProfilesConnector.propTypes = {
  dispatchFetchQualityProfiles: PropTypes.func.isRequired,
  dispatchDeleteQualityProfile: PropTypes.func.isRequired,
  dispatchCloneQualityProfile: PropTypes.func.isRequired,
  dispatchFetchGameCollections: PropTypes.func.isRequired
};

export default connect(createMapStateToProps, mapDispatchToProps)(QualityProfilesConnector);
