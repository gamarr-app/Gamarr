import ModelBase from 'App/ModelBase';

export default interface ImportListExclusion extends ModelBase {
  igdbId: number;
  gameTitle: string;
  gameYear: number;
}
