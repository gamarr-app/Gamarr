import Modal from 'Components/Modal/Modal';
import { Image } from 'Game/Game';
import AddNewDiscoverGameModalContentConnector from './AddNewDiscoverGameModalContentConnector';

interface AddNewDiscoverGameModalProps {
  isOpen: boolean;
  onModalClose: (didAdd?: boolean) => void;
  igdbId: number;
  title: string;
  year?: number;
  overview?: string;
  images?: Image[];
  folder?: string;
}

function AddNewDiscoverGameModal({
  isOpen,
  onModalClose,
  ...otherProps
}: AddNewDiscoverGameModalProps) {
  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddNewDiscoverGameModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default AddNewDiscoverGameModal;
