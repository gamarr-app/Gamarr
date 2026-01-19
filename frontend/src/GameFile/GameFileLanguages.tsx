import React from 'react';
import GameLanguages from 'Game/GameLanguages';
import useGameFile from './useGameFile';

interface GameFileLanguagesProps {
  gameFileId: number;
}

function GameFileLanguages({ gameFileId }: GameFileLanguagesProps) {
  const gameFile = useGameFile(gameFileId);

  return <GameLanguages languages={gameFile?.languages ?? []} />;
}

export default GameFileLanguages;
