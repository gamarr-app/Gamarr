import React from 'react';
import Modal from 'Components/Modal/Modal';
import DeleteGameModalContent from './DeleteGameModalContent';

interface DeleteGameModalProps {
  isOpen: boolean;
  gameIds: number[];
  onModalClose(): void;
}

function DeleteGameModal(props: DeleteGameModalProps) {
  const { isOpen, gameIds, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <DeleteGameModalContent gameIds={gameIds} onModalClose={onModalClose} />
    </Modal>
  );
}

export default DeleteGameModal;
