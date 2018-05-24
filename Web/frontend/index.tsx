import createHistory from 'history/createBrowserHistory';
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { ConnectedRouter, RouterState, routerMiddleware, routerReducer } from 'react-router-redux';
import { applyMiddleware, combineReducers, createStore, compose } from 'redux';
import sagaMiddlewareFactory from 'redux-saga';

import Layout from './layout';
import { reviewReducer } from './pages/review/state';
import reviewSagas from './pages/review/sagas';

interface State {
    c: number;
    router: RouterState
}

const history = createHistory();

const historyMiddleware = routerMiddleware(history);

const sagaMiddleware = sagaMiddlewareFactory();

const composeEnhancers = (window as any).__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const store = createStore(
    combineReducers({
        router: routerReducer,
        review: reviewReducer
    }),
    composeEnhancers(
        applyMiddleware(historyMiddleware, sagaMiddleware)
    )
);

for (const saga of reviewSagas) {
    sagaMiddleware.run(saga);
}

const Root = () => (
    <Provider store={store}>
        <ConnectedRouter history={history}>
            <Layout />
        </ConnectedRouter>
    </Provider>
);

ReactDOM.render((<Root />), document.getElementById('content'));