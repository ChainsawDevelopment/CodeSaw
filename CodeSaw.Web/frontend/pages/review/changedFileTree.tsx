import * as React from "react";
import List from '@ui/elements/List';
import Input from '@ui/elements/Input';
import Icon from '@ui/elements/Icon';

import ReviewMark from './reviewMark';
import * as PathPairs from "../../pathPair";
import { FileLink } from "./FileLink";
import { ReviewId, FileId } from "../../api/reviewer";
import ReviewMode from "./reviewMode";
import FileName from './FileName';

const FileItem = (props: { fileId: FileId, reviewId: ReviewId, isSelected: boolean, isReviewed: boolean, onclick?: () => void }) => {
    let header: JSX.Element;

    if (props.isSelected) {
        header = (<span className='selected-file'><FileName fileId={props.fileId} /></span>)
    } else {
        header = (<FileLink reviewId={props.reviewId} fileId={props.fileId} />);
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
        files: {id: FileId; name: PathPairs.PathPair}[];
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

        const filteredFiles = props.files
            .filter(p => this.state.searchValue == "" || p.name.newPath.indexOf(this.state.searchValue) != -1);

        const items = filteredFiles
            .map(f => (
                <FileItem
                    key={f.id}
                    fileId={f.id}
                    isSelected={f.id == props.selected}
                    isReviewed={props.reviewedFiles.indexOf(f.id) >= 0}
                    onclick={() => props.onSelect(f.id)}
                    reviewId={props.reviewId}
                />
        ));

        const openFirst = () => props.onSelect(filteredFiles[0].id);
    
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