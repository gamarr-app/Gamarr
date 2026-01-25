import { useCallback, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import Language from 'Language/Language';
import Quality from 'Quality/Quality';
import translate from 'Utilities/String/translate';

interface SelectOption {
  key: number;
  value: string;
}

interface InputChangeEvent {
  name: string;
  value: number | number[] | string;
}

export interface FileEditSavePayload {
  qualityId: number;
  languageIds: number[];
  indexerFlags: number;
  edition: string;
  releaseGroup: string;
}

interface FileEditModalContentProps {
  qualityId: number;
  relativePath: string;
  edition: string;
  releaseGroup: string;
  languageIds: number[];
  languages: Language[];
  indexerFlags: number;
  isFetching: boolean;
  isPopulated: boolean;
  error: Error | null;
  qualities: Quality[];
  onSaveInputs: (payload: FileEditSavePayload) => void;
  onModalClose: () => void;
}

function FileEditModalContent(props: FileEditModalContentProps) {
  const {
    isFetching,
    isPopulated,
    error,
    qualities,
    languages,
    relativePath,
    onModalClose,
    onSaveInputs,
    qualityId: initialQualityId,
    languageIds: initialLanguageIds,
    indexerFlags: initialIndexerFlags,
    edition: initialEdition,
    releaseGroup: initialReleaseGroup,
  } = props;

  const [qualityId, setQualityId] = useState(initialQualityId);
  const [languageIds, setLanguageIds] = useState(initialLanguageIds);
  const [indexerFlags, setIndexerFlags] = useState(initialIndexerFlags);
  const [edition, setEdition] = useState(initialEdition);
  const [releaseGroup, setReleaseGroup] = useState(initialReleaseGroup);

  const onQualityChange = useCallback(({ value }: { value: string }) => {
    setQualityId(parseInt(value));
  }, []);

  const onInputChange = useCallback(({ name, value }: InputChangeEvent) => {
    switch (name) {
      case 'languageIds':
        setLanguageIds(value as number[]);
        break;
      case 'indexerFlags':
        setIndexerFlags(value as number);
        break;
      case 'edition':
        setEdition(value as string);
        break;
      case 'releaseGroup':
        setReleaseGroup(value as string);
        break;
      default:
        break;
    }
  }, []);

  const onSavePress = useCallback(() => {
    onSaveInputs({
      qualityId,
      languageIds,
      indexerFlags,
      edition,
      releaseGroup,
    });
  }, [
    onSaveInputs,
    qualityId,
    languageIds,
    indexerFlags,
    edition,
    releaseGroup,
  ]);

  const qualityOptions: SelectOption[] = qualities.map(({ id, name }) => ({
    key: id,
    value: name,
  }));

  const languageOptions: SelectOption[] = languages.map(({ id, name }) => ({
    key: id,
    value: name,
  }));

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {translate('EditGameFile')} - {relativePath}
      </ModalHeader>

      <ModalBody>
        {isFetching && <LoadingIndicator />}

        {!isFetching && !!error && (
          <Alert kind={kinds.DANGER}>{translate('QualitiesLoadError')}</Alert>
        )}

        {isPopulated && !error && (
          <Form>
            <FormGroup>
              <FormLabel>{translate('Quality')}</FormLabel>

              <FormInputGroup
                type={inputTypes.SELECT}
                name="quality"
                value={qualityId as unknown as string}
                values={qualityOptions}
                onChange={onQualityChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('Languages')}</FormLabel>

              <FormInputGroup
                type={inputTypes.LANGUAGE_SELECT}
                name="languageIds"
                value={languageIds[0] ?? 0}
                values={languageOptions}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('IndexerFlags')}</FormLabel>

              <FormInputGroup
                type={inputTypes.INDEXER_FLAGS_SELECT}
                name="indexerFlags"
                indexerFlags={indexerFlags}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('Edition')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TEXT}
                name="edition"
                value={edition}
                onChange={onInputChange}
              />
            </FormGroup>

            <FormGroup>
              <FormLabel>{translate('ReleaseGroup')}</FormLabel>

              <FormInputGroup
                type={inputTypes.TEXT}
                name="releaseGroup"
                value={releaseGroup}
                onChange={onInputChange}
              />
            </FormGroup>
          </Form>
        )}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <Button kind={kinds.SUCCESS} onPress={onSavePress}>
          {translate('Save')}
        </Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default FileEditModalContent;
