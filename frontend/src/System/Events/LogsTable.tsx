import React from 'react';
import Alert from 'Components/Alert';
import LoadingIndicator from 'Components/Loading/LoadingIndicator';
import FilterMenu from 'Components/Menu/FilterMenu';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import PageToolbar from 'Components/Page/Toolbar/PageToolbar';
import PageToolbarButton from 'Components/Page/Toolbar/PageToolbarButton';
import PageToolbarSection from 'Components/Page/Toolbar/PageToolbarSection';
import Column from 'Components/Table/Column';
import Table from 'Components/Table/Table';
import TableBody from 'Components/Table/TableBody';
import TableOptionsModalWrapper from 'Components/Table/TableOptions/TableOptionsModalWrapper';
import TablePager from 'Components/Table/TablePager';
import { align, icons, kinds } from 'Helpers/Props';
import { SortDirection } from 'Helpers/Props/sortDirections';
import translate from 'Utilities/String/translate';
import LogsTableRow from './LogsTableRow';
import styles from './LogsTable.css';

interface LogFilter {
  key: string;
  label: string | (() => string);
  filters: { key: string; value: string; type: string }[];
}

export interface LogItem {
  id: number;
  level: string;
  time: string;
  logger: string;
  message: string;
  exception?: string;
}

interface LogsTableProps {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  items: LogItem[];
  columns: Column[];
  selectedFilterKey: string;
  filters: LogFilter[];
  totalRecords?: number;
  clearLogExecuting: boolean;
  onFilterSelect: (key: string | number) => void;
  onRefreshPress: () => void;
  onClearLogsPress: () => void;
  onFirstPagePress: () => void;
  onPreviousPagePress: () => void;
  onNextPagePress: () => void;
  onLastPagePress: () => void;
  onPageSelect: (page: number) => void;
  onSortPress: (sortKey: string) => void;
  onTableOptionChange: (payload: object) => void;
  page?: number;
  pageSize?: number;
  totalPages?: number;
  sortKey?: string;
  sortDirection?: SortDirection;
}

function LogsTable(props: LogsTableProps) {
  const {
    isFetching,
    isPopulated,
    error,
    items,
    columns,
    selectedFilterKey,
    filters,
    totalRecords,
    clearLogExecuting,
    onRefreshPress,
    onClearLogsPress,
    onFilterSelect,
    ...otherProps
  } = props;

  return (
    <PageContent className={styles.logsTable} title={translate('Logs')}>
      <PageToolbar>
        <PageToolbarSection>
          <PageToolbarButton
            label={translate('Refresh')}
            iconName={icons.REFRESH}
            spinningName={icons.REFRESH}
            isSpinning={isFetching}
            onPress={onRefreshPress}
          />

          <PageToolbarButton
            label={translate('Clear')}
            iconName={icons.CLEAR}
            isSpinning={clearLogExecuting}
            onPress={onClearLogsPress}
          />
        </PageToolbarSection>

        <PageToolbarSection alignContent={align.RIGHT}>
          <TableOptionsModalWrapper
            {...otherProps}
            columns={columns}
            canModifyColumns={false}
          >
            <PageToolbarButton
              label={translate('Options')}
              iconName={icons.TABLE}
            />
          </TableOptionsModalWrapper>

          <FilterMenu
            alignMenu={align.RIGHT}
            selectedFilterKey={selectedFilterKey}
            filters={filters}
            customFilters={[]}
            onFilterSelect={onFilterSelect}
          />
        </PageToolbarSection>
      </PageToolbar>

      <PageContentBody>
        {isFetching && !isPopulated && <LoadingIndicator />}

        {isPopulated && !error && !items.length && (
          <Alert kind={kinds.INFO}>{translate('NoEventsFound')}</Alert>
        )}

        {isPopulated && !error && !!items.length && (
          <div>
            <Table columns={columns} canModifyColumns={false} {...otherProps}>
              <TableBody>
                {items.map((item) => {
                  return (
                    <LogsTableRow key={item.id} columns={columns} {...item} />
                  );
                })}
              </TableBody>
            </Table>

            <TablePager
              totalRecords={totalRecords}
              isFetching={isFetching}
              {...otherProps}
            />
          </div>
        )}
      </PageContentBody>
    </PageContent>
  );
}

export default LogsTable;
