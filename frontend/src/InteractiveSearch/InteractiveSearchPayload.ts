interface GameSearchPayload {
  gameId: number;
  componentId?: number;
}

type InteractiveSearchPayload = GameSearchPayload;

export default InteractiveSearchPayload;
