import * as React from "react";
import Menu from 'semantic-ui-react/dist/commonjs/collections/Menu';
import Container from 'semantic-ui-react/dist/commonjs/elements/Container';
import { Link, Route, withRouter, RouteComponentProps } from "react-router-dom";
import ConnectedSwitch from './routing/ConnectedSwitch';
import LinkButton from './components/LinkButton';

import ReviewPage from './pages/review';
import Reviews from "./pages/reviews";

import "./layout.less";

const Home = () => (<h1>Home</h1>);

const Review = withRouter((props: RouteComponentProps<{projectId: string; id: string}>) => (
    <ReviewPage 
        reviewId={parseInt(props.match.params.id)} 
        projectId={parseInt(props.match.params.projectId)} 
    />  
));

const Layout = () => {
    return (
        <div className="test-div">
            test
        </div>
    );
};

export default () => (
    <> {/*<div style={{ paddingLeft: '1em', paddingRight: '1em' }}>*/}
        <Menu inverted>
            <Container fluid>
                <Menu.Item as={(props) => (<Link to='/' {...props} />)}>
                    Git Reviewer
                </Menu.Item>

                <Menu.Item as={(props) => (<Link to='/reviews' {...props} />)}>
                    Reviews
                </Menu.Item>
            </Container>
        </Menu>
        <Container fluid id="main-content">
            <ConnectedSwitch>
                <Route exact path="/" component={Home} />
                <Route path="/reviews" component={Reviews} />
                <Route path="/project/:projectId/review/:id" component={Review} />
                <Route path="/layout" component={Layout} />
            </ConnectedSwitch>
        </Container>
    </> //{/*</div>*/}
);
