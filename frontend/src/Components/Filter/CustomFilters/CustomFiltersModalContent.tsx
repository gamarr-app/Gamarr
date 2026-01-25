import { Error } from 'App/State/AppSectionState';
import { CustomFilter as CustomFilterType } from 'App/State/AppState';
import Button from 'Components/Link/Button';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import sortByProp from 'Utilities/Array/sortByProp';
import translate from 'Utilities/String/translate';
import CustomFilter from './CustomFilter';
import styles from './CustomFiltersModalContent.css';

interface CustomFiltersModalContentProps {
  selectedFilterKey: string | number;
  customFilters: CustomFilterType[];
  isDeleting: boolean;
  deleteError?: Error | null;
  dispatchDeleteCustomFilter: (payload: { id: number }) => void;
  dispatchSetFilter: (payload: { selectedFilterKey: string }) => void;
  onAddCustomFilter: () => void;
  onEditCustomFilter: (id: number) => void;
  onModalClose: () => void;
}

function CustomFiltersModalContent(props: CustomFiltersModalContentProps) {
  const {
    selectedFilterKey,
    customFilters,
    isDeleting,
    deleteError,
    dispatchDeleteCustomFilter,
    dispatchSetFilter,
    onAddCustomFilter,
    onEditCustomFilter,
    onModalClose,
  } = props;

  return (
    <ModalContent onModalClose={onModalClose}>
      <ModalHeader>{translate('CustomFilters')}</ModalHeader>

      <ModalBody>
        {[...customFilters].sort(sortByProp('label')).map((customFilter) => {
          return (
            <CustomFilter
              key={customFilter.id}
              id={customFilter.id}
              label={customFilter.label}
              selectedFilterKey={selectedFilterKey}
              isDeleting={isDeleting}
              deleteError={deleteError}
              dispatchSetFilter={dispatchSetFilter}
              dispatchDeleteCustomFilter={dispatchDeleteCustomFilter}
              onEditPress={onEditCustomFilter}
            />
          );
        })}

        <div className={styles.addButtonContainer}>
          <Button onPress={onAddCustomFilter}>
            {translate('AddCustomFilter')}
          </Button>
        </div>
        <br />
        {translate('FilterGamePropertiesOnlyNotFileWarning')}
      </ModalBody>

      <ModalFooter>
        <Button onPress={onModalClose}>{translate('Close')}</Button>
      </ModalFooter>
    </ModalContent>
  );
}

export default CustomFiltersModalContent;
