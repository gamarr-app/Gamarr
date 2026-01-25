declare module '*.module.css';

interface Window {
  Gamarr: {
    analytics: boolean;
    apiKey: string;
    apiRoot: string;
    branch: string;
    instanceName: string;
    isProduction: boolean;
    release: string;
    theme: string;
    urlBase: string;
    userHash: string;
    version: string;
  };
  __REDUX_DEVTOOLS_EXTENSION_COMPOSE__?: typeof import('redux').compose;
}
