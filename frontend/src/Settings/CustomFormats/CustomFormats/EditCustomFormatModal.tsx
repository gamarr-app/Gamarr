import React, { useCallback, useState } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditCustomFormatModalContentConnector from './EditCustomFormatModalContentConnector';

interface EditCustomFormatModalProps {
  id?: number;
  tagsFromId?: number;
  isOpen: boolean;
  onModalClose: () => void;
  onDeleteCustomFormatPress?: () => void;
}

function EditCustomFormatModal({
  id,
  tagsFromId,
  isOpen,
  onModalClose,
  onDeleteCustomFormatPress,
}: EditCustomFormatModalProps) {
  const dispatch = useDispatch();
  const [height, setHeight] = useState<number | 'auto'>('auto');

  const handleContentHeightChange = useCallback((newHeight: number) => {
    setHeight((prev) => {
      if (prev === 'auto' || newHeight > prev) {
        return newHeight;
      }
      return prev;
    });
  }, []);

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.customFormats' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  return (
    <Modal
      style={{ height: height === 'auto' ? 'auto' : `${height}px` }}
      isOpen={isOpen}
      size={sizes.LARGE}
      onModalClose={handleModalClose}
    >
      <EditCustomFormatModalContentConnector
        id={id}
        tagsFromId={tagsFromId}
        onContentHeightChange={handleContentHeightChange}
        onModalClose={handleModalClose}
        onDeleteCustomFormatPress={onDeleteCustomFormatPress}
      />
    </Modal>
  );
}

export default EditCustomFormatModal;
