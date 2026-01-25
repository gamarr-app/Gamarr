import { TagBase } from 'Components/Form/Tag/TagInput';
import TagInputTag, { TagInputTagProps } from 'Components/Form/Tag/TagInputTag';
import { kinds } from 'Helpers/Props';
import translate from 'Utilities/String/translate';
import styles from './FilterBuilderRowValueTag.css';

interface FilterBuilderRowValueTagProps<T extends TagBase>
  extends TagInputTagProps<T> {
  isLastTag: boolean;
}

function FilterBuilderRowValueTag<T extends TagBase>(
  props: FilterBuilderRowValueTagProps<T>
) {
  const { isLastTag, kind: _kind, ...otherProps } = props;

  return (
    <div className={styles.tag}>
      <TagInputTag kind={kinds.DEFAULT} {...otherProps} />

      {isLastTag ? null : <div className={styles.or}>{translate('Or')}</div>}
    </div>
  );
}

export default FilterBuilderRowValueTag;
