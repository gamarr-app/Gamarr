import { routerMiddleware } from 'connected-react-router';
import { History } from 'history';
import { applyMiddleware, compose, Middleware, StoreEnhancer } from 'redux';
import thunk from 'redux-thunk';
import createPersistState from './createPersistState';
import createSentryMiddleware from './createSentryMiddleware';

export default function (history: History): StoreEnhancer {
  const middlewares: Middleware[] = [];
  const sentryMiddleware = createSentryMiddleware();

  if (sentryMiddleware) {
    middlewares.push(sentryMiddleware);
  }

  middlewares.push(routerMiddleware(history));
  middlewares.push(thunk as Middleware);

  const composeEnhancers =
    // eslint-disable-next-line no-underscore-dangle
    window.__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

  return composeEnhancers(
    applyMiddleware(...middlewares),
    createPersistState()
  );
}
