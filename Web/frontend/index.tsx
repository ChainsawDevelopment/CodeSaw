import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { createStore, Action } from 'redux';
import { Provider, connect } from 'react-redux';

interface State {
    c: number;
}

const reducer = (s: State = { c: 2}, action: Action) => {
    return s;
};

const store = createStore(reducer);

const Component = 
    connect((s: State) => ({n: s.c}))(
        (props: {n: number}) => (<h1>Test {props.n}</h1>)
    );

const Root = () => (
    <Provider store={store}>
        <Component />
    </Provider>
);

ReactDOM.render((<Root />), document.getElementById('content'));