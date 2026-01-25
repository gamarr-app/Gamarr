import { connect } from 'react-redux';
import { Dispatch } from 'redux';
import { clearRestoreBackup } from 'Store/Actions/systemActions';
import RestoreBackupModal from './RestoreBackupModal';

interface OwnProps {
  onModalClose: () => void;
}

function createMapDispatchToProps(dispatch: Dispatch, props: OwnProps) {
  return {
    onModalClose() {
      dispatch(clearRestoreBackup());

      props.onModalClose();
    },
  };
}

export default connect(null, createMapDispatchToProps)(RestoreBackupModal);
