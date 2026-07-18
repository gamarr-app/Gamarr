type ColonReplacementFormat =
  'delete' | 'dash' | 'spaceDash' | 'spaceDashSpace' | 'smart';

type RenameProfile = 'gamarr' | 'noIntroPreserveById' | 'noIntroCanonical';

export default interface NamingConfig {
  renameGames: boolean;
  renameProfile: RenameProfile;
  replaceIllegalCharacters: boolean;
  colonReplacementFormat: ColonReplacementFormat;
  standardGameFormat: string;
  gameFolderFormat: string;
}
