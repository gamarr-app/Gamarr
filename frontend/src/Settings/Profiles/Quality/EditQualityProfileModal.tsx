import React, { useCallback, useState } from 'react';
import { useDispatch } from 'react-redux';
import Modal from 'Components/Modal/Modal';
import { sizes } from 'Helpers/Props';
import { clearPendingChanges } from 'Store/Actions/baseActions';
import EditQualityProfileModalContentConnector from './EditQualityProfileModalContentConnector';

interface EditQualityProfileModalProps {
  id?: number;
  isOpen: boolean;
  onModalClose: () => void;
}

function EditQualityProfileModal({
  id,
  isOpen,
  onModalClose,
}: EditQualityProfileModalProps) {
  const dispatch = useDispatch();
  const [height, setHeight] = useState<number | 'auto'>('auto');

  const handleModalClose = useCallback(() => {
    dispatch(clearPendingChanges({ section: 'settings.qualityProfiles' }));
    onModalClose();
  }, [dispatch, onModalClose]);

  const handleContentHeightChange = useCallback(
    (newHeight: number) => {
      if (height === 'auto' || newHeight > height) {
        setHeight(newHeight);
      }
    },
    [height]
  );

  return (
    <Modal
      style={{ height: height !== 'auto' ? `${height}px` : undefined }}
      isOpen={isOpen}
      size={sizes.EXTRA_LARGE}
      onModalClose={handleModalClose}
    >
      <EditQualityProfileModalContentConnector
        id={id}
        onContentHeightChange={handleContentHeightChange}
        onModalClose={handleModalClose}
      />
    </Modal>
  );
}

export default EditQualityProfileModal;
