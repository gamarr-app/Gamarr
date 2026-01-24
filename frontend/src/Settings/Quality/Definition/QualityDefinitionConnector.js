import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import { setQualityDefinitionValue } from 'Store/Actions/settingsActions';
import QualityDefinition from './QualityDefinition';

const mapDispatchToProps = {
  setQualityDefinitionValue,
  clearPendingChanges
};

class QualityDefinitionConnector extends Component {

  componentWillUnmount() {
    this.props.clearPendingChanges({ section: 'settings.qualityDefinitions' });
  }

  //
  // Listeners

  onTitleChange = ({ value }) => {
    this.props.setQualityDefinitionValue({ id: this.props.id, name: 'title', value });
  };

  //
  // Render

  render() {
    return (
      <QualityDefinition
        {...this.props}
        onTitleChange={this.onTitleChange}
      />
    );
  }
}

QualityDefinitionConnector.propTypes = {
  id: PropTypes.number.isRequired,
  minSize: PropTypes.number,
  maxSize: PropTypes.number,
  preferredSize: PropTypes.number,
  setQualityDefinitionValue: PropTypes.func.isRequired,
  clearPendingChanges: PropTypes.func.isRequired
};

export default connect(null, mapDispatchToProps)(QualityDefinitionConnector);
