import { useCallback, useState } from 'react';
import Icon from 'Components/Icon';
import IconButton from 'Components/Link/IconButton';
import Column from 'Components/Table/Column';
import TableOptionsModal from 'Components/Table/TableOptions/TableOptionsModal';
import VirtualTableHeader from 'Components/Table/VirtualTableHeader';
import VirtualTableHeaderCell from 'Components/Table/VirtualTableHeaderCell';
import VirtualTableSelectAllHeaderCell from 'Components/Table/VirtualTableSelectAllHeaderCell';
import { icons } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { CheckInputChanged } from 'typings/inputs';
import translate from 'Utilities/String/translate';
import DiscoverGameTableOptionsConnector from './DiscoverGameTableOptionsConnector';
import styles from './DiscoverGameHeader.css';

interface TableOptions {
  pageSize?: number;
  columns?: Column[];
}

interface DiscoverGameHeaderProps {
  columns: Column[];
  sortKey?: string;
  sortDirection?: SortDirection;
  onTableOptionChange: (payload: TableOptions) => void;
  onSortPress: (sortKey: string) => void;
  allSelected: boolean;
  allUnselected: boolean;
  onSelectAllChange: (change: CheckInputChanged) => void;
}

function DiscoverGameHeader({
  columns,
  sortKey,
  sortDirection,
  onTableOptionChange,
  onSortPress,
  allSelected,
  allUnselected,
  onSelectAllChange,
}: DiscoverGameHeaderProps) {
  const [isTableOptionsModalOpen, setIsTableOptionsModalOpen] = useState(false);

  const handleTableOptionsPress = useCallback(() => {
    setIsTableOptionsModalOpen(true);
  }, []);

  const handleTableOptionsModalClose = useCallback(() => {
    setIsTableOptionsModalOpen(false);
  }, []);

  return (
    <VirtualTableHeader>
      <VirtualTableSelectAllHeaderCell
        allSelected={allSelected}
        allUnselected={allUnselected}
        onSelectAllChange={onSelectAllChange}
      />

      {columns.map((column) => {
        const { name, label, isSortable, isVisible } = column;

        if (!isVisible) {
          return null;
        }

        if (name === 'actions') {
          return (
            <VirtualTableHeaderCell
              key={name}
              className={styles[name as keyof typeof styles]}
              name={name}
              isSortable={false}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
            >
              <IconButton
                name={icons.ADVANCED_SETTINGS}
                onPress={handleTableOptionsPress}
              />
            </VirtualTableHeaderCell>
          );
        }

        if (name === 'isRecommendation') {
          return (
            <VirtualTableHeaderCell
              key={name}
              className={styles[name as keyof typeof styles]}
              name={name}
              isSortable={true}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
            >
              <Icon
                name={icons.RECOMMENDED}
                size={12}
                title={translate('Recommendation')}
              />
            </VirtualTableHeaderCell>
          );
        }

        if (name === 'isTrending') {
          return (
            <VirtualTableHeaderCell
              key={name}
              className={styles[name as keyof typeof styles]}
              name={name}
              isSortable={true}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
            >
              <Icon
                name={icons.TRENDING}
                size={12}
                title={translate('Trending')}
              />
            </VirtualTableHeaderCell>
          );
        }

        if (name === 'isPopular') {
          return (
            <VirtualTableHeaderCell
              key={name}
              className={styles[name as keyof typeof styles]}
              name={name}
              isSortable={true}
              sortKey={sortKey}
              sortDirection={sortDirection}
              onSortPress={onSortPress}
            >
              <Icon
                name={icons.POPULAR}
                size={12}
                title={translate('Popular')}
              />
            </VirtualTableHeaderCell>
          );
        }

        return (
          <VirtualTableHeaderCell
            key={name}
            className={styles[name as keyof typeof styles]}
            name={name}
            isSortable={isSortable}
            sortKey={sortKey}
            sortDirection={sortDirection}
            onSortPress={onSortPress}
          >
            {typeof label === 'function' ? label() : label}
          </VirtualTableHeaderCell>
        );
      })}

      <TableOptionsModal
        isOpen={isTableOptionsModalOpen}
        columns={columns}
        optionsComponent={
          DiscoverGameTableOptionsConnector as React.ComponentType<{
            onTableOptionChange: (options: TableOptions) => void;
          }>
        }
        onTableOptionChange={onTableOptionChange}
        onModalClose={handleTableOptionsModalClose}
      />
    </VirtualTableHeader>
  );
}

export default DiscoverGameHeader;
