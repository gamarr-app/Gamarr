import React from 'react';
import Modal from 'Components/Modal/Modal';
import OrganizeGamesModalContent from './OrganizeGamesModalContent';

interface OrganizeGamesModalProps {
  isOpen: boolean;
  gameIds: number[];
  onModalClose: () => void;
}

function OrganizeGamesModal(props: OrganizeGamesModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      <OrganizeGamesModalContent {...otherProps} onModalClose={onModalClose} />
    </Modal>
  );
}

export default OrganizeGamesModal;
