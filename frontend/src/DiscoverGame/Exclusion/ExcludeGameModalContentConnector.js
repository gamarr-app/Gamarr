import PropTypes from 'prop-types';
import React, { Component } from 'react';
import { connect } from 'react-redux';
import { addImportListExclusions } from 'Store/Actions/discoverGameActions';
import ExcludeGameModalContent from './ExcludeGameModalContent';

const mapDispatchToProps = {
  addImportListExclusions
};

class ExcludeGameModalContentConnector extends Component {

  //
  // Listeners

  onExcludePress = () => {
    this.props.addImportListExclusions({ ids: [this.props.igdbId] });

    this.props.onModalClose(true);
  };

  //
  // Render

  render() {
    return (
      <ExcludeGameModalContent
        {...this.props}
        onExcludePress={this.onExcludePress}
      />
    );
  }
}

ExcludeGameModalContentConnector.propTypes = {
  igdbId: PropTypes.number.isRequired,
  title: PropTypes.string.isRequired,
  year: PropTypes.number.isRequired,
  onModalClose: PropTypes.func.isRequired,
  addImportListExclusions: PropTypes.func.isRequired
};

export default connect(undefined, mapDispatchToProps)(ExcludeGameModalContentConnector);
