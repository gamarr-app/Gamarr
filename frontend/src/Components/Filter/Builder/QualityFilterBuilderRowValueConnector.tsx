import { useEffect } from 'react';
import { useDispatch, useSelector } from 'react-redux';
import { createSelector } from 'reselect';
import AppState from 'App/State/AppState';
import { fetchQualityProfileSchema } from 'Store/Actions/settingsActions';
import getQualities from 'Utilities/Quality/getQualities';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function createQualitiesSelector() {
  return createSelector(
    (state: AppState) => state.settings.qualityProfiles,
    (qualityProfiles) => {
      const { isSchemaPopulated: isPopulated, schema } = qualityProfiles;

      const tagList = getQualities(schema.items);

      return {
        isPopulated,
        tagList,
      };
    }
  );
}

function QualityFilterBuilderRowValueConnector(
  props: FilterBuilderRowValueProps
) {
  const dispatch = useDispatch();
  const { isPopulated, tagList } = useSelector(createQualitiesSelector());

  useEffect(() => {
    if (!isPopulated) {
      dispatch(fetchQualityProfileSchema());
    }
  }, [isPopulated, dispatch]);

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default QualityFilterBuilderRowValueConnector;
