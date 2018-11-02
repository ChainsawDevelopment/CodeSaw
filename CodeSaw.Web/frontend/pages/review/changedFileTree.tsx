import * as React from "react";
import List from '@ui/elements/List';
import Input from '@ui/elements/Input';
import Icon from '@ui/elements/Icon';

import ReviewMark from './reviewMark';
import * as PathPairs from "../../pathPair";
import { FileLink } from "./FileLink";
import { ReviewId, FileId } from "../../api/reviewer";
import ReviewMode from "./reviewMode";

const FileItem = (props: { fileId: FileId, reviewId: ReviewId, isSelected: boolean, isReviewed: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<span className='selected-file'>{props.fileId}</span>)
    } else {
        header = (<FileLink reviewId={props.reviewId} path={PathPairs.make(props.fileId)} fileId={props.fileId} />);
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
        files: FileId[];
        reviewedFiles: FileId[]; 
        selected: FileId;
        onSelect: (fileId: FileId) => void;
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

        const filteredFiles = props.files;
            //.filter(p => this.state.searchValue == "" || p.newPath.indexOf(this.state.searchValue) != -1);
            //TODO: FIX THIS

        const items = filteredFiles
            .map(f => (
                <FileItem
                    key={f}
                    fileId={f}
                    isSelected={f == props.selected}
                    isReviewed={props.reviewedFiles.indexOf(f) >= 0}
                    onclick={() => props.onSelect(f)}
                    reviewId={props.reviewId}
                />
        ));

        const openFirst = () => props.onSelect(filteredFiles[0]);
    
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