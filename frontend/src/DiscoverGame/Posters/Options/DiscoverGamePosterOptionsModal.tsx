import Modal from 'Components/Modal/Modal';
import DiscoverGamePosterOptionsModalContentConnector from './DiscoverGamePosterOptionsModalContentConnector';

interface DiscoverGamePosterOptionsModalProps {
  isOpen: boolean;
  onModalClose: (...args: unknown[]) => void;
}

function DiscoverGamePosterOptionsModal({
  isOpen,
  onModalClose,
  ...otherProps
}: DiscoverGamePosterOptionsModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <DiscoverGamePosterOptionsModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default DiscoverGamePosterOptionsModal;
