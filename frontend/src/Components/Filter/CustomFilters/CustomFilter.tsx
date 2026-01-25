import { Component } from 'react';
import { Error } from 'App/State/AppSectionState';
import IconButton from 'Components/Link/IconButton';
import SpinnerIconButton from 'Components/Link/SpinnerIconButton';
import { icons } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './CustomFilter.css';

interface CustomFilterProps {
  id: number;
  label: string;
  selectedFilterKey: string | number;
  isDeleting: boolean;
  deleteError?: Error | null;
  dispatchSetFilter: (payload: { selectedFilterKey: string }) => void;
  onEditPress: (id: number) => void;
  dispatchDeleteCustomFilter: (payload: { id: number }) => void;
}

interface CustomFilterState {
  isDeleting: boolean;
}

class CustomFilter extends Component<CustomFilterProps, CustomFilterState> {
  //
  // Lifecycle

  constructor(props: CustomFilterProps) {
    super(props);

    this.state = {
      isDeleting: false,
    };
  }

  componentDidUpdate(prevProps: CustomFilterProps) {
    const { isDeleting, deleteError } = this.props;

    if (
      prevProps.isDeleting &&
      !isDeleting &&
      this.state.isDeleting &&
      deleteError
    ) {
      this.setState({ isDeleting: false });
    }
  }

  componentWillUnmount() {
    const { id, selectedFilterKey, dispatchSetFilter } = this.props;

    // Assume that delete and then unmounting means the deletion was successful.
    // Moving this check to an ancestor would be more accurate, but would have
    // more boilerplate.
    if (this.state.isDeleting && id === selectedFilterKey) {
      dispatchSetFilter({ selectedFilterKey: 'all' });
    }
  }

  //
  // Listeners

  onEditPress = () => {
    const { id, onEditPress } = this.props;

    onEditPress(id);
  };

  onRemovePress = () => {
    const { id, dispatchDeleteCustomFilter } = this.props;

    this.setState({ isDeleting: true }, () => {
      dispatchDeleteCustomFilter({ id });
    });
  };

  //
  // Render

  render() {
    const { label } = this.props;

    return (
      <div className={styles.customFilter}>
        <div className={styles.label}>{label}</div>

        <div className={styles.actions}>
          <IconButton name={icons.EDIT} onPress={this.onEditPress} />

          <SpinnerIconButton
            title={translate('RemoveFilter')}
            name={icons.REMOVE}
            isSpinning={this.state.isDeleting}
            onPress={this.onRemovePress}
          />
        </div>
      </div>
    );
  }
}

export default CustomFilter;
