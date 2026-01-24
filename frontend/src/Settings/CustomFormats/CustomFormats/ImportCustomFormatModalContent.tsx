import React, { useCallback, useEffect, useRef, useState } from 'react';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds, sizes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './ImportCustomFormatModalContent.css';

interface ParseError {
  message: string;
  detailedMessage?: string;
}

interface ImportCustomFormatModalContentProps {
  isFetching: boolean;
  error?: Error;
  specificationsPopulated: boolean;
  onImportPress: (json: string) => ParseError | null;
  onModalClose: () => void;
}

function ImportCustomFormatModalContent({
  isFetching,
  error,
  specificationsPopulated,
  onImportPress,
  onModalClose,
}: ImportCustomFormatModalContentProps) {
  const [json, setJson] = useState('');
  const [isSpinning, setIsSpinning] = useState(false);
  const [parseError, setParseError] = useState<ParseError | null>(null);
  const importTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(() => {
    return () => {
      if (importTimeoutRef.current) {
        clearTimeout(importTimeoutRef.current);
      }
    };
  }, []);

  const handleChange = useCallback((event: { value: string }) => {
    setJson(event.value);
  }, []);

  const doImport = useCallback(() => {
    const result = onImportPress(json);
    setParseError(result);
    setIsSpinning(false);

    if (!result) {
      onModalClose();
    }
  }, [json, onImportPress, onModalClose]);

  const handleImportPress = useCallback(() => {
    setIsSpinning(true);
    importTimeoutRef.current = setTimeout(doImport, 250);
  }, [doImport]);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('ImportCustomFormat')}</ModalHeader>

      <ModalBody>
        <div>
          {isFetching && <LoadingIndicator />}

          {!isFetching && !!error && (
            <Alert kind={kinds.DANGER}>
              {translate('CustomFormatsLoadError')}
            </Alert>
          )}

          {!isFetching && !error && specificationsPopulated && (
            <Form>
              <FormGroup size={sizes.MEDIUM}>
                <FormLabel>{translate('CustomFormatJson')}</FormLabel>
                <FormInputGroup
                  key={0}
                  inputClassName={styles.input}
                  type={inputTypes.TEXT_AREA}
                  name="customFormatJson"
                  value={json}
                  onChange={handleChange}
                  placeholder={'{\n  "name": "Custom Format"\n}'}
                  errors={parseError ? [parseError as any] : []}
                />
              </FormGroup>
            </Form>
          )}
        </div>
      </ModalBody>
      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Cancel')}</Button>
        <SpinnerErrorButton
          onPress={handleImportPress}
          isSpinning={isSpinning}
          error={parseError ? parseError.message : undefined}
        >
          {translate('Import')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default ImportCustomFormatModalContent;
