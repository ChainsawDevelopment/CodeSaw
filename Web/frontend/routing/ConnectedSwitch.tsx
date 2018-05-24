import { RouterState } from "react-router-redux";
import { connect } from "react-redux";
import { Switch } from "react-router";

export default connect((state: { router: RouterState }) => ({
    location: state.router.location
}))(Switch)