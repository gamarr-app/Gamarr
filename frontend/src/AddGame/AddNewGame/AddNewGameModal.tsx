import Modal from 'Components/Modal/Modal';
import { GamePlatform, Image } from 'Game/Game';
import AddNewGameModalContentConnector from './AddNewGameModalContentConnector';

interface AddNewGameModalProps {
  isOpen: boolean;
  igdbId: number;
  steamAppId?: number;
  title: string;
  year: number;
  overview?: string;
  folder: string;
  images: Image[];
  platforms?: GamePlatform[];
  onModalClose: () => void;
}

function AddNewGameModal(props: AddNewGameModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <AddNewGameModalContentConnector
        {...otherProps}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default AddNewGameModal;
