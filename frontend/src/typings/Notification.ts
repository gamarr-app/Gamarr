import Provider from './Provider';

interface Notification extends Provider {
  enable: boolean;
  tags: number[];

  // Event triggers
  onGrab: boolean;
  onDownload: boolean;
  onUpgrade: boolean;
  onRename: boolean;
  onGameAdded: boolean;
  onGameDelete: boolean;
  onGameFileDelete: boolean;
  onGameFileDeleteForUpgrade: boolean;
  onHealthIssue: boolean;
  onHealthRestored: boolean;
  onApplicationUpdate: boolean;
  onManualInteractionRequired: boolean;
  includeHealthWarnings: boolean;

  // Capability flags
  supportsOnGrab: boolean;
  supportsOnDownload: boolean;
  supportsOnUpgrade: boolean;
  supportsOnRename: boolean;
  supportsOnGameAdded: boolean;
  supportsOnGameDelete: boolean;
  supportsOnGameFileDelete: boolean;
  supportsOnGameFileDeleteForUpgrade: boolean;
  supportsOnHealthIssue: boolean;
  supportsOnHealthRestored: boolean;
  supportsOnApplicationUpdate: boolean;
  supportsOnManualInteractionRequired: boolean;
}

export default Notification;
