import * as React from "react";
import { ReviewSearchArgs, PageInfo, Review } from "../../api/reviewer";
import Pagination from "../../components/pagination";
import Form from "@ui/collections/Form";
import Select, { SelectProps } from '@ui/addons/Select';
import Grid from '@ui/collections/Grid';
import Input, { InputOnChangeData } from '@ui/elements/Input';

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

interface OrderBySelectorProps {
    orderBy: string;
    onChange(newOrderBy: string):void;
}
const OrderBySelector = (props: OrderBySelectorProps): JSX.Element => {
    const states = [
        { text: 'Updated At', value: 'updated_at' },
        { text: 'Created At', value: 'created_at' }
    ];

    const onStateChange =  (e: any, d: SelectProps) => props.onChange(d.value as 'created_at' | 'updated_at');

    return (
        <Form.Field inline>
            <label>OrderBy</label>
            <Select
                value={props.orderBy}
                options={states}
                onChange={onStateChange}
            />
        </Form.Field>
    );
};

interface SortSelectorProps {
    sort: string;
    onChange(sort: string):void;
}
const SortSelector = (props: SortSelectorProps): JSX.Element => {
    const states = [
        { text: 'Ascending', value: 'asc' },
        { text: 'Descending', value: 'desc' }
    ];

    const onStateChange =  (e: any, d: SelectProps) => props.onChange(d.value as 'asc' | 'desc');

    return (
        <Form.Field inline>
            <label>Sort</label>
            <Select
                value={props.sort}
                options={states}
                onChange={onStateChange}
            />
        </Form.Field>
    );
};

interface NameFilterProps {
    onChange(newName: string):void;
    initialName: string;
}
interface NameFilterState {
    name: string;
}
class NameFilter extends React.Component<NameFilterProps, NameFilterState> {
    constructor(props: NameFilterProps) {
        super(props);
        this.state = {
            name: props.initialName
        };
    }

    public render(): JSX.Element {
        const onNameChange =  (e: any, d: InputOnChangeData) => {
            this.setState({ name: d.value });
        };

        const onKeyPress = (e: any) => {
            if (e.key === 'Enter') {
                this.props.onChange(this.state.name);
            }
        };

        return (
            <Form.Field inline>
                <label>Name filter</label>
                <Input
                    value={this.state.name}
                    onChange={onNameChange}
                    onKeyPress={onKeyPress}
                />
            </Form.Field>
        );
    }
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

        const onOrderByChange = (newOrderBy: 'created_at' | 'updated_at') => this.updateArg({
            orderBy: newOrderBy
        });

        const onSortChange = (newSort: 'asc' | 'desc') => this.updateArg({
            sort: newSort
        });

        const onNameFilterChange = (newNameFilter: string) => this.updateArg({
            nameFilter: newNameFilter
        });

        return (
            <Grid>
                <Grid.Row>
                    <Grid.Column>
                        <Form>
                            <Form.Group inline>
                                <StateSelector state={current.state} onChange={onStateChange} />
                                <OrderBySelector orderBy={current.orderBy} onChange={onOrderByChange} />
                                <SortSelector sort={current.sort} onChange={onSortChange} />
                                <NameFilter initialName={current.nameFilter} onChange={onNameFilterChange} />
                            </Form.Group>
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