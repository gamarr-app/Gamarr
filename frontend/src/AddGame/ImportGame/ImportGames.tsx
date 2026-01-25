import { Component } from 'react';
import { Route } from 'react-router-dom';
import ImportGameConnector from 'AddGame/ImportGame/Import/ImportGameConnector';
import ImportGameSelectFolderConnector from 'AddGame/ImportGame/SelectFolder/ImportGameSelectFolderConnector';
import Switch from 'Components/Router/Switch';

class ImportGames extends Component {
  //
  // Render

  render() {
    return (
      <Switch>
        <Route
          exact={true}
          path="/add/import"
          component={ImportGameSelectFolderConnector}
        />

        <Route
          path="/add/import/:rootFolderId"
          component={ImportGameConnector}
        />
      </Switch>
    );
  }
}

export default ImportGames;
