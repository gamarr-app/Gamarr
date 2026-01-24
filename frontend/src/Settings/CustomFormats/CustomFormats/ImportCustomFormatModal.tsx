import React, { useCallback, useState } from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import ImportCustomFormatModalContentConnector from './ImportCustomFormatModalContentConnector';

interface ImportCustomFormatModalProps {
  isOpen: boolean;
  onModalClose: () => void;
}

function ImportCustomFormatModal({
  isOpen,
  onModalClose,
}: ImportCustomFormatModalProps) {
  const [height, setHeight] = useState<number | 'auto'>('auto');

  const handleContentHeightChange = useCallback((newHeight: number) => {
    setHeight((prev) => {
      if (prev === 'auto' || newHeight > prev) {
        return newHeight;
      }
      return prev;
    });
  }, []);

  return (
    <Modal
      style={{ height: height === 'auto' ? 'auto' : `${height}px` }}
      isOpen={isOpen}
      size={sizes.LARGE}
      onModalClose={onModalClose}
    >
      <ImportCustomFormatModalContentConnector
        onContentHeightChange={handleContentHeightChange}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default ImportCustomFormatModal;
