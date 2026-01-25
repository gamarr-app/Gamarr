interface AddGameDefaults {
  rootFolderPath: string;
  monitor: string;
  qualityProfileId: number;
  minimumAvailability: string;
  searchForGame: boolean;
  tags: number[];
}

interface AddGameAppState {
  isFetching: boolean;
  isPopulated: boolean;
  error: unknown;
  isAdding: boolean;
  isAdded: boolean;
  addError: unknown;
  items: unknown[];
  defaults: AddGameDefaults;
}

export default AddGameAppState;
