import React, { useCallback, useState } from 'react';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import ExportCustomFormatModalContentConnector from './ExportCustomFormatModalContentConnector';

interface ExportCustomFormatModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
}

function ExportCustomFormatModal({
  id,
  isOpen,
  onModalClose,
}: ExportCustomFormatModalProps) {
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
      <ExportCustomFormatModalContentConnector
        id={id}
        onContentHeightChange={handleContentHeightChange}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default ExportCustomFormatModal;
