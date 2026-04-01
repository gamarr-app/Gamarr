import { isEqual } from 'lodash';
import { createSelectorCreator, lruMemoize } from 'reselect';

const createDeepEqualSelector = createSelectorCreator(lruMemoize, isEqual);

export default createDeepEqualSelector;
