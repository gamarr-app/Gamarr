import React from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import DeleteGameModalContent, {
  DeleteGameModalContentProps,
} from './DeleteGameModalContent';

interface DeleteGameModalProps extends DeleteGameModalContentProps {
  isOpen: boolean;
}

function DeleteGameModal({
  isOpen,
  onModalClose,
  ...otherProps
}: DeleteGameModalProps) {
  return (
    <Modal isOpen={isOpen} size={sizes.MEDIUM} onModalClose={onModalClose}>
      <DeleteGameModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default DeleteGameModal;
