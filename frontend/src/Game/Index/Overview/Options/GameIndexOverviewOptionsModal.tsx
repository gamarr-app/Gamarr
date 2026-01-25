import Modal from 'Components/Modal/Modal';
import GameIndexOverviewOptionsModalContent from './GameIndexOverviewOptionsModalContent';

interface GameIndexOverviewOptionsModalProps {
  isOpen: boolean;
  onModalClose(...args: unknown[]): void;
}

function GameIndexOverviewOptionsModal({
  isOpen,
  onModalClose,
  ...otherProps
}: GameIndexOverviewOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <GameIndexOverviewOptionsModalContent
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default GameIndexOverviewOptionsModal;
