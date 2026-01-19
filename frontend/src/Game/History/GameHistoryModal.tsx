import React from 'react';
import Modal from 'Components/Modal/Modal';
import GameHistoryModalContent, {
  GameHistoryModalContentProps,
} from 'Game/History/GameHistoryModalContent';
import { sizes } from 'Helpers/Props';

interface GameHistoryModalProps extends GameHistoryModalContentProps {
  isOpen: boolean;
}

function GameHistoryModal({
  isOpen,
  onModalClose,
  ...otherProps
}: GameHistoryModalProps) {
  return (
    <Modal
      isOpen={isOpen}
      size={sizes.EXTRA_EXTRA_LARGE}
      onModalClose={onModalClose}
    >
      <GameHistoryModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default GameHistoryModal;
