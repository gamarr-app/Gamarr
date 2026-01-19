type ColonReplacementFormat =
  | 'delete'
  | 'dash'
  | 'spaceDash'
  | 'spaceDashSpace'
  | 'smart';

export default interface NamingConfig {
  renameGames: boolean;
  replaceIllegalCharacters: boolean;
  colonReplacementFormat: ColonReplacementFormat;
  standardGameFormat: string;
  gameFolderFormat: string;
}
