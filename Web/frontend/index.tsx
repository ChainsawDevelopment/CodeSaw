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

import userSagas from './pages/user/userSaga';

import { reviewsReducer } from './pages/reviews/state';
import reviewsSagas from './pages/reviews/sagas';
import { usersReducer } from './pages/user/state';

import { loadingReducer } from './loading/state';

import { adminReducer } from './pages/admin/state';
import adminSagas from './pages/admin/sagas';

interface State {
    router: RouterState
}

const history = createHistory();

const historyMiddleware = routerMiddleware(history);

const sagaMiddleware = sagaMiddlewareFactory();

const composeEnhancers = (window as any).__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const store = createStore(
    combineReducers({
        router: routerReducer,
        review: reviewReducer,
        reviews: reviewsReducer,
        admin: adminReducer,
        currentUser: usersReducer,
        loading: loadingReducer
    }),
    composeEnhancers(
        applyMiddleware(historyMiddleware, sagaMiddleware)
    )
);

for (const saga of reviewSagas) {
    sagaMiddleware.run(saga);
}

for (const saga of reviewsSagas) {
    sagaMiddleware.run(saga);
}

for (const saga of adminSagas) {
    sagaMiddleware.run(saga);
}

for (const saga of userSagas) {
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