import * as React from "react";
import Menu from 'semantic-ui-react/dist/commonjs/collections/Menu';
import Container from 'semantic-ui-react/dist/commonjs/elements/Container';
import { Link, Route, withRouter, RouteComponentProps } from "react-router-dom";
import ConnectedSwitch from './routing/ConnectedSwitch';
import LinkButton from './components/LinkButton';

import ReviewPage from './pages/review';

import "./layout.less";

const Home = () => (<h1>Home</h1>);
const Reviews = () => (
    <div>
        <h1>Reviews</h1>
        <LinkButton to="/review/3">
        Go to review 3
        </LinkButton>
        <LinkButton to="/review/4">
        Go to review 4
        </LinkButton>
    </div>
);

const Review = withRouter((props: RouteComponentProps<{id: number}>) => (
    <ReviewPage reviewId={props.match.params.id} />  
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
                <Route path="/review/:id" component={Review} />
                <Route path="/layout" component={Layout} />
            </ConnectedSwitch>
        </Container>
    </> //{/*</div>*/}
);
