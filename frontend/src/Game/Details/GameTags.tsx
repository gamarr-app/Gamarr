import Label from 'Components/Label';
import useGame from 'Game/useGame';
import { kinds, sizes } from 'Helpers/Props';
import useTags from 'Tags/useTags';
import sortByProp from 'Utilities/Array/sortByProp';

interface GameTagsProps {
  gameId: number;
}

function GameTags({ gameId }: GameTagsProps) {
  const game = useGame(gameId)!;
  const tagList = useTags();

  const tags = game.tags
    .map((tagId) => tagList.find((tag) => tag.id === tagId))
    .filter((tag) => !!tag)
    .sort(sortByProp('label'))
    .map((tag) => tag.label);

  return (
    <div>
      {tags.map((tag) => {
        return (
          <Label key={tag} kind={kinds.INFO} size={sizes.LARGE}>
            {tag}
          </Label>
        );
      })}
    </div>
  );
}

export default GameTags;
