import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import Card from 'Components/Card';
import FieldSet from 'Components/FieldSet';
import Icon from 'Components/Icon';
import PageSectionContent from 'Components/Page/PageSectionContent';
import { icons } from 'Helpers/Props';
import {
  cloneCustomFormat,
  deleteCustomFormat,
  fetchCustomFormats,
} from 'Store/Actions/settingsActions';
import createSortedSectionSelector from 'Store/Selectors/createSortedSectionSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import CustomFormat from './CustomFormat';
import EditCustomFormatModal from './EditCustomFormatModal';
import styles from './CustomFormats.css';

function CustomFormats() {
  const dispatch = useDispatch();
  const { isFetching, isPopulated, error, items, isDeleting } = useSelector(
    createSortedSectionSelector(
      'settings.customFormats',
      sortByProp('name') as (a: any, b: any) => number
    )
  ) as any;

  const [isCustomFormatModalOpen, setIsCustomFormatModalOpen] = useState(false);
  const [tagsFromId, setTagsFromId] = useState<number | undefined>(undefined);

  useEffect(() => {
    dispatch(fetchCustomFormats());
  }, [dispatch]);

  const handleConfirmDeleteCustomFormat = useCallback(
    (id: number) => {
      dispatch(deleteCustomFormat({ id }));
    },
    [dispatch]
  );

  const handleCloneCustomFormatPress = useCallback(
    (id: number) => {
      dispatch(cloneCustomFormat({ id }));
      setIsCustomFormatModalOpen(true);
      setTagsFromId(id);
    },
    [dispatch]
  );

  const handleEditCustomFormatPress = useCallback(() => {
    setIsCustomFormatModalOpen(true);
  }, []);

  const handleModalClose = useCallback(() => {
    setIsCustomFormatModalOpen(false);
    setTagsFromId(undefined);
  }, []);

  return (
    <FieldSet legend={translate('CustomFormats')}>
      <PageSectionContent
        errorMessage={translate('CustomFormatsLoadError')}
        isFetching={isFetching}
        isPopulated={isPopulated}
        error={error}
      >
        <div className={styles.customFormats}>
          {items.map((item: { id: number }) => {
            return (
              <CustomFormat
                key={item.id}
                {...item}
                isDeleting={isDeleting}
                onConfirmDeleteCustomFormat={handleConfirmDeleteCustomFormat}
                onCloneCustomFormatPress={handleCloneCustomFormatPress}
              />
            );
          })}

          <Card
            className={styles.addCustomFormat}
            onPress={handleEditCustomFormatPress}
          >
            <div className={styles.center}>
              <Icon name={icons.ADD} size={45} />
            </div>
          </Card>
        </div>

        <EditCustomFormatModal
          isOpen={isCustomFormatModalOpen}
          tagsFromId={tagsFromId}
          onModalClose={handleModalClose}
        />
      </PageSectionContent>
    </FieldSet>
  );
}

export default CustomFormats;
