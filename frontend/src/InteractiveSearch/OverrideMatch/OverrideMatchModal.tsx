import Modal from 'Components/Modal/Modal';
import DownloadProtocol from 'DownloadClient/DownloadProtocol';
import { sizes } from 'Helpers/Props';
import Language from 'Language/Language';
import { QualityModel } from 'Quality/Quality';
import OverrideMatchModalContent from './OverrideMatchModalContent';

interface OverrideMatchModalProps {
  isOpen: boolean;
  title: string;
  indexerId: number;
  guid: string;
  gameId?: number;
  languages: Language[];
  quality: QualityModel;
  protocol: DownloadProtocol;
  isGrabbing: boolean;
  grabError?: string;
  onModalClose(): void;
}

function OverrideMatchModal(props: OverrideMatchModalProps) {
  const {
    isOpen,
    title,
    indexerId,
    guid,
    gameId,
    languages,
    quality,
    protocol,
    isGrabbing,
    grabError,
    onModalClose,
  } = props;

  return (
    <Modal isOpen={isOpen} size={sizes.LARGE} onModalClose={onModalClose}>
      <OverrideMatchModalContent
        title={title}
        indexerId={indexerId}
        guid={guid}
        gameId={gameId}
        languages={languages}
        quality={quality}
        protocol={protocol}
        isGrabbing={isGrabbing}
        grabError={grabError}
        onModalClose={onModalClose}
      />
    </Modal>
  );
}

export default OverrideMatchModal;
