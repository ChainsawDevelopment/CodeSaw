import * as path from 'path';

export interface FileTreeNode<TValue> {
    name: string;
    value: TValue;
}

export interface FolderTreeNode<TValue> {
    nestElements: string[];
    name: string;
    files: FileTreeNode<TValue>[];
    folders: FolderTreeNode<TValue>[];
}

export type PathExtractor<TValue> = (item: TValue) => string;

const splitPath = <TValue>(item: TValue, extractPath: PathExtractor<TValue>) => {
    let itemPath = extractPath(item);

    if (itemPath.startsWith('/')) {
        itemPath = itemPath.substr(1);
    }

    let itemDirectory = path.dirname(itemPath);
    let directoryElements = itemDirectory.split('/');
    if (itemDirectory == '.') {
        itemDirectory = '';
        directoryElements = [];
    }

    return {
        value: item,
        fullPath: itemPath,
        basename: path.basename(itemPath),
        directory: itemDirectory,
        directoryElements: directoryElements,
    };
};

export const buildTree = <TValue>(files: TValue[], extractPath: PathExtractor<TValue>): FolderTreeNode<TValue> => {
    const splitted = files.map((f) => splitPath(f, extractPath));

    const root: FolderTreeNode<TValue> = {
        folders: [],
        files: [],
        nestElements: [],
        name: '',
    };

    for (const item of splitted) {
        if (item.directoryElements.length == 0) {
            root.files.push({
                name: item.basename,
                value: item.value,
            });
            continue;
        }

        let parent = root;
        for (const segment of item.directoryElements) {
            let inner = parent.folders.find((f) => f.name == segment) as FolderTreeNode<TValue>;
            if (inner == null) {
                inner = {
                    folders: [],
                    files: [],
                    nestElements: [],
                    name: segment,
                };
                parent.folders.push(inner);
            }
            parent = inner;
        }
        parent.files.push({
            name: item.basename,
            value: item.value,
        });
    }

    return root;
};

export const treeDepth = <TValue>(root: FolderTreeNode<TValue>): number => {
    if (root.folders.length > 0) {
        return 1 + Math.max(...root.folders.map((n) => treeDepth(n)));
    } else {
        return 1;
    }
};

export const listFiles = <TValue>(prefix: string, root: FolderTreeNode<TValue>): string[] => {
    const nestedPrefix = prefix + [...root.nestElements, root.name, ''].join('/');
    let result = [];

    for (const item of root.folders) {
        result = [...result, ...listFiles(nestedPrefix, item)];
    }

    for (const item of root.files) {
        result = [...result, nestedPrefix + item.name];
    }

    return result;
};

export const sortTree = <TValue>(root: FolderTreeNode<TValue>): FolderTreeNode<TValue> => {
    return {
        ...root,
        files: root.files.sort((a, b) => a.name.localeCompare(b.name)),
        folders: root.folders.sort((a, b) => a.name.localeCompare(b.name)).map((f) => sortTree(f)),
    };
};

export const shortTree = <TValue>(root: FolderTreeNode<TValue>): FolderTreeNode<TValue> => {
    const shortFolder = (top: FolderTreeNode<TValue>): FolderTreeNode<TValue> => {
        const nesting = [];

        while (top.folders.length == 1 && top.files.length == 0) {
            nesting.push(top.name);
            top = top.folders[0];
        }

        return {
            ...top,
            nestElements: nesting,
            folders: top.folders.map((f) => shortFolder(f)),
        };
    };

    return {
        ...root,
        folders: root.folders.map((f) => shortFolder(f)),
    };
};

export const nestedName = <TValue>(item: FolderTreeNode<TValue>): string => [...item.nestElements, item.name].join('/');
