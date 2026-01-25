import { useSelector } from 'react-redux';
import Game from 'Game/Game';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import sortByProp from 'Utilities/Array/sortByProp';
import FilterBuilderRowValue from './FilterBuilderRowValue';
import FilterBuilderRowValueProps from './FilterBuilderRowValueProps';

function GameFilterBuilderRowValue(props: FilterBuilderRowValueProps) {
  const allGames: Game[] = useSelector(createAllGamesSelector());

  const tagList = allGames
    .map((game) => ({ id: game.id, name: game.title }))
    .sort(sortByProp('name'));

  return <FilterBuilderRowValue {...props} tagList={tagList} />;
}

export default GameFilterBuilderRowValue;
