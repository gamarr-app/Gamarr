import React from 'react';
import Modal from 'Components/Modal/Modal';
import { Image } from 'Game/Game';
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
  onModalClose: () => void;
}

function AddNewGameModal(props: AddNewGameModalProps) {
  const { isOpen, onModalClose, ...otherProps } = props;

  return (
    <Modal isOpen={isOpen} onModalClose={onModalClose}>
      {/* eslint-disable-next-line @typescript-eslint/no-explicit-any */}
      <AddNewGameModalContentConnector
        {...(otherProps as any)}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default AddNewGameModal;
