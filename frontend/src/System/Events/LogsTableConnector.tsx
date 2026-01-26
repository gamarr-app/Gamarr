import { useCallback, useEffect, useRef } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { LogItem } from 'App/State/SystemAppState';
import * as commandNames from 'Commands/commandNames';
import withCurrentPage from 'Components/withCurrentPage';
import { SortDirection } from 'Helpers/Props/sortDirections';
import { executeCommand } from 'Store/Actions/commandActions';
import {
  fetchLogs,
  gotoLogsFirstPage,
  gotoLogsLastPage,
  gotoLogsNextPage,
  gotoLogsPage,
  gotoLogsPreviousPage,
  setLogsFilter,
  setLogsSort,
  setLogsTableOption,
} from 'Store/Actions/systemActions';
import createCommandExecutingSelector from 'Store/Selectors/createCommandExecutingSelector';
import LogsTable from './LogsTable';

const selectLogsState = createSelector(
  (state: AppState) => state.system.logs,
  createCommandExecutingSelector(commandNames.CLEAR_LOGS),
  (logs, clearLogExecuting) => {
    return {
      clearLogExecuting,
      ...logs,
      items: logs.items as LogItem[],
    };
  }
);

interface LogsTableConnectorProps {
  useCurrentPage: boolean;
}

function LogsTableConnector(props: LogsTableConnectorProps) {
  const { useCurrentPage } = props;
  const dispatch = useDispatch();

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
    page,
    pageSize,
    totalPages,
    sortKey,
    sortDirection,
  } = useSelector(selectLogsState);

  const prevClearLogExecutingRef = useRef(clearLogExecuting);

  const dispatchGotoLogsFirstPage = useCallback(() => {
    dispatch(gotoLogsFirstPage());
  }, [dispatch]);

  // Fetch on mount
  useEffect(() => {
    if (useCurrentPage) {
      dispatch(fetchLogs());
    } else {
      dispatchGotoLogsFirstPage();
    }
  }, [dispatch, useCurrentPage, dispatchGotoLogsFirstPage]);

  // Refresh when clear logs finishes
  useEffect(() => {
    if (prevClearLogExecutingRef.current && !clearLogExecuting) {
      dispatchGotoLogsFirstPage();
    }
    prevClearLogExecutingRef.current = clearLogExecuting;
  }, [clearLogExecuting, dispatchGotoLogsFirstPage]);

  const onFirstPagePress = useCallback(() => {
    dispatch(gotoLogsFirstPage());
  }, [dispatch]);

  const onPreviousPagePress = useCallback(() => {
    dispatch(gotoLogsPreviousPage());
  }, [dispatch]);

  const onNextPagePress = useCallback(() => {
    dispatch(gotoLogsNextPage());
  }, [dispatch]);

  const onLastPagePress = useCallback(() => {
    dispatch(gotoLogsLastPage());
  }, [dispatch]);

  const onPageSelect = useCallback(
    (pageNum: number) => {
      dispatch(gotoLogsPage({ page: pageNum }));
    },
    [dispatch]
  );

  const onSortPress = useCallback(
    (sortKeyValue: string) => {
      dispatch(setLogsSort({ sortKey: sortKeyValue }));
    },
    [dispatch]
  );

  const onFilterSelect = useCallback(
    (selectedFilterKeyValue: string | number) => {
      dispatch(
        setLogsFilter({ selectedFilterKey: String(selectedFilterKeyValue) })
      );
    },
    [dispatch]
  );

  const onTableOptionChange = useCallback(
    (payload: { pageSize?: number }) => {
      dispatch(setLogsTableOption(payload));

      if (payload.pageSize) {
        dispatch(gotoLogsFirstPage());
      }
    },
    [dispatch]
  );

  const onRefreshPress = useCallback(() => {
    dispatch(gotoLogsFirstPage());
  }, [dispatch]);

  const onClearLogsPress = useCallback(() => {
    dispatch(
      executeCommand({
        name: commandNames.CLEAR_LOGS,
        commandFinished: () => {
          dispatch(gotoLogsFirstPage());
        },
      })
    );
  }, [dispatch]);

  return (
    <LogsTable
      isFetching={isFetching}
      isPopulated={isPopulated}
      error={error}
      items={items}
      columns={columns}
      selectedFilterKey={selectedFilterKey}
      filters={filters}
      totalRecords={totalRecords}
      clearLogExecuting={clearLogExecuting}
      page={page}
      pageSize={pageSize}
      totalPages={totalPages}
      sortKey={sortKey}
      sortDirection={sortDirection as SortDirection}
      onFirstPagePress={onFirstPagePress}
      onPreviousPagePress={onPreviousPagePress}
      onNextPagePress={onNextPagePress}
      onLastPagePress={onLastPagePress}
      onPageSelect={onPageSelect}
      onSortPress={onSortPress}
      onFilterSelect={onFilterSelect}
      onTableOptionChange={onTableOptionChange}
      onRefreshPress={onRefreshPress}
      onClearLogsPress={onClearLogsPress}
    />
  );
}

export default withCurrentPage(LogsTableConnector);
