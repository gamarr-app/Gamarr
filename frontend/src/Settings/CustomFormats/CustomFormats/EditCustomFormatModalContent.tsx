import React, { useCallback, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import Alert from 'Components/Alert';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import Icon from 'Components/Icon';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { icons, inputTypes, kinds } from 'Helpers/Props';
import CustomFormat from 'typings/CustomFormat';
import { InputChanged } from 'typings/inputs';
import { PendingSection } from 'typings/pending';
import translate from 'Utilities/String/translate';
import ImportCustomFormatModal from './ImportCustomFormatModal';
import AddSpecificationModal from './Specifications/AddSpecificationModal';
import EditSpecificationModal from './Specifications/EditSpecificationModal';
import Specification from './Specifications/Specification';
import styles from './EditCustomFormatModalContent.css';

interface SpecificationItem {
  id: number;
  name: string;
  implementation: string;
  implementationName: string;
  negate: boolean;
  required: boolean;
  fields: object[];
}

interface EditCustomFormatModalContentProps {
  id?: number;
  isFetching: boolean;
  error?: Error | null;
  isSaving: boolean;
  saveError?: Error | string;
  item: PendingSection<CustomFormat>;
  specificationsPopulated: boolean;
  specifications: SpecificationItem[];
  advancedSettings?: boolean;
  onInputChange: (change: InputChanged) => void;
  onSavePress: () => void;
  onContentHeightChange: (height: number) => void;
  onModalClose: () => void;
  onDeleteCustomFormatPress?: () => void;
  onCloneSpecificationPress: (id: number) => void;
  onConfirmDeleteSpecification: (id: number) => void;
}

function EditCustomFormatModalContent({
  id,
  isFetching,
  error,
  isSaving,
  saveError,
  item,
  specificationsPopulated,
  specifications,
  onInputChange,
  onSavePress,
  onModalClose,
  onDeleteCustomFormatPress,
  onCloneSpecificationPress,
  onConfirmDeleteSpecification,
}: EditCustomFormatModalContentProps) {
  const [isAddSpecificationModalOpen, setIsAddSpecificationModalOpen] =
    useState(false);
  const [isEditSpecificationModalOpen, setIsEditSpecificationModalOpen] =
    useState(false);
  const [isImportCustomFormatModalOpen, setIsImportCustomFormatModalOpen] =
    useState(false);

  const { name, includeCustomFormatWhenRenaming } = item;

  const handleAddSpecificationPress = useCallback(() => {
    setIsAddSpecificationModalOpen(true);
  }, []);

  const handleAddSpecificationModalClose = useCallback(
    ({ specificationSelected = false } = {}) => {
      setIsAddSpecificationModalOpen(false);
      setIsEditSpecificationModalOpen(specificationSelected);
    },
    []
  );

  const handleEditSpecificationModalClose = useCallback(() => {
    setIsEditSpecificationModalOpen(false);
  }, []);

  const handleImportPress = useCallback(() => {
    setIsImportCustomFormatModalOpen(true);
  }, []);

  const handleImportCustomFormatModalClose = useCallback(() => {
    setIsImportCustomFormatModalOpen(false);
  }, []);

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>
        {id ? translate('EditCustomFormat') : translate('AddCustomFormat')}
      </ModalHeader>

      <ModalBody>
        <div>
          {isFetching && <LoadingIndicator />}

          {!isFetching && !!error && (
            <Alert kind={kinds.DANGER}>
              {translate('AddCustomFormatError')}
            </Alert>
          )}

          {!isFetching && !error && specificationsPopulated && (
            <div>
              <Form>
                <FormGroup>
                  <FormLabel>{translate('Name')}</FormLabel>

                  <FormInputGroup
                    type={inputTypes.TEXT}
                    name="name"
                    {...name}
                    onChange={onInputChange}
                  />
                </FormGroup>

                <FormGroup>
                  <FormLabel>
                    {translate('IncludeCustomFormatWhenRenaming')}
                  </FormLabel>

                  <FormInputGroup
                    type={inputTypes.CHECK}
                    name="includeCustomFormatWhenRenaming"
                    helpText={translate(
                      'IncludeCustomFormatWhenRenamingHelpText'
                    )}
                    {...includeCustomFormatWhenRenaming}
                    onChange={onInputChange}
                  />
                </FormGroup>
              </Form>

              <FieldSet legend={translate('Conditions')}>
                <Alert kind={kinds.INFO}>
                  <div>{translate('CustomFormatsSettingsTriggerInfo')}</div>
                </Alert>
                <div className={styles.customFormats}>
                  {specifications.map((tag) => {
                    return (
                      <Specification
                        key={tag.id}
                        {...tag}
                        onCloneSpecificationPress={onCloneSpecificationPress}
                        onConfirmDeleteSpecification={
                          onConfirmDeleteSpecification
                        }
                      />
                    );
                  })}

                  <Card
                    className={styles.addSpecification}
                    onPress={handleAddSpecificationPress}
                  >
                    <div className={styles.center}>
                      <Icon name={icons.ADD} size={45} />
                    </div>
                  </Card>
                </div>
              </FieldSet>

              <AddSpecificationModal
                isOpen={isAddSpecificationModalOpen}
                onModalClose={handleAddSpecificationModalClose}
              />

              <EditSpecificationModal
                isOpen={isEditSpecificationModalOpen}
                onModalClose={handleEditSpecificationModalClose}
              />

              <ImportCustomFormatModal
                isOpen={isImportCustomFormatModalOpen}
                onModalClose={handleImportCustomFormatModalClose}
              />
            </div>
          )}
        </div>
      </ModalBody>
      <ModalFooter>
        <div className={styles.rightButtons}>
          {id && (
            <Button
              className={styles.deleteButton}
              kind={kinds.DANGER}
              onPress={onDeleteCustomFormatPress}
            >
              {translate('Delete')}
            </Button>
          )}

          <Button className={styles.deleteButton} onPress={handleImportPress}>
            {translate('Import')}
          </Button>
        </div>

        <Button onPress={onModalClose}>{translate('Cancel')}</Button>

        <SpinnerErrorButton
          isSpinning={isSaving}
          error={saveError}
          onPress={onSavePress}
        >
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditCustomFormatModalContent;
