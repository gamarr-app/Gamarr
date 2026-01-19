import { connect } from 'react-redux';
import { createSelector } from 'reselect';
import FilterModal from 'Components/Filter/FilterModal';
import { setListGameFilter } from 'Store/Actions/discoverGameActions';

function createMapStateToProps() {
  return createSelector(
    (state) => state.discoverGame.items,
    (state) => state.discoverGame.filterBuilderProps,
    (sectionItems, filterBuilderProps) => {
      return {
        sectionItems,
        filterBuilderProps,
        customFilterType: 'discoverGame'
      };
    }
  );
}

const mapDispatchToProps = {
  dispatchSetFilter: setListGameFilter
};

export default connect(createMapStateToProps, mapDispatchToProps)(FilterModal);
