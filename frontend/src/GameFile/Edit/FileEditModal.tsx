import React from 'react';
import Modal from 'Components/Modal/Modal';
import FileEditModalContentConnector from './FileEditModalContentConnector';

interface FileEditModalProps {
  isOpen: boolean;
  onModalClose: () => void;
  gameFileId: number;
}

function FileEditModal(props: FileEditModalProps) {
  const { isOpen, onModalClose, gameFileId } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <FileEditModalContentConnector
        gameFileId={gameFileId}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default FileEditModal;
