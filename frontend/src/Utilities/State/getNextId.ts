interface ItemWithId {
  id: number;
}

function getNextId(items: ItemWithId[]): number {
  return items.reduce((id, x) => Math.max(id, x.id), 1) + 1;
}

export default getNextId;
