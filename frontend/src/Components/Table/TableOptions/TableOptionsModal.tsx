import _ from 'lodash';
import { useCallback, useEffect, useState } from 'react';
import { DndProvider } from 'react-dnd';
import { HTML5Backend } from 'react-dnd-html5-backend';
import { TouchBackend } from 'react-dnd-touch-backend';
import { MultiBackend, TouchTransition } from 'dnd-multi-backend';
import Form from 'Components/Form/Form';
import FormGroup from 'Components/Form/FormGroup';
import FormInputGroup from 'Components/Form/FormInputGroup';
import FormInputHelpText from 'Components/Form/FormInputHelpText';
import FormLabel from 'Components/Form/FormLabel';
import Button from 'Components/Link/Button';
import Modal from 'Components/Modal/Modal';
import ModalBody from 'Components/Modal/ModalBody';
import ModalContent from 'Components/Modal/ModalContent';
import ModalFooter from 'Components/Modal/ModalFooter';
import ModalHeader from 'Components/Modal/ModalHeader';
import { inputTypes } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import TableOptionsColumn from './TableOptionsColumn';
import TableOptionsColumnDragPreview from './TableOptionsColumnDragPreview';
import TableOptionsColumnDragSource from './TableOptionsColumnDragSource';
import styles from './TableOptionsModal.css';

const HTML5toTouch = {
  backends: [
    {
      id: 'html5',
      backend: HTML5Backend,
    },
    {
      id: 'touch',
      backend: TouchBackend,
      options: { enableMouseEvents: true },
      preview: true,
      transition: TouchTransition,
    },
  ],
};

interface Column {
  name: string;
  label: string | (() => string);
  columnLabel?: string;
  isVisible: boolean;
  isModifiable?: boolean;
}

interface TableOptionsModalProps {
  isOpen: boolean;
  columns: Column[];
  pageSize?: number;
  maxPageSize?: number;
  canModifyColumns?: boolean;
  optionsComponent?: React.ComponentType<{
    onTableOptionChange: (options: Record<string, unknown>) => void;
  }>;
  onTableOptionChange: (options: Record<string, unknown>) => void;
  onModalClose: () => void;
}

function TableOptionsModal({
  isOpen,
  columns,
  pageSize: initialPageSize,
  maxPageSize = 250,
  canModifyColumns = true,
  optionsComponent: OptionsComponent,
  onTableOptionChange,
  onModalClose,
}: TableOptionsModalProps) {
  const [pageSize, setPageSize] = useState(initialPageSize);
  const [pageSizeError, setPageSizeError] = useState<string | null>(null);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);

  useEffect(() => {
    if (initialPageSize !== pageSize) {
      setPageSize(initialPageSize);
    }
  }, [initialPageSize]);

  const handlePageSizeChange = useCallback(
    ({ value }: { value: number | null }) => {
      let error: string | null = null;

      if (value === null) {
        return;
      }

      if (value < 5) {
        error = translate('TablePageSizeMinimum', { minimumValue: '5' });
      } else if (value > maxPageSize) {
        error = translate('TablePageSizeMaximum', {
          maximumValue: `${maxPageSize}`,
        });
      } else {
        onTableOptionChange({ pageSize: value });
      }

      setPageSize(value);
      setPageSizeError(error);
    },
    [maxPageSize, onTableOptionChange]
  );

  const handleVisibleChange = useCallback(
    ({ name, value }: { name: string; value: boolean }) => {
      const newColumns = _.cloneDeep(columns);
      const column = _.find(newColumns, { name });

      if (column) {
        column.isVisible = value;
        onTableOptionChange({ columns: newColumns });
      }
    },
    [columns, onTableOptionChange]
  );

  const handleColumnDragMove = useCallback(
    (newDragIndex: number, newDropIndex: number) => {
      if (dragIndex !== newDragIndex || dropIndex !== newDropIndex) {
        setDragIndex(newDragIndex);
        setDropIndex(newDropIndex);
      }
    },
    [dragIndex, dropIndex]
  );

  const handleColumnDragEnd = useCallback(
    (_item: { name: string; index: number }, didDrop: boolean) => {
      if (didDrop && dropIndex !== null && dragIndex !== null) {
        const newColumns = _.cloneDeep(columns);
        const items = newColumns.splice(dragIndex, 1);
        newColumns.splice(dropIndex, 0, items[0]);

        onTableOptionChange({ columns: newColumns });
      }

      setDragIndex(null);
      setDropIndex(null);
    },
    [columns, dragIndex, dropIndex, onTableOptionChange]
  );

  const hasPageSize = initialPageSize !== undefined;
  const isDragging = dropIndex !== null;
  const isDraggingUp = isDragging && dragIndex !== null && dropIndex < dragIndex;
  const isDraggingDown =
    isDragging && dragIndex !== null && dropIndex > dragIndex;

  return (
    <DndProvider backend={MultiBackend} options={HTML5toTouch}>
      <Modal isOpen={isOpen} onModalClose={onModalClose}>
        {isOpen ? (
          <ModalContent onModalClose={onModalClose}>
            <ModalHeader>{translate('TableOptions')}</ModalHeader>

            <ModalBody>
              <Form>
                {hasPageSize ? (
                  <FormGroup>
                    <FormLabel>{translate('TablePageSize')}</FormLabel>

                    <FormInputGroup
                      type={inputTypes.NUMBER}
                      name="pageSize"
                      value={pageSize || 0}
                      helpText={translate('TablePageSizeHelpText')}
                      errors={
                        pageSizeError
                          ? [{ message: pageSizeError }]
                          : undefined
                      }
                      onChange={handlePageSizeChange}
                    />
                  </FormGroup>
                ) : null}

                {OptionsComponent ? (
                  <OptionsComponent onTableOptionChange={onTableOptionChange} />
                ) : null}

                {canModifyColumns ? (
                  <FormGroup>
                    <FormLabel>{translate('TableColumns')}</FormLabel>

                    <div>
                      <FormInputHelpText
                        text={translate('TableColumnsHelpText')}
                      />

                      <div className={styles.columns}>
                        {columns.map((column, index) => {
                          const {
                            name,
                            label,
                            columnLabel,
                            isVisible,
                            isModifiable,
                          } = column;

                          if (isModifiable !== false) {
                            return (
                              <TableOptionsColumnDragSource
                                key={name}
                                name={name}
                                label={columnLabel || label}
                                isVisible={isVisible}
                                isModifiable={true}
                                index={index}
                                isDraggingUp={isDraggingUp}
                                isDraggingDown={isDraggingDown}
                                onVisibleChange={handleVisibleChange}
                                onColumnDragMove={handleColumnDragMove}
                                onColumnDragEnd={handleColumnDragEnd}
                              />
                            );
                          }

                          const displayLabel = columnLabel || label;

                          return (
                            <TableOptionsColumn
                              key={name}
                              name={name}
                              label={
                                typeof displayLabel === 'function'
                                  ? displayLabel()
                                  : displayLabel
                              }
                              isVisible={isVisible}
                              index={index}
                              isModifiable={false}
                              onVisibleChange={handleVisibleChange}
                            />
                          );
                        })}

                        <TableOptionsColumnDragPreview />
                      </div>
                    </div>
                  </FormGroup>
                ) : null}
              </Form>
            </ModalBody>
            <ModalFooter>
              <Button onPress={onModalClose}>{translate('Close')}</Button>
            </ModalFooter>
          </ModalContent>
        ) : null}
      </Modal>
    </DndProvider>
  );
}

export default TableOptionsModal;
