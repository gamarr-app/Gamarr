import React from 'react';
import translate from 'Utilities/String/translate';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

const protocols = [
  {
    id: 'announced',
    get name() {
      return translate('Announced');
    },
  },
  {
    id: 'inCinemas',
    get name() {
      return translate('InDevelopment');
    },
  },
  {
    id: 'released',
    get name() {
      return translate('Released');
    },
  },
];

function MinimumAvailabilityFilterBuilderRowValue(
  props: FilterBuilderRowValueProps
) {
  return <FilterBuilderRowValue tagList={protocols} {...props} />;
}

export default MinimumAvailabilityFilterBuilderRowValue;
