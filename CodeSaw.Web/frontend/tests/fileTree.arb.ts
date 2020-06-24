import * as fc from 'fast-check';
import { FileTreeNode, FolderTreeNode, listFiles } from '@src/treeNode';

export interface FileRef {
    name: string;
}

const fileName = ['file1', 'file2', 'file3', 'file4'];
const folderNames = ['folder1', 'folder2', 'folder3'];

const fileArb: fc.Arbitrary<FileTreeNode<FileRef>> = fc.constantFrom(...fileName).map(f => ({
    type: 'file',
    name: f,
    value: { name: f }
}));

const filesArb: fc.Arbitrary<FileTreeNode<FileRef>[]> = fc.set(fileArb, (a, b) => a.name == b.name);

const folderArb: fc.Memo<FolderTreeNode<FileRef>> = fc.memo(n => {
    if(n > 1) {
        const folders = fc.set(folderArb(n - 1), (a, b) => a.name == b.name);

        return fc.tuple(fc.constantFrom(...folderNames), folders, filesArb).map(t => ({
            nestElements: [],
            name: t[0],
            folders: t[1],
            files: t[2]
        }));
    } else {
        return fc.tuple(fc.constantFrom(...folderNames), filesArb).map(t => ({
            nestElements: [],
            name: t[0],
            folders: [],
            files: t[1],
        }));
    }
});

const folderTreeArb = fc.integer(0, 4).chain(d => folderArb(d));
const folderTreesArb = fc.set(folderTreeArb, (a, b) => a.name == b.name);

export const fullTree = fc.tuple(filesArb, folderTreesArb).map(t => ({
    nestElements: [],
    name: '',
    files: t[0],
    folders: t[1],
}));

export const fileList = fullTree.map(f => listFiles('', f));

export const visitTree = <TValue>(root: FolderTreeNode<TValue>, action: ((node: FolderTreeNode<TValue>, nest: () => void) => void)): void => {
    action(root, () => {
        for (const item of root.folders) {
            visitTree(item, action);
        }
    });
}

