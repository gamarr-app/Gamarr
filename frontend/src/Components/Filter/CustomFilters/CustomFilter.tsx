import { useCallback, useEffect, useRef, useState } from 'react';
import { Error } from 'App/State/AppSectionState';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './CustomFilter.css';

interface CustomFilterProps {
  id: number;
  label: string;
  selectedFilterKey: string | number;
  isDeleting: boolean;
  deleteError?: Error | null;
  dispatchSetFilter: (payload: { selectedFilterKey: string }) => void;
  onEditPress: (id: number) => void;
  dispatchDeleteCustomFilter: (payload: { id: number }) => void;
}

function CustomFilter(props: CustomFilterProps) {
  const {
    id,
    label,
    selectedFilterKey,
    isDeleting: isDeletingProp,
    deleteError,
    dispatchSetFilter,
    onEditPress,
    dispatchDeleteCustomFilter,
  } = props;

  const [isDeleting, setIsDeleting] = useState(false);
  const prevIsDeletingRef = useRef(isDeletingProp);

  // Handle delete error - reset isDeleting state
  useEffect(() => {
    if (
      prevIsDeletingRef.current &&
      !isDeletingProp &&
      isDeleting &&
      deleteError
    ) {
      setIsDeleting(false);
    }
    prevIsDeletingRef.current = isDeletingProp;
  }, [isDeletingProp, isDeleting, deleteError]);

  // Handle unmount - reset filter if this filter was being deleted
  useEffect(() => {
    return () => {
      // Assume that delete and then unmounting means the deletion was successful.
      // Moving this check to an ancestor would be more accurate, but would have
      // more boilerplate.
      if (isDeleting && id === selectedFilterKey) {
        dispatchSetFilter({ selectedFilterKey: 'all' });
      }
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isDeleting, id, selectedFilterKey]);

  const handleEditPress = useCallback(() => {
    onEditPress(id);
  }, [id, onEditPress]);

  const handleRemovePress = useCallback(() => {
    setIsDeleting(true);
    dispatchDeleteCustomFilter({ id });
  }, [id, dispatchDeleteCustomFilter]);

  return (
    <div className={styles.customFilter}>
      <div className={styles.label}>{label}</div>

      <div className={styles.actions}>
        <IconButton name={icons.EDIT} onPress={handleEditPress} />

        <SpinnerIconButton
          title={translate('RemoveFilter')}
          name={icons.REMOVE}
          isSpinning={isDeleting}
          onPress={handleRemovePress}
        />
      </div>
    </div>
  );
}

export default CustomFilter;
