import _ from 'lodash';
import { HTML5toTouch } from 'rdndmb-html5-to-touch';
import { Component, ElementType } from 'react';
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
  canModifyColumns: boolean;
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

interface TableOptionsModalState {
  hasPageSize: boolean;
  pageSize?: number;
  pageSizeError: string | null;
  dragIndex: number | null;
  dropIndex: number | null;
}

class TableOptionsModal extends Component<
  TableOptionsModalProps,
  TableOptionsModalState
> {
  static defaultProps = {
    canModifyColumns: true,
  };

  //
  // Lifecycle

  constructor(props: TableOptionsModalProps) {
    super(props);

    this.state = {
      hasPageSize: !!props.pageSize,
      pageSize: props.pageSize,
      pageSizeError: null,
      dragIndex: null,
      dropIndex: null,
    };
  }

  componentDidUpdate(prevProps: TableOptionsModalProps) {
    if (prevProps.pageSize !== this.state.pageSize) {
      this.setState({ pageSize: this.props.pageSize });
    }
  }

  //
  // Listeners

  onPageSizeChange = ({ value }: InputChanged<number | null>) => {
    let pageSizeError: string | null = null;
    const maxPageSize = this.props.maxPageSize ?? 250;

    if (value === null || value < 5) {
      pageSizeError = translate('TablePageSizeMinimum', { minimumValue: '5' });
    } else if (value > maxPageSize) {
      pageSizeError = translate('TablePageSizeMaximum', {
        maximumValue: `${maxPageSize}`,
      });
    } else {
      this.props.onTableOptionChange({ pageSize: value });
    }

    this.setState({
      pageSize: value ?? undefined,
      pageSizeError,
    });
  };

  onVisibleChange = ({ name, value }: CheckInputChanged) => {
    const columns = _.cloneDeep(this.props.columns);

    const column = _.find(columns, { name });
    if (column) {
      column.isVisible = value;
    }

    this.props.onTableOptionChange({ columns });
  };

  onColumnDragMove = (dragIndex: number, dropIndex: number) => {
    if (
      this.state.dragIndex !== dragIndex ||
      this.state.dropIndex !== dropIndex
    ) {
      this.setState({
        dragIndex,
        dropIndex,
      });
    }
  };

  onColumnDragEnd = (_item: DragItem, didDrop: boolean) => {
    const { dragIndex, dropIndex } = this.state;

    if (didDrop && dropIndex !== null && dragIndex !== null) {
      const columns = _.cloneDeep(this.props.columns);
      const items = columns.splice(dragIndex, 1);
      columns.splice(dropIndex, 0, items[0]);

      this.props.onTableOptionChange({ columns });
    }

    this.setState({
      dragIndex: null,
      dropIndex: null,
    });
  };

  //
  // Render

  render() {
    const {
      isOpen,
      columns,
      canModifyColumns,
      optionsComponent: OptionsComponent,
      onTableOptionChange,
      onModalClose,
    } = this.props;

    const { hasPageSize, pageSize, pageSizeError, dragIndex, dropIndex } =
      this.state;

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
                          pageSizeError
                            ? [{ message: pageSizeError }]
                            : undefined
                        }
                        onChange={this.onPageSizeChange}
                      />
                    </FormGroup>
                  ) : null}

                  {OptionsComponent ? (
                    <OptionsComponent
                      onTableOptionChange={onTableOptionChange}
                    />
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
                                  onVisibleChange={this.onVisibleChange}
                                  onColumnDragMove={this.onColumnDragMove}
                                  onColumnDragEnd={this.onColumnDragEnd}
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
                                onVisibleChange={this.onVisibleChange}
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
}

export default TableOptionsModal;
