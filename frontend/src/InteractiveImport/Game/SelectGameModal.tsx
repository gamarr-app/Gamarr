import React from 'react';
import Modal from 'Components/Modal/Modal';
import Game from 'Game/Game';
import SelectGameModalContent from './SelectGameModalContent';

interface SelectGameModalProps {
  isOpen: boolean;
  modalTitle: string;
  onGameSelect(game: Game): void;
  onModalClose(): void;
}

function SelectGameModal(props: SelectGameModalProps) {
  const { isOpen, modalTitle, onGameSelect, onModalClose } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <SelectGameModalContent
        modalTitle={modalTitle}
        onGameSelect={onGameSelect}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default SelectGameModal;
