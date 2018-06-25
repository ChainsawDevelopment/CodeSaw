import logo from './assets/chainsaw.svg'
import './assets/favicon.png'

import * as React from "react";
import Menu from '@ui/collections/Menu';
import Container from '@ui/elements/Container';
import { Link, Route, withRouter, RouteComponentProps } from "react-router-dom";
import ConnectedSwitch from './routing/ConnectedSwitch';

import ReviewPage from './pages/review';
import ReviewPageTopMenu from './pages/review/topMenu';

import Reviews from "./pages/reviews";

import "./layout.less";
import { ReviewId } from "./api/reviewer";

const Home = () => (
    <span>
        <h1><span className="logo" dangerouslySetInnerHTML={{__html: logo}}></span>CodeSaw</h1>
        <p>The most brutal code review tool!</p>
        <Reviews />
    </span>);

const Review = withRouter((props: RouteComponentProps<{projectId: string; id: string}>) => {
    const { projectId, id } = props.match.params;
    const reviewId: ReviewId = { projectId: parseInt(projectId), reviewId: parseInt(id) };
    
    return (<ReviewPage 
        reviewId={reviewId} 
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
                <ConnectedSwitch>
                    <Route path="/project/:projectId/review/:id" component={ReviewPageTopMenu} />
                </ConnectedSwitch>
            </Container>
        </Menu>
        <Container fluid id="main-content">
            <ConnectedSwitch>
                <Route exact path="/" component={Home} />
                <Route path="/project/:projectId/review/:id" component={Review} />
                <Route path="/layout" component={Layout} />
            </ConnectedSwitch>
        </Container>

        <div className="footer">This is the bottom <i aria-hidden="true" className="pointing down icon"></i></div>
    </> //{/*</div>*/}
);
