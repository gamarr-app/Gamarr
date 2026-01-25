import React from 'react';
import Modal from 'Components/Modal/Modal';
import CollectionOverviewOptionsModalContentConnector from './CollectionOverviewOptionsModalContentConnector';

interface CollectionOverviewOptionsModalProps {
  isOpen: boolean;
  onModalClose: () => void;
}

function CollectionOverviewOptionsModal({
  isOpen,
  onModalClose,
}: CollectionOverviewOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <CollectionOverviewOptionsModalContentConnector
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default CollectionOverviewOptionsModal;
