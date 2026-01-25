import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import Column from 'Components/Table/Column';
import { setListGameTableOption } from 'Store/Actions/discoverGameActions';
import DiscoverGameHeader from './DiscoverGameHeader';

interface TableOptions {
  pageSize?: number;
  columns?: Column[];
}

function createMapDispatchToProps(dispatch: Dispatch) {
  return {
    onTableOptionChange(payload: TableOptions) {
      dispatch(setListGameTableOption(payload));
    },
  };
}

export default connect(undefined, createMapDispatchToProps)(DiscoverGameHeader);
