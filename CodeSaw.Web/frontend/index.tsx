import { createBrowserHistory } from 'history'
import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { Provider } from 'react-redux';
import { ConnectedRouter, RouterState, routerMiddleware, connectRouter } from 'connected-react-router';
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
import notify from './notify';
import { saveUnpublishedReview } from './pages/review/storage';

interface State {
    router: RouterState
}

const history = createBrowserHistory();

const historyMiddleware = routerMiddleware(history);

const sagaMiddleware = sagaMiddlewareFactory({
    onError: e => {
        console.error(e);
        notify.error('Error occured. CodeSaw might be no longer working properly!');
    }
});

const composeEnhancers = (window as any).__REDUX_DEVTOOLS_EXTENSION_COMPOSE__ || compose;

const store = createStore(
    combineReducers({
        router: connectRouter(history),
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

store.subscribe(() => {
    const state = store.getState();
    if (!state.review.currentReview.reviewId) {
        return;
    }

    saveUnpublishedReview(state.review);
})

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