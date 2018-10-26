import * as React from "react";
import List from '@ui/elements/List';
import Input from '@ui/elements/Input';
import Icon from '@ui/elements/Icon';

import ReviewMark from './reviewMark';
import * as PathPairs from "../../pathPair";
import { FileLink } from "./FileLink";
import { ReviewId } from "../../api/reviewer";
import ReviewMode from "./reviewMode";

const FileItem = (props: { path: PathPairs.PathPair, reviewId: ReviewId, isSelected: boolean, isReviewed: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<span className='selected-file'>{props.path.newPath}</span>)
    } else {
        header = (<FileLink reviewId={props.reviewId} path={props.path} />);
    }

    return (
        <List.Item className="file-tree-item">
            <List.Content>
                <ReviewMode>
                    <ReviewMode.Reviewer>
                        <ReviewMark reviewed={props.isReviewed} size='small' />
                    </ReviewMode.Reviewer>
                    <ReviewMode.Author>
                        <Icon circular inverted name='eye' color='grey' />
                    </ReviewMode.Author>
                </ReviewMode>
                <span>{header}</span>
            </List.Content>
        </List.Item>
    );
}

namespace ChangedFileTree {
    export interface Props {  
        paths: PathPairs.List;
        reviewedFiles: PathPairs.List; 
        selected: PathPairs.PathPair;
        onSelect: (path: PathPairs.PathPair) => void;
        reviewId: ReviewId;
    }

    export interface State {
        searchValue: string;
    }
}

export default class ChangeFileTree extends React.Component<ChangedFileTree.Props, ChangedFileTree.State> {
    searchFieldRef: React.RefObject<Input>;
    
    constructor(props) {
        super(props);

        this.searchFieldRef = React.createRef();

        this.state = {
            searchValue: ""
        }
    }

    componentDidMount() {
        setTimeout(() => this.searchFieldRef.current.focus(), 0);
    }

    render() { 
        const props = this.props;

        const filteredPaths = props.paths
            .filter(p => this.state.searchValue == "" || p.newPath.indexOf(this.state.searchValue) != -1);

        const items = filteredPaths
            .map(p => (
                <FileItem
                    key={p.newPath}
                    path={p}
                    isSelected={p.newPath == props.selected.newPath}
                    isReviewed={PathPairs.contains(props.reviewedFiles, p)}
                    onclick={() => props.onSelect(p)}
                    reviewId={props.reviewId}
                />
        ));

        const openFirst = () => props.onSelect(filteredPaths[0]);
    
        return (
            <div>
                <Input 
                    placeholder="Search..."
                    icon='search'
                    ref={this.searchFieldRef}
                    onChange={(e, data) => this.setState({searchValue: data.value})}
                    onKeyPress={e => (e.key == "Enter") && openFirst()} />
                <List className="file-tree">
                    {items}
                </List>
            </div>
        );
    }
}