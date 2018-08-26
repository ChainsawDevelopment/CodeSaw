import * as React from "react";
import { ReviewSearchArgs, PageInfo, Review } from "../../api/reviewer";
import Pagination from "../../components/pagination";
import Form from "@ui/collections/Form";
import Select, { SelectProps } from '@ui/addons/Select';
import Grid from '@ui/collections/Grid';

interface StateSelectorProps {
    state: string;
    onChange(newState: string):void;
}
const StateSelector = (props: StateSelectorProps): JSX.Element => {
    const states = [
        { text: 'Opened', value: 'opened' },
        { text: 'Closed', value: 'closed' },
        { text: 'Merged', value: 'merged' },
    ];

    const onStateChange =  (e: any, d: SelectProps) => props.onChange(d.value as string);

    return (
        <Form.Field inline>
            <label>State</label>
            <Select
                value={props.state}
                options={states}
                onChange={onStateChange}
            />
        </Form.Field>
    );
};

interface Props {
    currentPage: PageInfo;
    initialArgs: ReviewSearchArgs;
    loadResults(args: ReviewSearchArgs): void;
}

interface State {
    current: ReviewSearchArgs;
}

class SearchOptions extends React.Component<Props, State> {
    constructor(props: Props) {
        super(props);
        this.state = {
            current: props.initialArgs
        };
    }

    public componentDidUpdate(prevProps: Props, prevState: State) {
        if (prevState.current != this.state.current) {
            this.props.loadResults({
                ...this.state.current,
                page: 1
            });
        }
    }

    private updateArg = (change: {[T in keyof ReviewSearchArgs]?: ReviewSearchArgs[T]}) => {
        this.setState({ 
            ...this.state,
            current: {
                ...this.state.current,
                ...change
            }
        });
    }

    public render(): JSX.Element {
        const { current } = this.state;

        const onPageChange = (page: number) => this.props.loadResults({
            ...current,
            page: page
        });

        const onStateChange = (newState: string) => this.updateArg({
            state: newState
        });

        return (
            <Grid>
                <Grid.Row>
                    <Grid.Column>
                        <Form>
                            <StateSelector state={current.state} onChange={onStateChange} />
                        </Form>
                    </Grid.Column>
                </Grid.Row>
                <Grid.Row>
                    <Grid.Column width={12}>
                        <Pagination page={this.props.currentPage} onPageChange={onPageChange} />
                    </Grid.Column>
                </Grid.Row>
            </Grid>
        );
    }
}

export default SearchOptions;