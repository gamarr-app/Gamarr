import React from 'react';
import Modal from 'Components/Modal/Modal';
import DiscoverGameOverviewOptionsModalContentConnector from './DiscoverGameOverviewOptionsModalContentConnector';

interface DiscoverGameOverviewOptionsModalProps {
  isOpen: boolean;
  onModalClose: (...args: unknown[]) => void;
}

function DiscoverGameOverviewOptionsModal({
  isOpen,
  onModalClose,
  ...otherProps
}: DiscoverGameOverviewOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <DiscoverGameOverviewOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default DiscoverGameOverviewOptionsModal;
