import React from 'react';
import Alert from 'Components/Alert';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormLabel from 'Components/Form/FormLabel';
import ProviderFieldFormGroup from 'Components/Form/ProviderFieldFormGroup';
import Button from 'Components/Link/Button';
import SpinnerErrorButton from 'Components/Link/SpinnerErrorButton';
import InlineMarkdown from 'Components/Markdown/InlineMarkdown';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes, kinds } from 'Helpers/Props';
import { ModelBaseSetting } from 'Store/Selectors/selectSettings';
import { InputChanged } from 'typings/inputs';
import { PendingField, PendingSection } from 'typings/pending';
import translate from 'Utilities/String/translate';
import styles from './EditSpecificationModalContent.css';

interface EditSpecificationModalContentProps {
  advancedSettings: boolean;
  item: PendingSection<ModelBaseSetting>;
  onInputChange: (change: InputChanged) => void;
  onFieldChange: (change: InputChanged) => void;
  onCancelPress: () => void;
  onSavePress: () => void;
  onDeleteSpecificationPress?: () => void;
}

function EditSpecificationModalContent(
  props: EditSpecificationModalContentProps
) {
  const {
    advancedSettings,
    item,
    onInputChange,
    onFieldChange,
    onCancelPress,
    onSavePress,
    onDeleteSpecificationPress,
    ...otherProps
  } = props;

  const { id, implementationName, name, negate, required, fields } = item;

  const idValue = id?.value;
  const implementationNameValue = implementationName ?? '';

  return (
    <ModalContent onModalClose={onCancelPress}>
      <ModalHeader>
        {idValue
          ? translate('EditConditionImplementation', {
              implementationName: implementationNameValue,
            })
          : translate('AddConditionImplementation', {
              implementationName: implementationNameValue,
            })}
      </ModalHeader>

      <ModalBody>
        <Form {...otherProps}>
          {fields &&
            fields.some(
              (x: PendingField<unknown>) =>
                x.label ===
                translate('CustomFormatsSpecificationRegularExpression')
            ) && (
              <Alert kind={kinds.INFO}>
                <div>
                  <InlineMarkdown
                    data={translate('ConditionUsingRegularExpressions')}
                  />
                </div>
                <div>
                  <InlineMarkdown
                    data={translate('RegularExpressionsTutorialLink', {
                      url: 'https://www.regular-expressions.info/tutorial.html',
                    })}
                  />
                </div>
                <div>
                  <InlineMarkdown
                    data={translate('RegularExpressionsCanBeTested', {
                      url: 'http://regexstorm.net/tester',
                    })}
                  />
                </div>
              </Alert>
            )}

          <FormGroup>
            <FormLabel>{translate('Name')}</FormLabel>

            <FormInputGroup
              type={inputTypes.TEXT}
              name="name"
              {...name}
              onChange={onInputChange}
            />
          </FormGroup>

          {fields &&
            fields.map((field: PendingField<unknown>) => {
              return (
                <ProviderFieldFormGroup
                  key={field.name}
                  advancedSettings={advancedSettings}
                  provider="specifications"
                  providerData={item}
                  {...field}
                  onChange={onFieldChange}
                />
              );
            })}

          <FormGroup>
            <FormLabel>{translate('Negate')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="negate"
              {...negate}
              helpText={translate('NegateHelpText', {
                implementationName: implementationNameValue,
              })}
              onChange={onInputChange}
            />
          </FormGroup>

          <FormGroup>
            <FormLabel>{translate('Required')}</FormLabel>

            <FormInputGroup
              type={inputTypes.CHECK}
              name="required"
              {...required}
              helpText={translate('RequiredHelpText', {
                implementationName: implementationNameValue,
              })}
              onChange={onInputChange}
            />
          </FormGroup>
        </Form>
      </ModalBody>
      <ModalFooter>
        {idValue && (
          <Button
            className={styles.deleteButton}
            kind={kinds.DANGER}
            onPress={onDeleteSpecificationPress}
          >
            {translate('Delete')}
          </Button>
        )}

        <Button onPress={onCancelPress}>{translate('Cancel')}</Button>

        <SpinnerErrorButton isSpinning={false} onPress={onSavePress}>
          {translate('Save')}
        </SpinnerErrorButton>
      </ModalFooter>
    </ModalContent>
  );
}

export default EditSpecificationModalContent;
