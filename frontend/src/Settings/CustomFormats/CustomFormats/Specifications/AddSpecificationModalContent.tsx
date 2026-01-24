/* eslint-disable @typescript-eslint/no-explicit-any, react/jsx-no-bind */
import React, { useCallback, useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Alert from 'Components/Alert';
import Button from 'Components/Link/Button';
import Link from 'Components/Link/Link';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { kinds } from 'Helpers/Props';
import {
  fetchCustomFormatSpecificationSchema,
  selectCustomFormatSpecificationSchema,
} from 'Store/Actions/settingsActions';
import translate from 'Utilities/String/translate';
import AddSpecificationItem from './AddSpecificationItem';
import styles from './AddSpecificationModalContent.css';

interface AddSpecificationModalContentProps {
  onModalClose: (options?: { specificationSelected?: boolean }) => void;
}

function AddSpecificationModalContent({
  onModalClose,
}: AddSpecificationModalContentProps) {
  const dispatch = useDispatch();

  const { isSchemaFetching, isSchemaPopulated, schemaError, schema } =
    useSelector(
      (state: {
        settings: {
          customFormatSpecifications: {
            isSchemaFetching: boolean;
            isSchemaPopulated: boolean;
            schemaError: object | null;
            schema: any[];
          };
        };
      }) => state.settings.customFormatSpecifications
    );

  useEffect(() => {
    dispatch(fetchCustomFormatSpecificationSchema());
  }, [dispatch]);

  const handleSpecificationSelect = useCallback(
    ({ implementation, name }: { implementation: string; name?: string }) => {
      dispatch(
        selectCustomFormatSpecificationSchema({
          implementation,
          presetName: name,
        })
      );
      onModalClose({ specificationSelected: true });
    },
    [dispatch, onModalClose]
  );

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>Add Condition</ModalHeader>

      <ModalBody>
        {isSchemaFetching ? <LoadingIndicator /> : null}

        {!isSchemaFetching && !!schemaError ? (
          <Alert kind={kinds.DANGER}>{translate('AddConditionError')}</Alert>
        ) : null}

        {isSchemaPopulated && !schemaError ? (
          <div>
            <Alert kind={kinds.INFO}>
              <div>{translate('SupportedCustomConditions')}</div>
              <div>
                {translate('VisitTheWikiForMoreDetails')}
                <Link to="https://wiki.servarr.com/gamarr/settings#custom-formats-2">
                  {translate('Wiki')}
                </Link>
              </div>
            </Alert>

            <div className={styles.specifications}>
              {schema.map((specification: any) => {
                return (
                  <AddSpecificationItem
                    key={specification.implementation}
                    {...specification}
                    onSpecificationSelect={handleSpecificationSelect}
                  />
                );
              })}
            </div>
          </div>
        ) : null}
      </ModalBody>
      <ModalFooter>
        <Button onPress={() => onModalClose()}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default AddSpecificationModalContent;
