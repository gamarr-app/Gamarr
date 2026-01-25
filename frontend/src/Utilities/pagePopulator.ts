type Populator = () => void;

let currentPopulator: Populator | null = null;
let currentReasons: string[] = [];

export function registerPagePopulator(
  populator: Populator,
  reasons: string[] = []
): void {
  currentPopulator = populator;
  currentReasons = reasons;
}

export function unregisterPagePopulator(populator: Populator): void {
  if (currentPopulator === populator) {
    currentPopulator = null;
    currentReasons = [];
  }
}

export function repopulatePage(reason?: string): void {
  if (!currentPopulator) {
    return;
  }

  if (!reason) {
    currentPopulator();
  }

  if (reason && currentReasons.includes(reason)) {
    currentPopulator();
  }
}
