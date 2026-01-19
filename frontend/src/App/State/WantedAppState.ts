import AppSectionState, {
  AppSectionFilterState,
  PagedAppSectionState,
  TableAppSectionState,
} from 'App/State/AppSectionState';
import Game from 'Game/Game';

interface WantedGame extends Game {
  isSaving?: boolean;
}

interface WantedCutoffUnmetAppState
  extends AppSectionState<WantedGame>,
    AppSectionFilterState<WantedGame>,
    PagedAppSectionState,
    TableAppSectionState {}

interface WantedMissingAppState
  extends AppSectionState<WantedGame>,
    AppSectionFilterState<WantedGame>,
    PagedAppSectionState,
    TableAppSectionState {}

interface WantedAppState {
  cutoffUnmet: WantedCutoffUnmetAppState;
  missing: WantedMissingAppState;
}

export default WantedAppState;
