import PropTypes from 'prop-types';
import React from 'react';
import Modal from 'Components/Modal/Modal';
import DiscoverGameOverviewOptionsModalContentConnector from './DiscoverGameOverviewOptionsModalContentConnector';

function DiscoverGameOverviewOptionsModal({ isOpen, onModalClose, ...otherProps }) {
  return (
    <Modal
      isOpen={isOpen}
      onModalClose={onModalClose}
    >
      <DiscoverGameOverviewOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

DiscoverGameOverviewOptionsModal.propTypes = {
  isOpen: PropTypes.bool.isRequired,
  onModalClose: PropTypes.func.isRequired
};

export default DiscoverGameOverviewOptionsModal;
