import { useCallback, useState } from 'react';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, scrollDirections } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from '../ReleaseGroup/SelectReleaseGroupModalContent.css';

interface SelectVersionModalContentProps {
  version: string;
  modalTitle: string;
  onVersionSelect(version: string): void;
  onModalClose(): void;
}

function SelectVersionModalContent(props: SelectVersionModalContentProps) {
  const { modalTitle, onVersionSelect, onModalClose } = props;
  const [version, setVersion] = useState(props.version);

  const onVersionChange = useCallback(
    ({ value }: { value: string }) => {
      setVersion(value);
    },
    [setVersion]
  );

  const onVersionSelectWrapper = useCallback(() => {
    onVersionSelect(version);
  }, [version, onVersionSelect]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('SetVersionModalTitle', { modalTitle })}
      </ModalHeader>

      <ModalBody
        className={styles.modalBody}
        scrollDirection={scrollDirections.NONE}
      >
        <Form>
          <FormGroup>
            <FormLabel>{translate('Version')}</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="version"
              value={version}
              autoFocus={true}
              onChange={onVersionChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.SUCCESS} onPress={onVersionSelectWrapper}>
          {translate('SetVersion')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default SelectVersionModalContent;
