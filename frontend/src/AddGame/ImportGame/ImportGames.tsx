import { Route } from 'react-router-dom';
import ImportGameConnector from 'AddGame/ImportGame/Import/ImportGameConnector';
import ImportGameSelectFolderConnector from 'AddGame/ImportGame/SelectFolder/ImportGameSelectFolderConnector';
import Switch from 'Components/Router/Switch';

function ImportGames() {
  return (
    <Switch>
      <Route path="/add/import" element={<ImportGameSelectFolderConnector />} />

      <Route
        path="/add/import/:rootFolderId"
        element={<ImportGameConnector />}
      />
    </Switch>
  );
}

export default ImportGames;
