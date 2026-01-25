import React from 'react';
import { useSelector } from 'react-redux';
import createImportListSelector from 'Store/Selectors/createImportListSelector';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function ImportListFilterBuilderRowValueConnector(
  props: FilterBuilderRowValueProps
) {
  const importLists = useSelector(createImportListSelector());

  const tagList = importLists.map((importList) => {
    const { id, name } = importList;

    return {
      id,
      name,
    };
  });

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default ImportListFilterBuilderRowValueConnector;
