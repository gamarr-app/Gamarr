import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Helmet, HelmetProvider } from 'react-helmet-async';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { Store } from 'redux';
import Page from 'Components/Page/Page';
import ApplyTheme from './ApplyTheme';
import AppRoutes from './AppRoutes';

interface AppProps {
  store: Store;
}

const queryClient = new QueryClient();

function App({ store }: AppProps) {
  return (
    <HelmetProvider>
      <Helmet>
        <title>{window.Gamarr.instanceName}</title>
      </Helmet>
      <QueryClientProvider client={queryClient}>
        <Provider store={store}>
          <BrowserRouter basename={window.Gamarr.urlBase}>
            <ApplyTheme />
            <Page>
              <AppRoutes />
            </Page>
          </BrowserRouter>
        </Provider>
      </QueryClientProvider>
    </HelmetProvider>
  );
}

export default App;
