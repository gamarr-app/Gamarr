import { useSelector } from 'react-redux';
import createTagsSelector from 'Store/Selectors/createTagsSelector';
import TagList from './TagList';

interface GameTagListProps {
  tags: number[];
}

function GameTagList({ tags }: GameTagListProps) {
  const tagList = useSelector(createTagsSelector());

  return <TagList tags={tags} tagList={tagList} />;
}

export default GameTagList;
