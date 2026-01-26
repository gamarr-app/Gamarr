import _ from 'lodash';
import { HTML5toTouch } from 'rdndmb-html5-to-touch';
import { ElementType, useCallback, useEffect, useRef, useState } from 'react';
import { DndProvider } from 'react-dnd-multi-backend';
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
import Column from 'Components/Table/Column';
import { inputTypes } from 'Helpers/Props';
import { CheckInputChanged, InputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import TableOptionsColumn from './TableOptionsColumn';
import TableOptionsColumnDragPreview from './TableOptionsColumnDragPreview';
import TableOptionsColumnDragSource from './TableOptionsColumnDragSource';
import styles from './TableOptionsModal.css';

interface TableOptionsModalProps {
  isOpen: boolean;
  columns: Column[];
  pageSize?: number;
  maxPageSize?: number;
  canModifyColumns?: boolean;
  optionsComponent?: ElementType;
  onTableOptionChange: (options: TableOptions) => void;
  onModalClose: () => void;
}

interface TableOptions {
  pageSize?: number;
  columns?: Column[];
}

interface DragItem {
  id?: number;
  index: number;
}

function TableOptionsModal(props: TableOptionsModalProps) {
  const {
    isOpen,
    columns,
    pageSize: pageSizeProp,
    maxPageSize = 250,
    canModifyColumns = true,
    optionsComponent: OptionsComponent,
    onTableOptionChange,
    onModalClose,
  } = props;

  const hasPageSize = !!pageSizeProp;
  const [pageSize, setPageSize] = useState(pageSizeProp);
  const [pageSizeError, setPageSizeError] = useState<string | null>(null);
  const [dragIndex, setDragIndex] = useState<number | null>(null);
  const [dropIndex, setDropIndex] = useState<number | null>(null);

  const prevPageSizeProp = useRef(pageSizeProp);

  // Sync pageSize from props when it changes externally
  useEffect(() => {
    if (prevPageSizeProp.current !== pageSizeProp) {
      setPageSize(pageSizeProp);
      prevPageSizeProp.current = pageSizeProp;
    }
  }, [pageSizeProp]);

  const onPageSizeChange = useCallback(
    ({ value }: InputChanged<number | null>) => {
      let error: string | null = null;

      if (value === null || value < 5) {
        error = translate('TablePageSizeMinimum', { minimumValue: '5' });
      } else if (value > maxPageSize) {
        error = translate('TablePageSizeMaximum', {
          maximumValue: `${maxPageSize}`,
        });
      } else {
        onTableOptionChange({ pageSize: value });
      }

      setPageSize(value ?? undefined);
      setPageSizeError(error);
    },
    [maxPageSize, onTableOptionChange]
  );

  const onVisibleChange = useCallback(
    ({ name, value }: CheckInputChanged) => {
      const newColumns = _.cloneDeep(columns);

      const column = _.find(newColumns, { name });
      if (column) {
        column.isVisible = value;
      }

      onTableOptionChange({ columns: newColumns });
    },
    [columns, onTableOptionChange]
  );

  const onColumnDragMove = useCallback(
    (newDragIndex: number, newDropIndex: number) => {
      setDragIndex((prevDragIndex) => {
        setDropIndex((prevDropIndex) => {
          if (
            prevDragIndex !== newDragIndex ||
            prevDropIndex !== newDropIndex
          ) {
            return newDropIndex;
          }
          return prevDropIndex;
        });
        if (prevDragIndex !== newDragIndex) {
          return newDragIndex;
        }
        return prevDragIndex;
      });
    },
    []
  );

  const onColumnDragEnd = useCallback(
    (_item: DragItem, didDrop: boolean) => {
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

  const isDragging = dropIndex !== null;
  const isDraggingUp =
    isDragging && dragIndex !== null && dropIndex < dragIndex;
  const isDraggingDown =
    isDragging && dragIndex !== null && dropIndex > dragIndex;

  return (
    <DndProvider options={HTML5toTouch}>
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
                        pageSizeError ? [{ message: pageSizeError }] : undefined
                      }
                      onChange={onPageSizeChange}
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

                          const displayLabel = columnLabel || label;
                          const stringLabel =
                            typeof displayLabel === 'function'
                              ? displayLabel
                              : String(displayLabel);

                          if (isModifiable !== false) {
                            return (
                              <TableOptionsColumnDragSource
                                key={name}
                                name={name}
                                label={stringLabel}
                                isVisible={isVisible}
                                isModifiable={true}
                                index={index}
                                isDragging={isDragging}
                                isDraggingUp={isDraggingUp}
                                isDraggingDown={isDraggingDown}
                                onVisibleChange={onVisibleChange}
                                onColumnDragMove={onColumnDragMove}
                                onColumnDragEnd={onColumnDragEnd}
                              />
                            );
                          }

                          return (
                            <TableOptionsColumn
                              key={name}
                              name={name}
                              label={stringLabel}
                              isVisible={isVisible}
                              index={index}
                              isModifiable={false}
                              onVisibleChange={onVisibleChange}
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
