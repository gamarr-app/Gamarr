import { connect } from 'react-redux';
import { setListGameTableOption } from 'Store/Actions/discoverGameActions';
import DiscoverGameHeader from './DiscoverGameHeader';

function createMapDispatchToProps(dispatch, props) {
  return {
    onTableOptionChange(payload) {
      dispatch(setListGameTableOption(payload));
    }
  };
}

export default connect(undefined, createMapDispatchToProps)(DiscoverGameHeader);
