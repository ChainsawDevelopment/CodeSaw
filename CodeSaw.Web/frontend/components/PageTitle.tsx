import * as React from "react";

const PageTitle = (props: {children: string}) => {
    document.title = props.children;
    return null;
}

export default PageTitle;