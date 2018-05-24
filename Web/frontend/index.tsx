import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { createStore, Action, combineReducers, applyMiddleware  } from 'redux';
import { Provider, connect } from 'react-redux';
import { ConnectedRouter, routerReducer, routerMiddleware, push, RouterState } from 'react-router-redux';
import { Route, Switch } from 'react-router';
import createHistory from 'history/createBrowserHistory'
import { Link } from 'react-router-dom';
import Button from 'semantic-ui-react/dist/commonjs/elements/Button';
import LinkButton from './components/LinkButton';
import Layout from './layout';

interface State {
    c: number;
    router: RouterState
}

const history = createHistory();

const middleware = routerMiddleware(history)

const store = createStore(
    combineReducers({
        router: routerReducer
    }),
    applyMiddleware(middleware));

const Component = 
    connect((s: State) => ({n: s.c}))(
        (props: {n: number}) => (<h1>Test {props.n}</h1>)
    );

const Label = (props: {text: string}) => (<p>{props.text}</p>);

const Text = () => (<Label text="text"/>);



//   const AppContainer = () => (
//       <div>
//           <strong>Pre route</strong>
//             <ConnectedSwitch>
//                 <Route exact path="/" component={() => (<h1>Home <LinkButton to="/about">About</LinkButton></h1>)} />
//                 <Route path="/about" component={() => (<h1>About <Link to="/">Home</Link></h1>)} />
//             </ConnectedSwitch>
//         <strong>Post route</strong>
//     </div>
//   )
  
//   const App = connect((state: State) => ({
//     location: state.router.location,
//   }))(AppContainer)

const Root = () => (
    <Provider store={store}>
        <ConnectedRouter history={history}>
            <Layout />
        </ConnectedRouter>
    </Provider>
);

ReactDOM.render((<Root />), document.getElementById('content'));