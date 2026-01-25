import React, { Component } from 'react';
import { connect } from 'react-redux';
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
import { AppDispatch } from 'Store/thunks';
import LogsTable from './LogsTable';

function createMapStateToProps() {
  return createSelector(
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
}

function createMapDispatchToProps(dispatch: AppDispatch) {
  return {
    fetchLogs() {
      dispatch(fetchLogs());
    },
    gotoLogsFirstPage() {
      dispatch(gotoLogsFirstPage());
    },
    gotoLogsPreviousPage() {
      dispatch(gotoLogsPreviousPage());
    },
    gotoLogsNextPage() {
      dispatch(gotoLogsNextPage());
    },
    gotoLogsLastPage() {
      dispatch(gotoLogsLastPage());
    },
    gotoLogsPage(payload: { page: number }) {
      dispatch(gotoLogsPage(payload));
    },
    setLogsSort(payload: { sortKey: string }) {
      dispatch(setLogsSort(payload));
    },
    setLogsFilter(payload: { selectedFilterKey: string }) {
      dispatch(setLogsFilter(payload));
    },
    setLogsTableOption(payload: { pageSize?: number }) {
      dispatch(setLogsTableOption(payload));
    },
    executeCommand(payload: { name: string; commandFinished?: () => void }) {
      dispatch(executeCommand(payload));
    },
  };
}

type StateProps = ReturnType<ReturnType<typeof createMapStateToProps>>;
type DispatchProps = ReturnType<typeof createMapDispatchToProps>;

interface LogsTableConnectorOwnProps {
  useCurrentPage: boolean;
}

type LogsTableConnectorProps = StateProps &
  DispatchProps &
  LogsTableConnectorOwnProps;

class LogsTableConnector extends Component<LogsTableConnectorProps> {
  //
  // Lifecycle

  componentDidMount() {
    const { useCurrentPage, fetchLogs, gotoLogsFirstPage } = this.props;

    if (useCurrentPage) {
      fetchLogs();
    } else {
      gotoLogsFirstPage();
    }
  }

  componentDidUpdate(prevProps: LogsTableConnectorProps) {
    if (prevProps.clearLogExecuting && !this.props.clearLogExecuting) {
      this.props.gotoLogsFirstPage();
    }
  }

  //
  // Listeners

  onFirstPagePress = () => {
    this.props.gotoLogsFirstPage();
  };

  onPreviousPagePress = () => {
    this.props.gotoLogsPreviousPage();
  };

  onNextPagePress = () => {
    this.props.gotoLogsNextPage();
  };

  onLastPagePress = () => {
    this.props.gotoLogsLastPage();
  };

  onPageSelect = (page: number) => {
    this.props.gotoLogsPage({ page });
  };

  onSortPress = (sortKey: string) => {
    this.props.setLogsSort({ sortKey });
  };

  onFilterSelect = (selectedFilterKey: string | number) => {
    this.props.setLogsFilter({ selectedFilterKey: String(selectedFilterKey) });
  };

  onTableOptionChange = (payload: { pageSize?: number }) => {
    this.props.setLogsTableOption(payload);

    if (payload.pageSize) {
      this.props.gotoLogsFirstPage();
    }
  };

  onRefreshPress = () => {
    this.props.gotoLogsFirstPage();
  };

  onClearLogsPress = () => {
    this.props.executeCommand({
      name: commandNames.CLEAR_LOGS,
      commandFinished: this.onCommandFinished,
    });
  };

  onCommandFinished = () => {
    this.props.gotoLogsFirstPage();
  };

  //
  // Render

  render() {
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
    } = this.props;

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
        onFirstPagePress={this.onFirstPagePress}
        onPreviousPagePress={this.onPreviousPagePress}
        onNextPagePress={this.onNextPagePress}
        onLastPagePress={this.onLastPagePress}
        onPageSelect={this.onPageSelect}
        onSortPress={this.onSortPress}
        onFilterSelect={this.onFilterSelect}
        onTableOptionChange={this.onTableOptionChange}
        onRefreshPress={this.onRefreshPress}
        onClearLogsPress={this.onClearLogsPress}
      />
    );
  }
}

export default withCurrentPage(
  connect(createMapStateToProps, createMapDispatchToProps)(LogsTableConnector)
);
