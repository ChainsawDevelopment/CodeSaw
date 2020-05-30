import * as React from "react";
import List from '@ui/elements/List';

interface TreeNode {
    name: string;
    children: TreeNode[];
}

const buildTree = (prefix: string, childrenCount: number, depth: number) : TreeNode[] => {
    if(depth == 0) {
        return [];
    }

    return new Array(childrenCount).fill(null).map((_, i) => ({
        name: `${prefix}.${i}`,
        children: buildTree(`${prefix}.${i}`, childrenCount, depth - 1)
    }));
}

const tree = buildTree('item', 4, 3);

const TreeNodeView = (props: {node: TreeNode}): JSX.Element => {
    const children = props.node.children.map(n => <TreeNodeView node={n} />);

    return <List.Item>
        <List.Icon name='folder' />
        <List.Content>
            <List.Header>{props.node.name}</List.Header>
            {children.length == 0 ? null : <List.List>{children}</List.List>}
        </List.Content>
    </List.Item>;
}

const DummyTree = (props: {}): JSX.Element => {
    const items = tree.map(n => <TreeNodeView node={n} />);

    return <List>
        {items}
    </List>
};

export default DummyTree;