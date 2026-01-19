import React from 'react';
import Modal from 'Components/Modal/Modal';
import GameIndexPosterOptionsModalContent from './GameIndexPosterOptionsModalContent';

interface GameIndexPosterOptionsModalProps {
  isOpen: boolean;
  onModalClose(...args: unknown[]): unknown;
}

function GameIndexPosterOptionsModal({
  isOpen,
  onModalClose,
}: GameIndexPosterOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <GameIndexPosterOptionsModalContent onModalClose={onModalClose} />
    </Modal>
  );
}

export default GameIndexPosterOptionsModal;
