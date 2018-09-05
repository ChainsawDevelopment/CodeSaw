import logo from './assets/chainsaw.svg'
import './assets/favicon.png'

import * as React from "react";
import Menu from '@ui/collections/Menu';
import Container from '@ui/elements/Container';
import Icon from '@ui/elements/Icon';
import Dimmer from '@ui/modules/Dimmer';
import Loader from '@ui/elements/Loader';
import { Link, Route, withRouter, RouteComponentProps } from "react-router-dom";
import { connect } from 'react-redux';
import { ToastContainer } from 'react-toastify';
import 'react-toastify/dist/ReactToastify.min.css';

import ConnectedSwitch from './routing/ConnectedSwitch';

import ReviewPage from './pages/review';
import ReviewPageTopMenu from './pages/review/topMenu';

import Reviews from "./pages/reviews";
import AdminPage from './pages/admin';

import "./layout.less";
import { ReviewId } from "./api/reviewer";
import CurrentUser from './pages/user/User';
import { RootState } from './rootState';

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

interface StateProps {
    inProgressOperationsCount: number;
}

const Layout = (props: StateProps) => (
    <Dimmer.Dimmable as='div'>
        <Dimmer active={props.inProgressOperationsCount > 0} page>
            <Loader size='large' />
        </Dimmer>
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
        <ToastContainer />
        <Container fluid id="main-content">
            <ConnectedSwitch>
                <Route exact path="/" component={Home} />
                <Route path="/project/:projectId/review/:id/:fileName?" component={Review} />
                <Route exact path="/admin" component={AdminPage} />
            </ConnectedSwitch>
        </Container>

       

        <div className="footer">This is the bottom <i aria-hidden="true" className="pointing down icon"></i></div>
    </Dimmer.Dimmable>
);

export default connect(
    (state: RootState):StateProps => ({
        inProgressOperationsCount: state.loading.inProgressOperationsCount
    })
)(Layout);