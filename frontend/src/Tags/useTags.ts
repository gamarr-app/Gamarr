import { useMemo } from 'react';
import { useSelector } from 'react-redux';
import createTagsSelector from 'Store/Selectors/createTagsSelector';

const useTags = () => {
  return useSelector(useMemo(() => createTagsSelector(), []));
};

export default useTags;
