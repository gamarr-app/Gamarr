import { useSelector } from 'react-redux';
import DescriptionList from 'Components/DescriptionList/DescriptionList';
import DescriptionListItem from 'Components/DescriptionList/DescriptionListItem';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { sizes } from 'Helpers/Props';
import createUISettingsSelector from 'Store/Selectors/createUISettingsSelector';
import formatDateTime from 'Utilities/Date/formatDateTime';
import formatBytes from 'Utilities/Number/formatBytes';
import translate from 'Utilities/String/translate';

interface FileDetailsModalProps {
  isOpen: boolean;
  onModalClose: () => void;
  path?: string;
  size?: number;
  dateAdded?: string;
  sceneName?: string;
  releaseGroup?: string;
}

function FileDetailsModal(props: FileDetailsModalProps) {
  const {
    isOpen,
    onModalClose,
    path,
    size,
    dateAdded,
    sceneName,
    releaseGroup,
  } = props;

  const { shortDateFormat, timeFormat } = useSelector(
    createUISettingsSelector()
  );

  return (
    <Modal isOpen={isOpen} size={sizes.MEDIUM} onModalClose={onModalClose}>
      <ModalContent onModalClose={onModalClose}>
        <ModalHeader>{translate('Details')}</ModalHeader>

        <ModalBody>
          <DescriptionList>
            {path ? (
              <DescriptionListItem title={translate('Path')} data={path} />
            ) : null}

            {size ? (
              <DescriptionListItem
                title={translate('Size')}
                data={formatBytes(size)}
              />
            ) : null}

            {dateAdded ? (
              <DescriptionListItem
                title={translate('Added')}
                data={formatDateTime(dateAdded, shortDateFormat, timeFormat)}
              />
            ) : null}

            {sceneName ? (
              <DescriptionListItem
                title={translate('SceneName')}
                data={sceneName}
              />
            ) : null}

            {releaseGroup ? (
              <DescriptionListItem
                title={translate('ReleaseGroup')}
                data={releaseGroup}
              />
            ) : null}
          </DescriptionList>
        </ModalBody>

        <ModalFooter>
          <Button onPress={onModalClose}>{translate('Close')}</Button>
        </ModalFooter>
      </ModalContent>
    </Modal>
  );
}

export default FileDetailsModal;
