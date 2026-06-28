import { useEffect, useMemo } from 'react';
import { useSelector } from 'react-redux';
import { useNavigate, useParams } from 'react-router-dom';
import NotFound from 'Components/NotFound';
import usePrevious from 'Helpers/Hooks/usePrevious';
import createAllGamesSelector from 'Store/Selectors/createAllGamesSelector';
import translate from 'Utilities/String/translate';
import GameDetails from './GameDetails';

function GameDetailsPage() {
  const allGames = useSelector(useMemo(() => createAllGamesSelector(), []));
  const { titleSlug } = useParams<{ titleSlug: string }>();
  const navigate = useNavigate();

  const gameIndex = allGames.findIndex((game) => game.titleSlug === titleSlug);

  const previousIndex = usePrevious(gameIndex);

  useEffect(() => {
    if (
      gameIndex === -1 &&
      previousIndex !== -1 &&
      previousIndex !== undefined
    ) {
      navigate(`${window.Gamarr.urlBase}/`);
    }
  }, [gameIndex, previousIndex, navigate]);

  const game = allGames[gameIndex];

  if (gameIndex === -1 || game === undefined) {
    return <NotFound message={translate('GameCannotBeFound')} />;
  }

  return <GameDetails gameId={game.id} />;
}

export default GameDetailsPage;
