// @ts-expect-error - no type declarations available
import { DndProvider } from 'react-dnd-multi-backend';
// @ts-expect-error - no type declarations available
import HTML5toTouch from 'react-dnd-multi-backend/dist/esm/HTML5toTouch';
import PageContent from 'Components/Page/PageContent';
import PageContentBody from 'Components/Page/PageContentBody';
import SettingsToolbar from 'Settings/SettingsToolbar';
import translate from 'Utilities/String/translate';
import DelayProfiles from './Delay/DelayProfiles';
import QualityProfiles from './Quality/QualityProfiles';
import ReleaseProfiles from './Release/ReleaseProfiles';

// Only a single DragDrop Context can exist so it's done here to allow editing
// quality profiles and reordering delay profiles to work.

function Profiles() {
  return (
    <PageContent title={translate('Profiles')}>
      <SettingsToolbar showSave={false} />

      <PageContentBody>
        <DndProvider options={HTML5toTouch}>
          <QualityProfiles />
          <DelayProfiles />
          <ReleaseProfiles />
        </DndProvider>
      </PageContentBody>
    </PageContent>
  );
}

export default Profiles;
