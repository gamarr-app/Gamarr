import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ConnectedRouter, ConnectedRouterProps } from 'connected-react-router';
import { Helmet, HelmetProvider } from 'react-helmet-async';
import { Provider } from 'react-redux';
import { Store } from 'redux';
import Page from 'Components/Page/Page';
import ApplyTheme from './ApplyTheme';
import AppRoutes from './AppRoutes';

interface AppProps {
  store: Store;
  history: ConnectedRouterProps['history'];
}

const queryClient = new QueryClient();

function App({ store, history }: AppProps) {
  return (
    <HelmetProvider>
      <Helmet>
        <title>{window.Gamarr.instanceName}</title>
      </Helmet>
      <QueryClientProvider client={queryClient}>
        <Provider store={store}>
          <ConnectedRouter history={history}>
            <ApplyTheme />
            <Page>
              <AppRoutes />
            </Page>
          </ConnectedRouter>
        </Provider>
      </QueryClientProvider>
    </HelmetProvider>
  );
}

export default App;
