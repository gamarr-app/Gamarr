import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { Helmet, HelmetProvider } from 'react-helmet-async';
import { Provider } from 'react-redux';
import { RouterProvider } from 'react-router-dom';
import { Store } from 'redux';
import ApplyTheme from './ApplyTheme';
import { router } from './AppRouter';

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
          <ApplyTheme />
          <RouterProvider router={router} />
        </Provider>
      </QueryClientProvider>
    </HelmetProvider>
  );
}

export default App;
