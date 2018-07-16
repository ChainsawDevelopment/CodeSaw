import logo from './assets/chainsaw.svg'
import './assets/favicon.png'

import * as React from "react";
import Menu from '@ui/collections/Menu';
import Container from '@ui/elements/Container';
import Icon from '@ui/elements/Icon';
import { Link, Route, withRouter, RouteComponentProps } from "react-router-dom";
import ConnectedSwitch from './routing/ConnectedSwitch';

import ReviewPage from './pages/review';
import ReviewPageTopMenu from './pages/review/topMenu';

import Reviews from "./pages/reviews";
import AdminPage from './pages/admin';

import "./layout.less";
import { ReviewId } from "./api/reviewer";
import CurrentUser from './pages/user/User';

const Home = () => (
    <span>
        <h1><span className="logo" dangerouslySetInnerHTML={{__html: logo}}></span>CodeSaw</h1>
        <p>The most brutal code review tool!</p>
        <Reviews />
    </span>);

const Review = withRouter((props: RouteComponentProps<{projectId: string; id: string, fileName: string}>) => {
    const { projectId, id, fileName } = props.match.params;
    const reviewId: ReviewId = { projectId: parseInt(projectId), reviewId: parseInt(id) };

    return (<ReviewPage
        reviewId={reviewId}
        fileName={decodeURIComponent(fileName || '')}
    />)
});

const Layout = () => {
    return (
        <div className="test-div">
            test
        </div>
    );
};

export default () => (
    <>
        <Menu inverted>
            <Container fluid>
                <Menu.Item as={(props) => (<Link to='/' {...props} />)}>
                    Home
                </Menu.Item>
                <Menu.Item as={(props) => (<Link to='/admin' {...props} />)}>
                    Administration
                </Menu.Item>
                <Menu.Menu position='right'>
                    <ConnectedSwitch>
                        <Route path="/project/:projectId/review/:id" component={ReviewPageTopMenu} />
                    </ConnectedSwitch>
                    <Menu.Item>
                        <Icon name="user" circular />
                        <strong><CurrentUser /></strong>
                    </Menu.Item>
                </Menu.Menu>
            </Container>
        </Menu>
        <Container fluid id="main-content">
            <ConnectedSwitch>
                <Route exact path="/" component={Home} />
                <Route path="/project/:projectId/review/:id/:fileName?" component={Review} />
                <Route exact path="/admin" component={AdminPage} />
                <Route path="/layout" component={Layout} />
            </ConnectedSwitch>
        </Container>

        <div className="footer">This is the bottom <i aria-hidden="true" className="pointing down icon"></i></div>
    </>
);
