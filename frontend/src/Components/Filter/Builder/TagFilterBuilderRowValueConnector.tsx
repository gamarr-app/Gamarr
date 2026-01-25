import { useSelector } from 'react-redux';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function TagFilterBuilderRowValueConnector(props: FilterBuilderRowValueProps) {
  const tags = useSelector(createTagsSelector());

  const tagList = tags.map((tag) => {
    const { id, label: name } = tag;

    return {
      id,
      name,
    };
  });

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default TagFilterBuilderRowValueConnector;
