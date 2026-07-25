export const noIntroVerificationStatuses = [
  'verified',
  'nameMismatch',
  'unknown',
  'badDump',
  'missing',
  'duplicate',
] as const;

export type NoIntroVerificationStatus =
  (typeof noIntroVerificationStatuses)[number];

export const noIntroRomComponentTypes = [
  'retailRom',
  'eReaderCards',
  'multiboot',
  'video',
  'bios',
  'romhackOrUnverified',
] as const;

export type NoIntroRomComponentType = (typeof noIntroRomComponentTypes)[number];

export interface NoIntroCatalogComponentSlot {
  readonly slotLabel: string;
  readonly canonicalName: string;
  readonly componentType: NoIntroRomComponentType;
}

export interface NoIntroCatalogGamePlan {
  readonly systemKey: string;
  readonly gameTitle: string;
  readonly regionLanguageComponents: readonly NoIntroCatalogComponentSlot[];
  readonly downloadPlayComponents: readonly NoIntroCatalogComponentSlot[];
}

export interface NoIntroCatalogStandalonePlan {
  readonly title: string;
  readonly componentType: NoIntroRomComponentType;
}

export interface NoIntroCatalogPlan {
  readonly games: readonly NoIntroCatalogGamePlan[];
  readonly standaloneGames: readonly NoIntroCatalogStandalonePlan[];
}

export interface NoIntroVerificationResult {
  readonly id: number;
  readonly verificationStatus: NoIntroVerificationStatus;
  readonly actualFileName: string;
  readonly expectedFileName?: string;
  readonly isDuplicate: boolean;
  readonly isMissing: boolean;
}
